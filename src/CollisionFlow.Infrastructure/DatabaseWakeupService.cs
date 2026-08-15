using CollisionFlow.Infrastructure.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CollisionFlow.Infrastructure;

/// <summary>
/// Knocks on the database's door while the application is serving from the fallback.
/// </summary>
/// <remarks>
/// <para>
/// A serverless database that has auto-paused takes far longer to resume than any request
/// is willing to wait. The repository's five-second budget therefore can never wake it -
/// every request times out, falls back, and leaves the database asleep. Without something
/// like this the application would degrade once and stay degraded forever.
/// </para>
/// <para>
/// So the probe uses a deliberately generous timeout, because unlike a user request it has
/// nowhere to be. Once it succeeds the database is warm, the circuit breaker's next trial
/// request lands on a live server, and normal service resumes on its own.
/// </para>
/// <para>
/// It runs <b>only while degraded</b>, and that restraint is the whole point. The free tier
/// allows 100,000 vCore-seconds a month; at the 0.5 vCore floor, a probe that kept the
/// database permanently awake would exhaust a month's allowance in under three days. Waking
/// it on demand costs seconds. Keeping it awake costs the offer.
/// </para>
/// </remarks>
public sealed class DatabaseWakeupService : BackgroundService
{
    private static readonly TimeSpan ProbeInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromMinutes(2);

    private readonly ISqlConnectionFactory _connections;
    private readonly IDataSourceStatus _status;
    private readonly ILogger<DatabaseWakeupService> _logger;

    public DatabaseWakeupService(
        ISqlConnectionFactory connections,
        IDataSourceStatus status,
        ILogger<DatabaseWakeupService> logger)
    {
        _connections = connections;
        _status = status;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(ProbeInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                if (!_status.IsDegraded)
                {
                    continue;
                }

                await ProbeAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // The host is shutting down.
        }
    }

    private async Task ProbeAsync(CancellationToken stoppingToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        cancellation.CancelAfter(ProbeTimeout);

        try
        {
            await using var connection = _connections.Create();
            await connection.OpenAsync(cancellation.Token);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellation.Token);

            _logger.LogInformation(
                "Database answered the wake-up probe; it should serve the next request.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug(ex, "Database is still unavailable.");
        }
    }
}
