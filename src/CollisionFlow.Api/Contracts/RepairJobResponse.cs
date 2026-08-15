using CollisionFlow.Domain;

namespace CollisionFlow.Api.Contracts;

/// <summary>What the API returns for a repair order.</summary>
/// <remarks>
/// A separate shape from the domain entity on purpose. The entity is free to grow
/// private state and behavior without changing the wire contract, and nothing
/// internal leaks to a client simply because someone added a property.
/// </remarks>
public sealed record RepairJobResponse
{
    public required Guid Id { get; init; }

    /// <summary>The number a service advisor reads over the phone, e.g. "RO-10428".</summary>
    public required string JobNumber { get; init; }

    public required string CustomerName { get; init; }

    public required int VehicleYear { get; init; }

    public required string VehicleMake { get; init; }

    public required string VehicleModel { get; init; }

    /// <summary>Year, make and model pre-joined for display, e.g. "2021 Toyota RAV4".</summary>
    public required string VehicleDescription { get; init; }

    public required string RepairCenter { get; init; }

    public required RepairStatus Status { get; init; }

    /// <summary>The label the business uses, e.g. "Waiting on Parts".</summary>
    public required string StatusDisplayName { get; init; }

    /// <summary>
    /// Statuses this job may legally move to right now.
    /// </summary>
    /// <remarks>
    /// Sent with every job so the client can render only the moves that are valid.
    /// The rule stays owned by the server; the UI just stops offering what would be
    /// rejected anyway.
    /// </remarks>
    public required IReadOnlyList<RepairStatus> AllowedTransitions { get; init; }

    public required DateTimeOffset CreatedUtc { get; init; }

    public required DateTimeOffset UpdatedUtc { get; init; }
}
