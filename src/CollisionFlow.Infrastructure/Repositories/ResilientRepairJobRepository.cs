using CollisionFlow.Domain;
using CollisionFlow.Domain.Abstractions;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace CollisionFlow.Infrastructure.Repositories;

/// <summary>
/// Serves from SQL, and from the in-memory store when SQL cannot be reached.
/// </summary>
/// <remarks>
/// <para>
/// A decorator rather than a branch inside the SQL repository: neither implementation
/// knows this class exists, and both can be tested in isolation against the same
/// interface. Adding resilience did not require editing either of them.
/// </para>
/// <para>
/// The deployed database is Azure SQL's free tier, which auto-pauses after an hour of
/// inactivity and takes up to a minute to resume. Without this, someone opening the link
/// the morning after it was sent gets a timeout. With it, they get the board, plus a
/// banner telling them exactly what they are looking at.
/// </para>
/// </remarks>
public sealed class ResilientRepairJobRepository : IRepairJobRepository
{
    private readonly SqlRepairJobRepository _database;
    private readonly InMemoryRepairJobRepository _fallback;
    private readonly DataSourceStatus _status;
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<ResilientRepairJobRepository> _logger;

    public ResilientRepairJobRepository(
        SqlRepairJobRepository database,
        InMemoryRepairJobRepository fallback,
        DataSourceStatus status,
        ILogger<ResilientRepairJobRepository> logger)
    {
        _database = database;
        _fallback = fallback;
        _status = status;
        _logger = logger;

        _pipeline = new ResiliencePipelineBuilder()
            // Five seconds is the point at which a person decides the page is broken.
            // A paused serverless database takes far longer than that to resume, so
            // waiting for it is worse than answering from the fallback and recovering later.
            .AddTimeout(TimeSpan.FromSeconds(5))
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 2,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(20),

                // A rejected status change is a business outcome, not an outage. Without
                // this, a user repeatedly attempting an illegal transition would trip the
                // breaker and take the database offline for everyone else.
                ShouldHandle = new PredicateBuilder()
                    .Handle<Exception>(ex => ex is not DomainException),
            })
            .Build();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RepairJob>> GetAllAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            nameof(GetAllAsync),
            ct => _database.GetAllAsync(ct),
            ct => _fallback.GetAllAsync(ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<RepairJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            nameof(GetByIdAsync),
            ct => _database.GetByIdAsync(id, ct),
            ct => _fallback.GetByIdAsync(id, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<RepairJob?> UpdateStatusAsync(
        Guid id,
        RepairStatus newStatus,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            nameof(UpdateStatusAsync),
            ct => _database.UpdateStatusAsync(id, newStatus, ct),
            ct => _fallback.UpdateStatusAsync(id, newStatus, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<StatusChange>> GetStatusHistoryAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            nameof(GetStatusHistoryAsync),
            ct => _database.GetStatusHistoryAsync(id, ct),
            ct => _fallback.GetStatusHistoryAsync(id, ct),
            cancellationToken);

    private async Task<T> ExecuteAsync<T>(
        string operation,
        Func<CancellationToken, Task<T>> onDatabase,
        Func<CancellationToken, Task<T>> onFallback,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _pipeline.ExecuteAsync(
                async ct => await onDatabase(ct),
                cancellationToken);

            if (_status.MoveTo(DataSourceKind.Database))
            {
                _logger.LogInformation("Database reachable again; serving from SQL.");
            }

            return result;
        }
        catch (DomainException)
        {
            // The database was reachable and said no. That answer is correct and must
            // reach the caller unchanged - retrying it against the fallback would let a
            // rejected change appear to succeed.
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller went away. Not a failure of anything.
            throw;
        }
        catch (Exception ex)
        {
            if (_status.MoveTo(DataSourceKind.InMemory))
            {
                _logger.LogError(
                    ex,
                    "Database unavailable during {Operation}; serving from the in-memory fallback.",
                    operation);
            }
            else if (ex is not BrokenCircuitException)
            {
                _logger.LogDebug(ex, "Database still unavailable during {Operation}.", operation);
            }

            return await onFallback(cancellationToken);
        }
    }
}
