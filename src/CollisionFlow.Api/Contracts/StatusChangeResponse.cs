using CollisionFlow.Domain;

namespace CollisionFlow.Api.Contracts;

/// <summary>One entry in a repair order's audit trail.</summary>
public sealed record StatusChangeResponse
{
    public required RepairStatus From { get; init; }

    public required string FromDisplayName { get; init; }

    public required RepairStatus To { get; init; }

    public required string ToDisplayName { get; init; }

    public required DateTimeOffset ChangedUtc { get; init; }

    public required string ChangedBy { get; init; }

    public string? Note { get; init; }
}
