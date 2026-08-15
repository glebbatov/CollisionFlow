using CollisionFlow.Domain;

namespace CollisionFlow.Infrastructure.Repositories;

/// <summary>
/// The sample data set. Deliberately hand-written rather than randomly generated.
/// Every status is represented, the whole set is deterministic so tests and
/// screenshots do not shift underneath us, and the vehicle mix spans what a
/// national collision network actually sees - muscle, JDM, European classics and
/// current EVs - with highline and electric work routed to the LUXE certified
/// location, mirroring how Crash Champions splits that work today.
/// </summary>
public static class SeedRepairJobs
{
    private sealed record Row(
        string JobNumber,
        string Customer,
        int Year,
        string Make,
        string Model,
        string Center,
        RepairStatus Status,
        int CreatedDaysAgo,
        int UpdatedHoursAgo);

    private static readonly Row[] Rows =
    [
        // Intake
        new("RO-10412", "Marcus Bell",       1969, "Dodge",      "Charger R/T",        "Chicago - Lincoln Park",      RepairStatus.Received,        1,  3),
        new("RO-10413", "Priya Raman",       1990, "Mazda",      "MX-5 Miata",         "Naperville",                  RepairStatus.Received,        1,  6),
        new("RO-10414", "Danielle Okafor",   1984, "Jeep",       "Cherokee XJ",        "Schaumburg",                  RepairStatus.Received,        2,  9),
        new("RO-10435", "Trent Osei",        2023, "Porsche",    "Taycan",             "LUXE Chicago - EV Certified", RepairStatus.Received,        0,  1),

        // On the lift
        new("RO-10415", "Wes Kaminski",      1967, "Ford",       "Mustang Fastback",   "Oak Lawn",                    RepairStatus.InProgress,      3,  2),
        new("RO-10416", "Angela Ruiz",       1998, "Toyota",     "Supra Turbo",        "Westmont",                    RepairStatus.InProgress,      4,  5),
        new("RO-10417", "Tomas Nowak",       1970, "Chevrolet",  "Chevelle SS 454",    "Chicago - Lincoln Park",      RepairStatus.InProgress,      4,  1),
        new("RO-10418", "Sandra Whitfield",  2012, "Tesla",      "Model S",            "LUXE Chicago - EV Certified", RepairStatus.InProgress,      5,  7),
        new("RO-10434", "Fatima Haddad",     2004, "Subaru",     "Impreza WRX STI",    "Schaumburg",                  RepairStatus.InProgress,      3,  1),

        // Parts holds - the ones where sourcing is the whole problem
        new("RO-10419", "Ibrahim Diallo",    1999, "Nissan",     "Skyline GT-R",       "Naperville",                  RepairStatus.WaitingOnParts,  6,  20),
        new("RO-10420", "Karen Lindqvist",   1974, "Lamborghini","Countach LP400",     "LUXE Chicago - EV Certified", RepairStatus.WaitingOnParts,  7,  30),
        new("RO-10421", "Devon Pratt",       2023, "Rivian",     "R1T",                "LUXE Chicago - EV Certified", RepairStatus.WaitingOnParts,  8,  44),
        new("RO-10422", "Alicia Moreau",     1964, "Pontiac",    "GTO",                "Oak Lawn",                    RepairStatus.WaitingOnParts,  9,  52),

        // Under inspection
        new("RO-10423", "Hector Salinas",    1991, "Acura",      "NSX",                "Westmont",                    RepairStatus.QualityCheck,    7,  4),
        new("RO-10424", "Grace Yeoh",        1986, "Toyota",     "Corolla GT-S",       "Naperville",                  RepairStatus.QualityCheck,    8,  2),
        new("RO-10425", "Peter Nakamura",    1988, "BMW",        "M3",                 "Chicago - Lincoln Park",      RepairStatus.QualityCheck,    9,  11),

        // Waiting on the customer
        new("RO-10426", "Renee Delacroix",   2022, "Lucid",      "Air Grand Touring",  "LUXE Chicago - EV Certified", RepairStatus.ReadyForPickup,  10, 5),
        new("RO-10427", "Owen Brady",        1977, "Porsche",    "911 Turbo",          "Oak Lawn",                    RepairStatus.ReadyForPickup,  11, 8),
        new("RO-10428", "Simone Achebe",     1963, "Chevrolet",  "Corvette Sting Ray", "Schaumburg",                  RepairStatus.ReadyForPickup,  12, 14),

        // Closed
        new("RO-10429", "Caleb Ferreira",    1992, "Dodge",      "Viper RT/10",        "Westmont",                    RepairStatus.Completed,       18, 72),
        new("RO-10430", "Nadia Petrov",      1981, "DeLorean",   "DMC-12",             "Chicago - Lincoln Park",      RepairStatus.Completed,       20, 96),
        new("RO-10431", "Jerome Whitaker",   1972, "Datsun",     "240Z",               "Naperville",                  RepairStatus.Completed,       22, 120),
        new("RO-10432", "Bianca Sorrentino", 2005, "Ford",       "GT",                 "LUXE Chicago - EV Certified", RepairStatus.Completed,       25, 144),
        new("RO-10433", "Louis Tremblay",    2015, "Nissan",     "370Z NISMO",         "Oak Lawn",                    RepairStatus.Completed,       27, 168),
    ];

    /// <summary>Builds the sample set relative to a supplied "now", so timestamps always look current.</summary>
    public static IReadOnlyList<RepairJob> Create(DateTimeOffset nowUtc) =>
        Rows.Select(r => RepairJob.Rehydrate(
                id: DeterministicId(r.JobNumber),
                jobNumber: r.JobNumber,
                customerName: r.Customer,
                vehicle: new Vehicle(r.Year, r.Make, r.Model),
                repairCenter: r.Center,
                status: r.Status,
                createdUtc: nowUtc.AddDays(-r.CreatedDaysAgo),
                updatedUtc: nowUtc.AddHours(-r.UpdatedHoursAgo)))
            .ToArray();

    /// <summary>
    /// Derives a stable GUID from the repair order number so that a restart does not
    /// invalidate every link, bookmark or test fixture that referenced a job by id.
    /// </summary>
    private static Guid DeterministicId(string jobNumber)
    {
        var bytes = new byte[16];
        var source = System.Text.Encoding.UTF8.GetBytes(jobNumber);
        for (var i = 0; i < source.Length && i < bytes.Length; i++)
        {
            bytes[i] = source[i];
        }

        return new Guid(bytes);
    }
}
