namespace CollisionFlow.Domain;

/// <summary>
/// Presentation-neutral vocabulary for <see cref="RepairStatus"/>: the wording
/// the business uses, the order work flows in, and which state ends the job.
/// </summary>
public static class RepairStatusInfo
{
    private static readonly RepairStatus[] Ordered =
    [
        RepairStatus.Received,
        RepairStatus.InProgress,
        RepairStatus.WaitingOnParts,
        RepairStatus.QualityCheck,
        RepairStatus.ReadyForPickup,
        RepairStatus.Completed,
    ];

    /// <summary>Statuses in the order work normally flows through the shop.</summary>
    public static IReadOnlyList<RepairStatus> InWorkflowOrder => Ordered;

    /// <summary>Zero-based position in the workflow. Drives display ordering everywhere.</summary>
    public static int SortOrder(RepairStatus status) => Array.IndexOf(Ordered, status);

    /// <summary>The label the business uses, exactly as written in the requirements.</summary>
    public static string DisplayName(RepairStatus status) => status switch
    {
        RepairStatus.Received => "Received",
        RepairStatus.InProgress => "In Progress",
        RepairStatus.WaitingOnParts => "Waiting on Parts",
        RepairStatus.QualityCheck => "Quality Check",
        RepairStatus.ReadyForPickup => "Ready for Pickup",
        RepairStatus.Completed => "Completed",

        // Deliberately exhaustive: adding a status without adding its label
        // fails here loudly instead of rendering an enum name to a customer.
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unmapped repair status."),
    };

    /// <summary>A terminal status has no legal transitions out of it.</summary>
    public static bool IsTerminal(RepairStatus status) => status is RepairStatus.Completed;
}
