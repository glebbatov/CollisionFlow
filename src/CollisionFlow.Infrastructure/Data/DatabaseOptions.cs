namespace CollisionFlow.Infrastructure.Data;

/// <summary>Database configuration, bound from the <c>Database</c> configuration section.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Connection string. When empty the application runs entirely on the in-memory
    /// repository - which is what makes it possible to clone the repository and press
    /// F5 without installing SQL Server first.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Apply the scripts in <c>db/</c> at startup. They are idempotent, so this is safe
    /// to leave on; it keeps a fresh environment one run away from working.
    /// </summary>
    public bool InitializeSchema { get; set; } = true;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);
}
