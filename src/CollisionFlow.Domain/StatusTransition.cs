namespace CollisionFlow.Domain;

/// <summary>A single legal edge in the repair workflow: <paramref name="From"/> may become <paramref name="To"/>.</summary>
public readonly record struct StatusTransition(RepairStatus From, RepairStatus To);
