namespace CollisionFlow.Domain;

/// <summary>
/// Answers which status changes the business permits.
/// </summary>
/// <remarks>
/// This is an interface rather than a static helper because the set of legal
/// transitions is data, not code. In development it comes from
/// <see cref="StatusTransitionPolicy.Default"/>; in production it is loaded from
/// <c>dbo.StatusTransition</c>. Same rules, one source of truth, no redeploy to
/// change the workflow.
/// </remarks>
public interface IStatusTransitionPolicy
{
    /// <summary>True when a repair order may move directly from one status to another.</summary>
    bool IsAllowed(RepairStatus from, RepairStatus to);

    /// <summary>Every status reachable in one step from <paramref name="current"/>, in workflow order.</summary>
    IReadOnlyList<RepairStatus> AllowedNextFrom(RepairStatus current);
}
