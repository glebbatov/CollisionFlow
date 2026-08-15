using CollisionFlow.Infrastructure.Data;
using Microsoft.Extensions.Options;

namespace CollisionFlow.Infrastructure;

/// <summary>Which store is currently answering requests.</summary>
public enum DataSourceKind
{
    /// <summary>The SQL database - the authoritative store.</summary>
    Database = 0,

    /// <summary>The in-process fallback. Reads are sample data; writes do not survive a restart.</summary>
    InMemory = 1,
}

/// <summary>Reports which store is serving requests, so the UI can say so plainly.</summary>
public interface IDataSourceStatus
{
    DataSourceKind Current { get; }

    /// <summary>True when the application is not serving from its authoritative store.</summary>
    bool IsDegraded { get; }

    /// <summary>When the current state began.</summary>
    DateTimeOffset Since { get; }
}

/// <summary>
/// Tracks whether the application is serving from the database or from the fallback.
/// </summary>
/// <remarks>
/// This exists so degradation is <i>visible</i>. An application that quietly serves stale
/// sample data while its database is unreachable is worse than one that fails, because
/// nobody finds out until the numbers are wrong. Publishing the state lets the UI say
/// "these changes are temporary" instead of implying they were saved.
/// </remarks>
public sealed class DataSourceStatus : IDataSourceStatus
{
    private readonly TimeProvider _clock;
    private readonly object _gate = new();

    private DataSourceKind _current;
    private DateTimeOffset _since;

    public DataSourceStatus(TimeProvider clock, IOptions<DatabaseOptions> databaseOptions)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(databaseOptions);

        _clock = clock;
        _since = clock.GetUtcNow();

        // With no connection string the in-memory store is the intended store, not a
        // degradation - so this does not start out reporting a problem.
        _current = databaseOptions.Value.IsConfigured
            ? DataSourceKind.Database
            : DataSourceKind.InMemory;

        _configuredForDatabase = databaseOptions.Value.IsConfigured;
    }

    private readonly bool _configuredForDatabase;

    public DataSourceKind Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public bool IsDegraded => _configuredForDatabase && Current is not DataSourceKind.Database;

    public DateTimeOffset Since
    {
        get
        {
            lock (_gate)
            {
                return _since;
            }
        }
    }

    /// <summary>Records that the application is now serving from a different store.</summary>
    /// <returns><c>true</c> when this call changed the state, so callers log a transition rather than every request.</returns>
    public bool MoveTo(DataSourceKind kind)
    {
        lock (_gate)
        {
            if (_current == kind)
            {
                return false;
            }

            _current = kind;
            _since = _clock.GetUtcNow();
            return true;
        }
    }
}
