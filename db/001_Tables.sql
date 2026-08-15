/*
    001_Tables.sql - schema.

    Every script in this folder is idempotent and safe to run repeatedly, so the
    same files bootstrap a developer's LocalDB, a Testcontainers instance in CI,
    and Azure SQL on startup. No branch of that path is untested.
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
-- Lookup: the approved statuses.
-- Ids are fixed and match the RepairStatus enum in the domain. They are part of
-- the persisted contract, so they are assigned explicitly and never reordered.
------------------------------------------------------------------------------
IF OBJECT_ID('dbo.RepairStatus', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RepairStatus
    (
        RepairStatusId TINYINT       NOT NULL CONSTRAINT PK_RepairStatus PRIMARY KEY,
        Code           VARCHAR(32)   NOT NULL CONSTRAINT UQ_RepairStatus_Code UNIQUE,
        DisplayName    NVARCHAR(50)  NOT NULL,
        SortOrder      TINYINT       NOT NULL,
        IsTerminal     BIT           NOT NULL CONSTRAINT DF_RepairStatus_IsTerminal DEFAULT (0)
    );
END
GO

------------------------------------------------------------------------------
-- The workflow itself, stored as edges.
--
-- This table IS the business rule. The stored procedure validates against it,
-- the API reports it, and the client renders its status pickers from it. Adding
-- or removing a legal move is a data change, not a deployment.
------------------------------------------------------------------------------
IF OBJECT_ID('dbo.StatusTransition', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StatusTransition
    (
        FromStatusId TINYINT NOT NULL,
        ToStatusId   TINYINT NOT NULL,

        CONSTRAINT PK_StatusTransition PRIMARY KEY (FromStatusId, ToStatusId),
        CONSTRAINT FK_StatusTransition_From FOREIGN KEY (FromStatusId) REFERENCES dbo.RepairStatus (RepairStatusId),
        CONSTRAINT FK_StatusTransition_To   FOREIGN KEY (ToStatusId)   REFERENCES dbo.RepairStatus (RepairStatusId),

        -- A "transition" to the same status is a no-op, not an edge. Keeping it
        -- out of the table means the no-op path cannot be confused with a rule.
        CONSTRAINT CK_StatusTransition_NotSelf CHECK (FromStatusId <> ToStatusId)
    );
END
GO

------------------------------------------------------------------------------
-- Repair centers.
------------------------------------------------------------------------------
IF OBJECT_ID('dbo.RepairCenter', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RepairCenter
    (
        RepairCenterId INT           NOT NULL IDENTITY(1, 1) CONSTRAINT PK_RepairCenter PRIMARY KEY,
        Name           NVARCHAR(100) NOT NULL CONSTRAINT UQ_RepairCenter_Name UNIQUE,
        City           NVARCHAR(60)  NOT NULL,
        [State]        CHAR(2)       NOT NULL,
        IsActive       BIT           NOT NULL CONSTRAINT DF_RepairCenter_IsActive DEFAULT (1)
    );
END
GO

------------------------------------------------------------------------------
-- Repair orders.
------------------------------------------------------------------------------
IF OBJECT_ID('dbo.RepairJob', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RepairJob
    (
        -- Surrogate clustering key. A random GUID makes a poor clustered index -
        -- inserts land mid-page and fragment the table - so the public identifier
        -- stays a GUID while the physical order follows a sequential int.
        RepairJobKey   INT              NOT NULL IDENTITY(1, 1),

        RepairJobId    UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RepairJob PRIMARY KEY NONCLUSTERED,
        JobNumber      VARCHAR(16)      NOT NULL CONSTRAINT UQ_RepairJob_JobNumber UNIQUE,
        CustomerName   NVARCHAR(120)    NOT NULL,
        VehicleYear    SMALLINT         NOT NULL,
        VehicleMake    NVARCHAR(50)     NOT NULL,
        VehicleModel   NVARCHAR(60)     NOT NULL,
        RepairCenterId INT              NOT NULL,
        RepairStatusId TINYINT          NOT NULL,
        CreatedUtc     DATETIMEOFFSET(3) NOT NULL,
        UpdatedUtc     DATETIMEOFFSET(3) NOT NULL,

        -- Optimistic concurrency token. SQL Server maintains it; the API surfaces
        -- it as an ETag so two advisors editing the same order cannot silently
        -- overwrite each other.
        [RowVersion]   ROWVERSION       NOT NULL,

        CONSTRAINT FK_RepairJob_RepairCenter FOREIGN KEY (RepairCenterId) REFERENCES dbo.RepairCenter (RepairCenterId),
        CONSTRAINT FK_RepairJob_RepairStatus FOREIGN KEY (RepairStatusId) REFERENCES dbo.RepairStatus (RepairStatusId),

        -- The same sanity bounds the domain enforces. Validation that only lives
        -- in the application is validation that a stray script can bypass.
        CONSTRAINT CK_RepairJob_VehicleYear  CHECK (VehicleYear BETWEEN 1900 AND 2100),
        CONSTRAINT CK_RepairJob_CustomerName CHECK (LEN(LTRIM(RTRIM(CustomerName))) > 0)
    );

    CREATE UNIQUE CLUSTERED INDEX CIX_RepairJob_RepairJobKey ON dbo.RepairJob (RepairJobKey);
END
GO

------------------------------------------------------------------------------
-- Audit trail. Every accepted status change writes a row, in the same
-- transaction as the change itself.
------------------------------------------------------------------------------
IF OBJECT_ID('dbo.RepairJobStatusHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RepairJobStatusHistory
    (
        RepairJobStatusHistoryId BIGINT           NOT NULL IDENTITY(1, 1)
            CONSTRAINT PK_RepairJobStatusHistory PRIMARY KEY,
        RepairJobId              UNIQUEIDENTIFIER NOT NULL,
        FromStatusId             TINYINT          NOT NULL,
        ToStatusId               TINYINT          NOT NULL,
        ChangedUtc               DATETIMEOFFSET(3) NOT NULL,
        ChangedBy                NVARCHAR(120)    NOT NULL CONSTRAINT DF_History_ChangedBy DEFAULT ('system'),
        Note                     NVARCHAR(400)    NULL,

        CONSTRAINT FK_History_RepairJob   FOREIGN KEY (RepairJobId)  REFERENCES dbo.RepairJob (RepairJobId),
        CONSTRAINT FK_History_FromStatus  FOREIGN KEY (FromStatusId) REFERENCES dbo.RepairStatus (RepairStatusId),
        CONSTRAINT FK_History_ToStatus    FOREIGN KEY (ToStatusId)   REFERENCES dbo.RepairStatus (RepairStatusId)
    );
END
GO
