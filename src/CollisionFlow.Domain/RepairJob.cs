namespace CollisionFlow.Domain;

/// <summary>
/// A repair order: one vehicle, at one repair center, moving through the workflow.
/// </summary>
/// <remarks>
/// <see cref="Status"/> has a private setter and the only way to move it is
/// <see cref="ChangeStatus"/>. There is no code path that can put a repair order
/// into an illegal state, because there is no code path that can set the status
/// without consulting the policy.
/// </remarks>
public sealed class RepairJob
{
    private RepairJob(
        Guid id,
        string jobNumber,
        string customerName,
        Vehicle vehicle,
        string repairCenter,
        RepairStatus status,
        DateTimeOffset createdUtc,
        DateTimeOffset updatedUtc)
    {
        Id = id;
        JobNumber = jobNumber;
        CustomerName = customerName;
        Vehicle = vehicle;
        RepairCenter = repairCenter;
        Status = status;
        CreatedUtc = createdUtc;
        UpdatedUtc = updatedUtc;
    }

    /// <summary>Stable identifier. A GUID so the client can reference a job without leaking row counts.</summary>
    public Guid Id { get; }

    /// <summary>The repair order number a service advisor would read over the phone, e.g. "RO-10428".</summary>
    public string JobNumber { get; }

    public string CustomerName { get; }

    public Vehicle Vehicle { get; }

    public string RepairCenter { get; }

    public RepairStatus Status { get; private set; }

    public DateTimeOffset CreatedUtc { get; }

    public DateTimeOffset UpdatedUtc { get; private set; }

    /// <summary>
    /// Opens a new repair order. New work always starts at
    /// <see cref="RepairStatus.Received"/> - that is not a caller's choice.
    /// </summary>
    public static RepairJob Open(
        string jobNumber,
        string customerName,
        Vehicle vehicle,
        string repairCenter,
        DateTimeOffset nowUtc)
    {
        Validate(jobNumber, customerName, vehicle, repairCenter);

        return new RepairJob(
            Guid.NewGuid(), jobNumber.Trim(), customerName.Trim(), vehicle, repairCenter.Trim(),
            RepairStatus.Received, nowUtc, nowUtc);
    }

    /// <summary>
    /// Rebuilds a repair order from storage.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="Open"/> on purpose. Opening a job enforces the
    /// rules for <i>new</i> work; rehydrating replays what already happened, so it
    /// must accept any status the store legitimately holds. Collapsing the two
    /// would mean either weakening creation or being unable to load a finished job.
    /// </remarks>
    public static RepairJob Rehydrate(
        Guid id,
        string jobNumber,
        string customerName,
        Vehicle vehicle,
        string repairCenter,
        RepairStatus status,
        DateTimeOffset createdUtc,
        DateTimeOffset updatedUtc)
    {
        Validate(jobNumber, customerName, vehicle, repairCenter);
        RequireDefinedStatus(status);

        return new RepairJob(
            id, jobNumber.Trim(), customerName.Trim(), vehicle, repairCenter.Trim(),
            status, createdUtc, updatedUtc);
    }

    /// <summary>
    /// Moves the repair order to a new status, if the workflow permits it.
    /// </summary>
    /// <returns>
    /// <c>true</c> when the status actually changed; <c>false</c> when the job was
    /// already in <paramref name="newStatus"/>. Re-sending the current status is a
    /// no-op rather than an error, which is what makes the HTTP PUT idempotent.
    /// </returns>
    /// <exception cref="InvalidStatusTransitionException">The workflow does not allow this move.</exception>
    public bool ChangeStatus(RepairStatus newStatus, IStatusTransitionPolicy policy, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(policy);
        RequireDefinedStatus(newStatus);

        if (newStatus == Status)
        {
            return false;
        }

        if (!policy.IsAllowed(Status, newStatus))
        {
            throw new InvalidStatusTransitionException(Status, newStatus);
        }

        Status = newStatus;
        UpdatedUtc = nowUtc;
        return true;
    }

    private static void Validate(string jobNumber, string customerName, Vehicle vehicle, string repairCenter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentException.ThrowIfNullOrWhiteSpace(repairCenter);
    }

    /// <summary>
    /// Rejects values outside the approved set. An <c>enum</c> is only a suggestion
    /// at runtime - <c>(RepairStatus)99</c> is a perfectly valid cast - so the
    /// "only approved statuses" rule needs an explicit check, not just a type.
    /// </summary>
    private static void RequireDefinedStatus(RepairStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status), status, "Not one of the approved repair statuses.");
        }
    }
}
