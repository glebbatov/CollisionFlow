using CollisionFlow.Domain;

namespace CollisionFlow.Api.Contracts;

/// <summary>A status in the workflow, with everything a client needs to render it.</summary>
public sealed record RepairStatusResponse
{
    public required RepairStatus Status { get; init; }

    public required string DisplayName { get; init; }

    /// <summary>Position in the workflow, for ordering a pipeline view.</summary>
    public required int SortOrder { get; init; }

    /// <summary>True when no further transitions are possible.</summary>
    public required bool IsTerminal { get; init; }

    /// <summary>Statuses reachable in one step from this one.</summary>
    public required IReadOnlyList<RepairStatus> AllowedTransitions { get; init; }
}
