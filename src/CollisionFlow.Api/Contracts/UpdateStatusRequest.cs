using System.ComponentModel.DataAnnotations;
using CollisionFlow.Domain;

namespace CollisionFlow.Api.Contracts;

/// <summary>Body of a status change request.</summary>
public sealed record UpdateStatusRequest
{
    /// <summary>
    /// The status to move to, as its name - for example <c>"QualityCheck"</c>.
    /// </summary>
    /// <remarks>
    /// Nullable so that an omitted field fails validation with a clear 400 instead
    /// of silently defaulting to the first enum member. An unrecognised name is
    /// rejected by the JSON layer before it ever reaches the domain.
    /// </remarks>
    [Required(ErrorMessage = "A target status is required.")]
    public RepairStatus? Status { get; init; }
}
