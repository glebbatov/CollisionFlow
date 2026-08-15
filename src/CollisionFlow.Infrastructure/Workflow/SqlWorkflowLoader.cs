using System.Data;
using CollisionFlow.Domain;
using CollisionFlow.Infrastructure.Data;
using Dapper;

namespace CollisionFlow.Infrastructure.Workflow;

/// <summary>Reads the workflow graph from <c>dbo.usp_Workflow_Get</c>.</summary>
public sealed class SqlWorkflowLoader
{
    private readonly ISqlConnectionFactory _connections;

    public SqlWorkflowLoader(ISqlConnectionFactory connections)
    {
        _connections = connections;
    }

    /// <summary>
    /// Loads the statuses and the legal edges between them in a single round trip.
    /// </summary>
    /// <remarks>
    /// The procedure returns two result sets and Dapper reads both from one command.
    /// Two queries would have been simpler to write and strictly worse: the two sets
    /// have to be consistent with each other, and one round trip guarantees they were
    /// read from the same point in time.
    /// </remarks>
    public async Task<IStatusTransitionPolicy> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connections.Create();

        await using var results = await connection.QueryMultipleAsync(
            new CommandDefinition(
                "dbo.usp_Workflow_Get",
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        // The first set is read and discarded here: display names are served from the
        // domain, and this loader's job is the graph. Reading it keeps the procedure's
        // contract honest and leaves the reader in the right place for the second set.
        _ = await results.ReadAsync<StatusRow>();
        var edges = (await results.ReadAsync<TransitionRow>()).ToArray();

        if (edges.Length == 0)
        {
            throw new InvalidOperationException(
                "dbo.StatusTransition is empty - the workflow would permit no changes at all.");
        }

        return new StatusTransitionPolicy(
            edges.Select(e => new StatusTransition((RepairStatus)e.FromStatusId, (RepairStatus)e.ToStatusId)));
    }

    private sealed record StatusRow(byte RepairStatusId, string Code, string DisplayName, byte SortOrder, bool IsTerminal);

    private sealed record TransitionRow(byte FromStatusId, byte ToStatusId);
}
