using CollisionFlow.Domain;

namespace CollisionFlow.Api.Contracts;

/// <summary>Maps domain objects to wire contracts.</summary>
/// <remarks>
/// Hand-written rather than reflection-based. At this size a mapping library would
/// add a dependency, a startup cost and a class of runtime-only failures, in
/// exchange for saving about thirty lines that the compiler currently checks.
/// </remarks>
internal static class ContractMappings
{
    public static RepairJobResponse ToResponse(this RepairJob job, IStatusTransitionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(policy);

        return new RepairJobResponse
        {
            Id = job.Id,
            JobNumber = job.JobNumber,
            CustomerName = job.CustomerName,
            VehicleYear = job.Vehicle.Year,
            VehicleMake = job.Vehicle.Make,
            VehicleModel = job.Vehicle.Model,
            VehicleDescription = job.Vehicle.Description,
            RepairCenter = job.RepairCenter,
            Status = job.Status,
            StatusDisplayName = RepairStatusInfo.DisplayName(job.Status),
            AllowedTransitions = policy.AllowedNextFrom(job.Status),
            CreatedUtc = job.CreatedUtc,
            UpdatedUtc = job.UpdatedUtc,
        };
    }

    public static StatusChangeResponse ToResponse(this StatusChange change)
    {
        ArgumentNullException.ThrowIfNull(change);

        return new StatusChangeResponse
        {
            From = change.From,
            FromDisplayName = RepairStatusInfo.DisplayName(change.From),
            To = change.To,
            ToDisplayName = RepairStatusInfo.DisplayName(change.To),
            ChangedUtc = change.ChangedUtc,
            ChangedBy = change.ChangedBy,
            Note = change.Note,
        };
    }

    public static RepairStatusResponse ToResponse(this RepairStatus status, IStatusTransitionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return new RepairStatusResponse
        {
            Status = status,
            DisplayName = RepairStatusInfo.DisplayName(status),
            SortOrder = RepairStatusInfo.SortOrder(status),
            IsTerminal = RepairStatusInfo.IsTerminal(status),
            AllowedTransitions = policy.AllowedNextFrom(status),
        };
    }
}
