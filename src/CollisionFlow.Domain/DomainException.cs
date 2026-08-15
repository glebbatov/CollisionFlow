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
/// path forgets to ask first. Defense in depth, not flow control.
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

    /// <summary>
    /// Used when the database rejected the move. The stored procedure composes its own
    /// message from the status table, so it is the authority on the wording - repeating
    /// the lookup here would risk the two disagreeing.
    /// </summary>
    public InvalidStatusTransitionException(string message) : base(message)
    {
    }

    /// <summary>The status moved from, when known. Null when the database raised the error.</summary>
    public RepairStatus? From { get; }

    /// <summary>The status moved to, when known. Null when the database raised the error.</summary>
    public RepairStatus? To { get; }
}
