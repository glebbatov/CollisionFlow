using System.Data.Common;

namespace CollisionFlow.Infrastructure.Data;

/// <summary>
/// Creates database connections.
/// </summary>
/// <remarks>
/// An interface rather than injecting the connection string everywhere: repositories
/// ask for a connection and never learn where the server is, which keeps credentials
/// in one place and makes the repositories trivially testable against any provider.
/// </remarks>
public interface ISqlConnectionFactory
{
    DbConnection Create();
}
