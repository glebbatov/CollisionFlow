using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace CollisionFlow.Infrastructure.Data;

/// <inheritdoc />
public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly DatabaseOptions _options;

    public SqlConnectionFactory(IOptions<DatabaseOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <summary>
    /// Returns a closed connection. ADO.NET pools the underlying physical connections,
    /// so creating one per operation is the cheap, correct pattern - holding a long-lived
    /// connection open would defeat the pool and serialize unrelated requests.
    /// </summary>
    public DbConnection Create() => new SqlConnection(_options.ConnectionString);
}
