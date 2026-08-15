namespace CollisionFlow.Infrastructure.Data;

/// <summary>
/// Error numbers raised by the stored procedures.
/// </summary>
/// <remarks>
/// These are a contract between <c>db/003_StoredProcedures.sql</c> and this assembly.
/// They are documented at the top of that file and must not be renumbered.
/// </remarks>
internal static class SqlErrorNumbers
{
    /// <summary>The workflow does not permit the requested status change.</summary>
    public const int InvalidTransition = 50001;

    /// <summary>The row changed since the caller read it.</summary>
    public const int ConcurrencyConflict = 50002;

    /// <summary>No repair order with the supplied id.</summary>
    public const int RepairJobNotFound = 50004;
}
