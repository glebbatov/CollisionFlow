/*
    003_StoredProcedures.sql

    CREATE OR ALTER throughout, so this file is the definition of each procedure
    rather than a one-time migration. Re-running it redeploys the current shape.

    Error contract - the API maps these numbers to HTTP status codes, so they are
    part of the interface and must not be renumbered:

        50001  transition not permitted by the workflow  -> 422 Unprocessable Entity
        50002  concurrency conflict (stale RowVersion)   -> 409 Conflict
        50004  repair order not found                    -> 404 Not Found
*/
/*
    SET options are part of a script's correctness, not its environment.

    A filtered index cannot be created unless QUOTED_IDENTIFIER is ON, and - less
    obviously - a stored procedure permanently captures these settings at CREATE
    time. A procedure compiled with QUOTED_IDENTIFIER OFF fails at runtime the
    moment it touches a table carrying a filtered index.

    sqlcmd connects with QUOTED_IDENTIFIER OFF by default, so setting it here
    means these scripts behave identically through sqlcmd, SSMS, the application's
    startup migrator and CI - rather than depending on who ran them.
*/
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO


------------------------------------------------------------------------------
-- The projection every read returns. Defined once, in one procedure, and reused
-- by the others so the result shape cannot drift between endpoints.
------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.usp_RepairJob_GetById
    @RepairJobId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        j.RepairJobId,
        j.JobNumber,
        j.CustomerName,
        j.VehicleYear,
        j.VehicleMake,
        j.VehicleModel,
        RepairCenter   = c.Name,
        RepairStatusId = j.RepairStatusId,
        StatusCode     = s.Code,
        j.CreatedUtc,
        j.UpdatedUtc,
        j.[RowVersion]
    FROM dbo.RepairJob AS j
    INNER JOIN dbo.RepairCenter AS c ON c.RepairCenterId = j.RepairCenterId
    INNER JOIN dbo.RepairStatus AS s ON s.RepairStatusId = j.RepairStatusId
    WHERE j.RepairJobId = @RepairJobId;
END
GO

------------------------------------------------------------------------------
-- The board. Optional filters; NULL means "no filter", which keeps one plan
-- shape for the common case instead of building a string.
------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.usp_RepairJob_GetAll
    @RepairStatusId TINYINT = NULL,
    @RepairCenterId INT     = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        j.RepairJobId,
        j.JobNumber,
        j.CustomerName,
        j.VehicleYear,
        j.VehicleMake,
        j.VehicleModel,
        RepairCenter   = c.Name,
        RepairStatusId = j.RepairStatusId,
        StatusCode     = s.Code,
        j.CreatedUtc,
        j.UpdatedUtc,
        j.[RowVersion]
    FROM dbo.RepairJob AS j
    INNER JOIN dbo.RepairCenter AS c ON c.RepairCenterId = j.RepairCenterId
    INNER JOIN dbo.RepairStatus AS s ON s.RepairStatusId = j.RepairStatusId
    WHERE (@RepairStatusId IS NULL OR j.RepairStatusId = @RepairStatusId)
      AND (@RepairCenterId IS NULL OR j.RepairCenterId = @RepairCenterId)
    ORDER BY j.UpdatedUtc DESC
    OPTION (RECOMPILE);   -- optional predicates; let the optimizer see the actual values
END
GO

------------------------------------------------------------------------------
-- The audit trail for one repair order, newest first.
------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.usp_RepairJob_GetStatusHistory
    @RepairJobId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        h.RepairJobStatusHistoryId,
        h.RepairJobId,
        FromStatusCode = f.Code,
        ToStatusCode   = t.Code,
        h.ChangedUtc,
        h.ChangedBy,
        h.Note
    FROM dbo.RepairJobStatusHistory AS h
    INNER JOIN dbo.RepairStatus AS f ON f.RepairStatusId = h.FromStatusId
    INNER JOIN dbo.RepairStatus AS t ON t.RepairStatusId = h.ToStatusId
    WHERE h.RepairJobId = @RepairJobId
    ORDER BY h.ChangedUtc DESC, h.RepairJobStatusHistoryId DESC;
END
GO

------------------------------------------------------------------------------
-- The workflow, in two result sets: the statuses, then the legal edges between
-- them. One round trip; Dapper reads both with QueryMultiple.
--
-- This is what makes "the rules live in one place" true rather than aspirational
-- - the application builds its transition policy from these rows at startup.
------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.usp_Workflow_Get
AS
BEGIN
    SET NOCOUNT ON;

    SELECT RepairStatusId, Code, DisplayName, SortOrder, IsTerminal
    FROM dbo.RepairStatus
    ORDER BY SortOrder;

    SELECT FromStatusId, ToStatusId
    FROM dbo.StatusTransition
    ORDER BY FromStatusId, ToStatusId;
END
GO

------------------------------------------------------------------------------
-- Move a repair order to a new status.
--
-- Everything that must agree happens inside one transaction: the row is read
-- under a write lock, the workflow is checked, the concurrency token is checked,
-- the status changes, and the audit row is written. There is no window in which
-- a second caller can observe a status change without its history row, or slip a
-- competing update between the check and the write.
------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.usp_RepairJob_UpdateStatus
    @RepairJobId UNIQUEIDENTIFIER,
    @ToStatusId  TINYINT,
    @ChangedBy   NVARCHAR(120) = N'system',
    @Note        NVARCHAR(400) = NULL,
    @RowVersion  BINARY(8)     = NULL   -- optional; when supplied, enforced
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @FromStatusId TINYINT,
                @CurrentRowVersion BINARY(8);

        -- UPDLOCK taken on the read, not the write. Two advisors submitting at the
        -- same instant serialize here instead of both passing the workflow check.
        SELECT
            @FromStatusId      = j.RepairStatusId,
            @CurrentRowVersion = j.[RowVersion]
        FROM dbo.RepairJob AS j WITH (UPDLOCK, ROWLOCK)
        WHERE j.RepairJobId = @RepairJobId;

        IF @FromStatusId IS NULL
            THROW 50004, 'Repair order not found.', 1;

        IF @RowVersion IS NOT NULL AND @RowVersion <> @CurrentRowVersion
            THROW 50002, 'The repair order was changed by someone else since you loaded it.', 1;

        -- Re-sending the current status succeeds and changes nothing. That is what
        -- makes the HTTP PUT idempotent, so a retry after a dropped connection is
        -- safe. It deliberately writes no history row - a no-op is not activity.
        IF @ToStatusId = @FromStatusId
        BEGIN
            COMMIT TRANSACTION;
            EXEC dbo.usp_RepairJob_GetById @RepairJobId;
            RETURN;
        END

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.StatusTransition
            WHERE FromStatusId = @FromStatusId
              AND ToStatusId   = @ToStatusId)
        BEGIN
            -- Compose the message here rather than in the application: the status
            -- names live in this database, so this is where they should be read.
            DECLARE @message NVARCHAR(400) =
                CONCAT(N'A repair order in ''',
                       (SELECT DisplayName FROM dbo.RepairStatus WHERE RepairStatusId = @FromStatusId),
                       N''' cannot move to ''',
                       (SELECT DisplayName FROM dbo.RepairStatus WHERE RepairStatusId = @ToStatusId),
                       N'''.');

            THROW 50001, @message, 1;
        END

        UPDATE dbo.RepairJob
        SET RepairStatusId = @ToStatusId,
            UpdatedUtc     = CAST(SYSUTCDATETIME() AS DATETIMEOFFSET(3))
        WHERE RepairJobId = @RepairJobId;

        INSERT dbo.RepairJobStatusHistory (RepairJobId, FromStatusId, ToStatusId, ChangedUtc, ChangedBy, Note)
        VALUES (@RepairJobId, @FromStatusId, @ToStatusId,
                CAST(SYSUTCDATETIME() AS DATETIMEOFFSET(3)), @ChangedBy, @Note);

        COMMIT TRANSACTION;

        EXEC dbo.usp_RepairJob_GetById @RepairJobId;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        THROW;   -- preserves the original error number so the API can map it
    END CATCH
END
GO
