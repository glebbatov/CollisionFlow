namespace CollisionFlow.Api.Contracts;

/// <summary>Which store is answering, so the client can be honest about what it is showing.</summary>
public sealed record SystemStatusResponse
{
    /// <summary><c>Database</c> or <c>InMemory</c>.</summary>
    public required string DataSource { get; init; }

    /// <summary>True when the application is not serving from its authoritative store.</summary>
    public required bool IsDegraded { get; init; }

    /// <summary>
    /// A short, fixed explanation suitable for display.
    /// </summary>
    /// <remarks>
    /// Deliberately not the underlying exception message. Connection failures name servers,
    /// databases and sometimes accounts; none of that belongs in a response to an anonymous
    /// caller. The detail goes to the log, where it is useful and not public.
    /// </remarks>
    public string? Message { get; init; }

    public required DateTimeOffset Since { get; init; }
}
