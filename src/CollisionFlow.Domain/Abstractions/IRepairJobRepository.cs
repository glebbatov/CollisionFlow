namespace CollisionFlow.Domain.Abstractions;

/// <summary>
/// How the application reads and writes repair orders.
/// </summary>
/// <remarks>
/// This interface lives in the domain, not in the infrastructure that implements
/// it. That inverts the dependency: storage depends on the business, the business
/// does not depend on storage. It is what lets the same API run against stored
/// procedures in Azure and against an in-memory list in a unit test, with neither
/// the controllers nor the domain knowing which is in play.
/// </remarks>
public interface IRepairJobRepository
{
    /// <summary>Every repair order, newest activity first.</summary>
    Task<IReadOnlyList<RepairJob>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>A single repair order, or <c>null</c> when no such order exists.</summary>
    Task<RepairJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a status change and persists it.
    /// </summary>
    /// <returns>The updated repair order, or <c>null</c> when no such order exists.</returns>
    /// <exception cref="InvalidStatusTransitionException">The workflow does not allow this move.</exception>
    Task<RepairJob?> UpdateStatusAsync(
        Guid id,
        RepairStatus newStatus,
        CancellationToken cancellationToken = default);

    /// <summary>Every recorded status change for a repair order, newest first.</summary>
    Task<IReadOnlyList<StatusChange>> GetStatusHistoryAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
