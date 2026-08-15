namespace CollisionFlow.Domain;

/// <summary>
/// The vehicle under repair. A value object: two vehicles with the same year,
/// make and model are the same vehicle description, and none of it can change
/// after construction.
/// </summary>
public sealed record Vehicle
{
    /// <summary>Lower sanity bound. Older than this is a data-entry error, not a collision repair.</summary>
    public const int EarliestSupportedYear = 1900;

    /// <summary>Upper sanity bound. Kept as a constant so validation stays deterministic and testable.</summary>
    public const int LatestSupportedYear = 2100;

    public Vehicle(int year, string make, string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(make);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        if (year is < EarliestSupportedYear or > LatestSupportedYear)
        {
            throw new ArgumentOutOfRangeException(
                nameof(year), year,
                $"Vehicle year must be between {EarliestSupportedYear} and {LatestSupportedYear}.");
        }

        Year = year;
        Make = make.Trim();
        Model = model.Trim();
    }

    public int Year { get; }

    public string Make { get; }

    public string Model { get; }

    /// <summary>How a service advisor would say it out loud: "2022 Toyota RAV4".</summary>
    public string Description => $"{Year} {Make} {Model}";

    public override string ToString() => Description;
}
