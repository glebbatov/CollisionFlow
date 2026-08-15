namespace CollisionFlow.Domain;

/// <summary>
/// The repair workflow as a directed graph, built from a set of legal edges.
/// </summary>
/// <remarks>
/// <para>
/// The graph is intentionally not a straight line. Two real-world shop
/// behaviors shape it:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>Parts holds are reversible.</b> A job waiting on a back-ordered bumper
///     returns to In Progress when the part lands - it does not restart.
///   </description></item>
///   <item><description>
///     <b>Quality Check can fail.</b> A job that does not pass QC goes back to
///     In Progress for rework rather than forward to the customer. Modeling this
///     rework loop is the difference between a workflow and a progress bar.
///   </description></item>
/// </list>
/// </remarks>
public sealed class StatusTransitionPolicy : IStatusTransitionPolicy
{
    private static readonly IReadOnlyList<RepairStatus> None = [];

    private readonly Dictionary<RepairStatus, IReadOnlyList<RepairStatus>> _allowedNext;

    /// <summary>Builds a policy from an arbitrary edge set - for example, rows read from the database.</summary>
    public StatusTransitionPolicy(IEnumerable<StatusTransition> transitions)
    {
        ArgumentNullException.ThrowIfNull(transitions);

        _allowedNext = transitions
            .GroupBy(t => t.From)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<RepairStatus>)g
                    .Select(t => t.To)
                    .Distinct()
                    .OrderBy(RepairStatusInfo.SortOrder)
                    .ToArray());
    }

    /// <summary>
    /// The canonical edge set. <c>db/002_Seed.sql</c> inserts exactly these rows,
    /// so the in-memory and SQL-backed policies cannot disagree.
    /// </summary>
    /// <remarks>
    /// Declared before <see cref="Default"/> deliberately. Static initializers run in
    /// textual order, so a <see cref="Default"/> declared first would be constructed
    /// from a still-null list and throw on first use.
    /// </remarks>
    public static IReadOnlyList<StatusTransition> DefaultTransitions { get; } =
    [
        // Intake: work can start, or parts can be ordered before teardown begins.
        new(RepairStatus.Received, RepairStatus.InProgress),
        new(RepairStatus.Received, RepairStatus.WaitingOnParts),

        // Active repair: stall on parts, or hand off to QC.
        new(RepairStatus.InProgress, RepairStatus.WaitingOnParts),
        new(RepairStatus.InProgress, RepairStatus.QualityCheck),

        // The part arrived - resume where we left off.
        new(RepairStatus.WaitingOnParts, RepairStatus.InProgress),

        // QC either passes the job forward or sends it back for rework.
        new(RepairStatus.QualityCheck, RepairStatus.InProgress),
        new(RepairStatus.QualityCheck, RepairStatus.ReadyForPickup),

        // The customer collects the vehicle.
        new(RepairStatus.ReadyForPickup, RepairStatus.Completed),

        // Completed is terminal by omission. Reopening a closed repair order is a
        // supplement in the real business process, not an edit to history.
    ];

    /// <summary>The canonical workflow, used when no database-backed policy is available.</summary>
    public static StatusTransitionPolicy Default { get; } = new(DefaultTransitions);

    /// <inheritdoc />
    public bool IsAllowed(RepairStatus from, RepairStatus to) =>
        _allowedNext.TryGetValue(from, out var next) && next.Contains(to);

    /// <inheritdoc />
    public IReadOnlyList<RepairStatus> AllowedNextFrom(RepairStatus current) =>
        _allowedNext.TryGetValue(current, out var next) ? next : None;
}
