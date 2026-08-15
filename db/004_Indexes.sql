/*
    004_Indexes.sql

    One index per query that actually runs, with the query named. An index nobody
    can point at is write cost with no reader.
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
-- Serves usp_RepairJob_GetAll: filter by status, order by UpdatedUtc descending.
-- The INCLUDE list covers the projection, so the board renders from the index
-- without touching the clustered index at all.
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RepairJob_Status_Updated' AND object_id = OBJECT_ID('dbo.RepairJob'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RepairJob_Status_Updated
        ON dbo.RepairJob (RepairStatusId, UpdatedUtc DESC)
        INCLUDE (RepairJobId, JobNumber, CustomerName, VehicleYear, VehicleMake,
                 VehicleModel, RepairCenterId, CreatedUtc);
END
GO

------------------------------------------------------------------------------
-- Serves the per-location view: "everything at Naperville right now."
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RepairJob_Center_Updated' AND object_id = OBJECT_ID('dbo.RepairJob'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RepairJob_Center_Updated
        ON dbo.RepairJob (RepairCenterId, UpdatedUtc DESC);
END
GO

------------------------------------------------------------------------------
-- Filtered index over open work only.
--
-- A collision center accumulates completed orders indefinitely but only ever
-- works on the open ones. Excluding Completed (status 6) keeps this index small
-- and stable no matter how much history the table carries - the working set stops
-- growing even though the table does not.
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RepairJob_Open' AND object_id = OBJECT_ID('dbo.RepairJob'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RepairJob_Open
        ON dbo.RepairJob (UpdatedUtc DESC)
        INCLUDE (RepairJobId, JobNumber, RepairStatusId, RepairCenterId)
        WHERE RepairStatusId <> 6;
END
GO

------------------------------------------------------------------------------
-- Serves usp_RepairJob_GetStatusHistory.
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_History_Job_Changed' AND object_id = OBJECT_ID('dbo.RepairJobStatusHistory'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_History_Job_Changed
        ON dbo.RepairJobStatusHistory (RepairJobId, ChangedUtc DESC);
END
GO
