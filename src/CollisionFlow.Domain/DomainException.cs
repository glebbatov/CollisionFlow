namespace CollisionFlow.Domain;

/// <summary>
/// Base type for violations of a business rule, as opposed to a programming
/// error or an infrastructure failure. The API maps these to 4xx responses;
/// everything else is a 500.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}

/// <summary>
/// Thrown when a repair order is asked to move to a status the workflow does not
/// permit from its current one.
/// </summary>
/// <remarks>
/// In normal operation the API rejects the request before this can fire - the
/// caller gets a 422 listing the transitions that <i>are</i> legal. This exception
/// is the layer beneath that: it guarantees the rule holds even if a future code
/// path forgets to ask first. Defence in depth, not flow control.
/// </remarks>
public sealed class InvalidStatusTransitionException : DomainException
{
    public InvalidStatusTransitionException(RepairStatus from, RepairStatus to)
        : base($"A repair order cannot move from '{RepairStatusInfo.DisplayName(from)}' " +
               $"to '{RepairStatusInfo.DisplayName(to)}'.")
    {
        From = from;
        To = to;
    }

    public RepairStatus From { get; }

    public RepairStatus To { get; }
}
