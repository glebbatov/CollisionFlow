namespace CollisionFlow.Domain;

/// <summary>
/// The six statuses approved by the business. Nothing outside this set is a
/// legal state for a repair order.
/// </summary>
/// <remarks>
/// Values are assigned explicitly rather than left to the compiler. These
/// numbers become primary keys in <c>dbo.RepairStatus</c>, so they are part of
/// the persisted contract - reordering the members must never renumber them.
/// </remarks>
public enum RepairStatus
{
    Received = 1,
    InProgress = 2,
    WaitingOnParts = 3,
    QualityCheck = 4,
    ReadyForPickup = 5,
    Completed = 6,
}
