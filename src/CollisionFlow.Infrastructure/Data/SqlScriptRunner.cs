using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace CollisionFlow.Infrastructure.Data;

/// <summary>
/// Applies the SQL scripts embedded from <c>db/</c>, in filename order.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately not a migration framework. Every script is idempotent - tables guarded
/// by <c>IF OBJECT_ID(...) IS NULL</c>, reference data merged, procedures declared with
/// <c>CREATE OR ALTER</c> - so running all of them every time converges the database on
/// the intended state. There is no journal table to fall out of step with reality, and
/// no "it worked locally because I had already run the old version" failure mode.
/// </para>
/// <para>
/// The trade is that this does not handle destructive changes. A schema with real
/// production data would need versioned migrations; a numbered, idempotent script folder
/// is the right size for this.
/// </para>
/// </remarks>
public sealed partial class SqlScriptRunner
{
    private readonly ISqlConnectionFactory _connections;
    private readonly ILogger<SqlScriptRunner> _logger;

    public SqlScriptRunner(ISqlConnectionFactory connections, ILogger<SqlScriptRunner> logger)
    {
        _connections = connections;
        _logger = logger;
    }

    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        var assembly = typeof(SqlScriptRunner).Assembly;

        // Discovered rather than hard-coded, so adding db/005_Whatever.sql needs no
        // change here. Ordinal sort on the numeric prefix gives a deterministic order.
        var scripts = assembly.GetManifestResourceNames()
            .Where(name => name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        if (scripts.Length == 0)
        {
            _logger.LogWarning("No embedded SQL scripts were found; the schema was not applied.");
            return;
        }

        await using var connection = _connections.Create();
        await connection.OpenAsync(cancellationToken);

        foreach (var script in scripts)
        {
            var sql = await ReadResourceAsync(assembly, script, cancellationToken);

            // GO is a client-side batch separator, not a T-SQL keyword - ADO.NET has
            // never understood it, so the batches have to be split here.
            foreach (var batch in BatchSeparator().Split(sql))
            {
                if (string.IsNullOrWhiteSpace(batch))
                {
                    continue;
                }

                await using var command = connection.CreateCommand();
                command.CommandText = batch;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            _logger.LogInformation("Applied database script {Script}.", script);
        }
    }

    private static async Task<string> ReadResourceAsync(
        Assembly assembly,
        string resourceName,
        CancellationToken cancellationToken)
    {
        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded script '{resourceName}' could not be opened.");

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex BatchSeparator();
}
