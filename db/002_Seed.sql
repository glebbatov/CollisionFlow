/*
    002_Seed.sql - reference data and the sample repair orders.

    MERGE rather than INSERT so the script is re-runnable: it converges the
    database on the intended state instead of failing on the second run.
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
-- The six approved statuses. Ids match the RepairStatus enum exactly.
------------------------------------------------------------------------------
MERGE dbo.RepairStatus AS target
USING (VALUES
    (1, 'Received',       N'Received',         0, 0),
    (2, 'InProgress',     N'In Progress',      1, 0),
    (3, 'WaitingOnParts', N'Waiting on Parts', 2, 0),
    (4, 'QualityCheck',   N'Quality Check',    3, 0),
    (5, 'ReadyForPickup', N'Ready for Pickup', 4, 0),
    (6, 'Completed',      N'Completed',        5, 1)
) AS source (RepairStatusId, Code, DisplayName, SortOrder, IsTerminal)
    ON target.RepairStatusId = source.RepairStatusId
WHEN MATCHED THEN UPDATE SET
    Code = source.Code, DisplayName = source.DisplayName,
    SortOrder = source.SortOrder, IsTerminal = source.IsTerminal
WHEN NOT MATCHED BY TARGET THEN
    INSERT (RepairStatusId, Code, DisplayName, SortOrder, IsTerminal)
    VALUES (source.RepairStatusId, source.Code, source.DisplayName, source.SortOrder, source.IsTerminal);
GO

------------------------------------------------------------------------------
-- The workflow.
--
-- These rows must match StatusTransitionPolicy.DefaultTransitions in the domain
-- exactly. The domain constant is the fallback used when the database is
-- unreachable; this table is the source of truth when it is not. An integration
-- test asserts the two agree.
--
-- WHEN NOT MATCHED BY SOURCE removes edges deleted from this list, so the table
-- converges rather than accumulating.
------------------------------------------------------------------------------
MERGE dbo.StatusTransition AS target
USING (VALUES
    (1, 2),  -- Received       -> In Progress
    (1, 3),  -- Received       -> Waiting on Parts   (parts ordered before teardown)
    (2, 3),  -- In Progress    -> Waiting on Parts
    (2, 4),  -- In Progress    -> Quality Check
    (3, 2),  -- Waiting on Parts -> In Progress      (the part landed; resume, not restart)
    (4, 2),  -- Quality Check  -> In Progress        (failed QC; rework)
    (4, 5),  -- Quality Check  -> Ready for Pickup
    (5, 6)   -- Ready for Pickup -> Completed
             -- Completed is terminal by omission.
) AS source (FromStatusId, ToStatusId)
    ON target.FromStatusId = source.FromStatusId AND target.ToStatusId = source.ToStatusId
WHEN NOT MATCHED BY TARGET THEN
    INSERT (FromStatusId, ToStatusId) VALUES (source.FromStatusId, source.ToStatusId)
WHEN NOT MATCHED BY SOURCE THEN
    DELETE;
GO

------------------------------------------------------------------------------
-- Repair centers.
------------------------------------------------------------------------------
MERGE dbo.RepairCenter AS target
USING (VALUES
    (N'Chicago - Lincoln Park',      N'Chicago',    'IL'),
    (N'Westmont',                    N'Westmont',   'IL'),
    (N'Naperville',                  N'Naperville', 'IL'),
    (N'Oak Lawn',                    N'Oak Lawn',   'IL'),
    (N'Schaumburg',                  N'Schaumburg', 'IL'),
    (N'LUXE Chicago - EV Certified', N'Chicago',    'IL')
) AS source (Name, City, [State])
    ON target.Name = source.Name
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Name, City, [State]) VALUES (source.Name, source.City, source.[State]);
GO

------------------------------------------------------------------------------
-- Sample repair orders.
--
-- MERGE matches on JobNumber, so re-running this script never re-issues an id:
-- a repair order keeps the same identifier for the life of the database, and
-- the script stays safe to run on every deployment.
--
-- Timestamps are relative to now, so the board always looks current rather than
-- frozen on the day the seed was written.
------------------------------------------------------------------------------
DECLARE @now DATETIMEOFFSET(3) = CAST(SYSUTCDATETIME() AS DATETIMEOFFSET(3));

WITH source AS (
    SELECT *
    FROM (VALUES
        ('RO-10412', N'Marcus Bell',       1969, N'Dodge',       N'Charger R/T',        N'Chicago - Lincoln Park',      1,  1,   3),
        ('RO-10413', N'Priya Raman',       1990, N'Mazda',       N'MX-5 Miata',         N'Naperville',                  1,  1,   6),
        ('RO-10414', N'Danielle Okafor',   1984, N'Jeep',        N'Cherokee XJ',        N'Schaumburg',                  1,  2,   9),
        ('RO-10435', N'Trent Osei',        2023, N'Porsche',     N'Taycan',             N'LUXE Chicago - EV Certified', 1,  0,   1),
        ('RO-10415', N'Wes Kaminski',      1967, N'Ford',        N'Mustang Fastback',   N'Oak Lawn',                    2,  3,   2),
        ('RO-10416', N'Angela Ruiz',       1998, N'Toyota',      N'Supra Turbo',        N'Westmont',                    2,  4,   5),
        ('RO-10417', N'Tomas Nowak',       1970, N'Chevrolet',   N'Chevelle SS 454',    N'Chicago - Lincoln Park',      2,  4,   1),
        ('RO-10418', N'Sandra Whitfield',  2012, N'Tesla',       N'Model S',            N'LUXE Chicago - EV Certified', 2,  5,   7),
        ('RO-10434', N'Fatima Haddad',     2004, N'Subaru',      N'Impreza WRX STI',    N'Schaumburg',                  2,  3,   1),
        ('RO-10419', N'Ibrahim Diallo',    1999, N'Nissan',      N'Skyline GT-R',       N'Naperville',                  3,  6,  20),
        ('RO-10420', N'Karen Lindqvist',   1974, N'Lamborghini', N'Countach LP400',     N'LUXE Chicago - EV Certified', 3,  7,  30),
        ('RO-10421', N'Devon Pratt',       2023, N'Rivian',      N'R1T',                N'LUXE Chicago - EV Certified', 3,  8,  44),
        ('RO-10422', N'Alicia Moreau',     1964, N'Pontiac',     N'GTO',                N'Oak Lawn',                    3,  9,  52),
        ('RO-10423', N'Hector Salinas',    1991, N'Acura',       N'NSX',                N'Westmont',                    4,  7,   4),
        ('RO-10424', N'Grace Yeoh',        1986, N'Toyota',      N'Corolla GT-S',       N'Naperville',                  4,  8,   2),
        ('RO-10425', N'Peter Nakamura',    1988, N'BMW',         N'M3',                 N'Chicago - Lincoln Park',      4,  9,  11),
        ('RO-10426', N'Renee Delacroix',   2022, N'Lucid',       N'Air Grand Touring',  N'LUXE Chicago - EV Certified', 5, 10,   5),
        ('RO-10427', N'Owen Brady',        1977, N'Porsche',     N'911 Turbo',          N'Oak Lawn',                    5, 11,   8),
        ('RO-10428', N'Simone Achebe',     1963, N'Chevrolet',   N'Corvette Sting Ray', N'Schaumburg',                  5, 12,  14),
        ('RO-10429', N'Caleb Ferreira',    1992, N'Dodge',       N'Viper RT/10',        N'Westmont',                    6, 18,  72),
        ('RO-10430', N'Nadia Petrov',      1981, N'DeLorean',    N'DMC-12',             N'Chicago - Lincoln Park',      6, 20,  96),
        ('RO-10431', N'Jerome Whitaker',   1972, N'Datsun',      N'240Z',               N'Naperville',                  6, 22, 120),
        ('RO-10432', N'Bianca Sorrentino', 2005, N'Ford',        N'GT',                 N'LUXE Chicago - EV Certified', 6, 25, 144),
        ('RO-10433', N'Louis Tremblay',    2015, N'Nissan',      N'370Z NISMO',         N'Oak Lawn',                    6, 27, 168)
    ) AS v (JobNumber, CustomerName, VehicleYear, VehicleMake, VehicleModel, CenterName, StatusId, CreatedDaysAgo, UpdatedHoursAgo)
)
MERGE dbo.RepairJob AS target
USING (
    SELECT
        RepairJobId = NEWID(),
        s.JobNumber, s.CustomerName, s.VehicleYear, s.VehicleMake, s.VehicleModel,
        c.RepairCenterId,
        RepairStatusId = s.StatusId,
        CreatedUtc = DATEADD(DAY,  -s.CreatedDaysAgo,  @now),
        UpdatedUtc = DATEADD(HOUR, -s.UpdatedHoursAgo, @now)
    FROM source s
    INNER JOIN dbo.RepairCenter c ON c.Name = s.CenterName
) AS source
    ON target.JobNumber = source.JobNumber
WHEN NOT MATCHED BY TARGET THEN
    INSERT (RepairJobId, JobNumber, CustomerName, VehicleYear, VehicleMake, VehicleModel,
            RepairCenterId, RepairStatusId, CreatedUtc, UpdatedUtc)
    VALUES (source.RepairJobId, source.JobNumber, source.CustomerName, source.VehicleYear,
            source.VehicleMake, source.VehicleModel, source.RepairCenterId, source.RepairStatusId,
            source.CreatedUtc, source.UpdatedUtc);
GO
