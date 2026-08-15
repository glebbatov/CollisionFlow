using CollisionFlow.Domain;

namespace CollisionFlow.Infrastructure.Workflow;

/// <summary>
/// The application's live workflow policy.
/// </summary>
/// <remarks>
/// Starts as <see cref="StatusTransitionPolicy.Default"/> - the workflow compiled into
/// the domain - and is replaced with the database's version once it can be read. That
/// ordering matters: the application is never in a state where it has no rules, so a
/// database that is slow, paused or unreachable at startup degrades to a correct policy
/// rather than to no policy.
/// </remarks>
public sealed class MutableStatusTransitionPolicy : IStatusTransitionPolicy
{
    private volatile IStatusTransitionPolicy _current = StatusTransitionPolicy.Default;

    /// <summary>True once the workflow has been adopted from the database.</summary>
    public bool LoadedFromDatabase { get; private set; }

    public void ReplaceWith(IStatusTransitionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        _current = policy;
        LoadedFromDatabase = true;
    }

    /// <inheritdoc />
    public bool IsAllowed(RepairStatus from, RepairStatus to) => _current.IsAllowed(from, to);

    /// <inheritdoc />
    public IReadOnlyList<RepairStatus> AllowedNextFrom(RepairStatus current) =>
        _current.AllowedNextFrom(current);
}
