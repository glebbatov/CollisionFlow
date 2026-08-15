namespace CollisionFlow.Domain;

/// <summary>
/// One recorded movement of a repair order through the workflow.
/// </summary>
/// <remarks>
/// Written in the same transaction as the status change itself, so there is no state in
/// which a repair order has moved without a record of why. In a collision network that
/// matters beyond tidiness: "when did this go to Ready for Pickup, and who said so" is a
/// question insurers and customers actually ask.
/// </remarks>
public sealed record StatusChange(
    RepairStatus From,
    RepairStatus To,
    DateTimeOffset ChangedUtc,
    string ChangedBy,
    string? Note);
