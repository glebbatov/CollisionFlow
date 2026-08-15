using System.Collections.Concurrent;
using CollisionFlow.Domain;
using CollisionFlow.Domain.Abstractions;

namespace CollisionFlow.Infrastructure.Repositories;

/// <summary>
/// An in-process repair order store backed by the sample data set.
/// </summary>
/// <remarks>
/// This is not a throwaway stub. It is the fallback the application degrades to
/// when Azure SQL is paused or unreachable, and it is the store the unit tests run
/// against, so it has to honour the same contract as the SQL implementation -
/// including refusing illegal transitions.
/// </remarks>
public sealed class InMemoryRepairJobRepository : IRepairJobRepository
{
    private readonly ConcurrentDictionary<Guid, RepairJob> _jobs;
    private readonly IStatusTransitionPolicy _policy;
    private readonly TimeProvider _clock;
    private readonly object _writeGate = new();

    public InMemoryRepairJobRepository(IStatusTransitionPolicy policy, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(clock);

        _policy = policy;
        _clock = clock;
        _jobs = new ConcurrentDictionary<Guid, RepairJob>(
            SeedRepairJobs.Create(clock.GetUtcNow()).ToDictionary(j => j.Id));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RepairJob>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<RepairJob> ordered = _jobs.Values
            .OrderByDescending(j => j.UpdatedUtc)
            .ToArray();

        return Task.FromResult(ordered);
    }

    /// <inheritdoc />
    public Task<RepairJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_jobs.TryGetValue(id, out var job) ? job : null);
    }

    /// <inheritdoc />
    public Task<RepairJob?> UpdateStatusAsync(
        Guid id,
        RepairStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Repair orders are mutable aggregates held by reference, so two concurrent
        // advisors could interleave a read and a write. The SQL implementation solves
        // this with a rowversion check inside the stored procedure; in memory, a single
        // write gate is the honest equivalent.
        lock (_writeGate)
        {
            if (!_jobs.TryGetValue(id, out var job))
            {
                return Task.FromResult<RepairJob?>(null);
            }

            job.ChangeStatus(newStatus, _policy, _clock.GetUtcNow());
            return Task.FromResult<RepairJob?>(job);
        }
    }
}
