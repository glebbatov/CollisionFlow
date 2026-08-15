using System.Data;
using CollisionFlow.Domain;
using CollisionFlow.Domain.Abstractions;
using CollisionFlow.Infrastructure.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CollisionFlow.Infrastructure.Repositories;

/// <summary>
/// Repair order storage backed by stored procedures.
/// </summary>
/// <remarks>
/// Every statement this class sends is a procedure name and a parameter set. No SQL is
/// composed here, so there is no string for user input to escape from, and the database
/// can be tuned or its plans inspected without touching the application.
/// </remarks>
public sealed class SqlRepairJobRepository : IRepairJobRepository
{
    private readonly ISqlConnectionFactory _connections;

    public SqlRepairJobRepository(ISqlConnectionFactory connections)
    {
        ArgumentNullException.ThrowIfNull(connections);
        _connections = connections;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RepairJob>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connections.Create();

        var rows = await connection.QueryAsync<RepairJobRow>(
            new CommandDefinition(
                "dbo.usp_RepairJob_GetAll",
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return rows.Select(row => row.ToDomain()).ToArray();
    }

    /// <inheritdoc />
    public async Task<RepairJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = _connections.Create();

        var row = await connection.QuerySingleOrDefaultAsync<RepairJobRow>(
            new CommandDefinition(
                "dbo.usp_RepairJob_GetById",
                new { RepairJobId = id },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    /// <inheritdoc />
    public async Task<RepairJob?> UpdateStatusAsync(
        Guid id,
        RepairStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connections.Create();

        try
        {
            var row = await connection.QuerySingleOrDefaultAsync<RepairJobRow>(
                new CommandDefinition(
                    "dbo.usp_RepairJob_UpdateStatus",
                    new { RepairJobId = id, ToStatusId = (byte)newStatus },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            return row?.ToDomain();
        }
        catch (SqlException ex) when (ex.Number == SqlErrorNumbers.RepairJobNotFound)
        {
            // "Not found" is an ordinary outcome of asking about something that isn't
            // there, so it is translated to null rather than propagated as a failure.
            return null;
        }
        catch (SqlException ex) when (ex.Number == SqlErrorNumbers.InvalidTransition)
        {
            // A broken business rule is not a database failure. Translating it at this
            // boundary is what lets the API layer stay unaware that SQL Server exists.
            throw new InvalidStatusTransitionException(ex.Message);
        }
    }

    /// <summary>
    /// The shape returned by the read procedures.
    /// </summary>
    /// <remarks>
    /// Kept private and separate from <see cref="RepairJob"/>: the entity's constructor
    /// is deliberately closed, and a persistence type that Dapper can populate by
    /// convention should not be the type that enforces invariants.
    /// </remarks>
    private sealed class RepairJobRow
    {
        public Guid RepairJobId { get; init; }

        public string JobNumber { get; init; } = string.Empty;

        public string CustomerName { get; init; } = string.Empty;

        public short VehicleYear { get; init; }

        public string VehicleMake { get; init; } = string.Empty;

        public string VehicleModel { get; init; } = string.Empty;

        public string RepairCenter { get; init; } = string.Empty;

        public byte RepairStatusId { get; init; }

        public DateTimeOffset CreatedUtc { get; init; }

        public DateTimeOffset UpdatedUtc { get; init; }

        public RepairJob ToDomain() => RepairJob.Rehydrate(
            id: RepairJobId,
            jobNumber: JobNumber,
            customerName: CustomerName,
            vehicle: new Vehicle(VehicleYear, VehicleMake, VehicleModel),
            repairCenter: RepairCenter,

            // The cast is safe because Rehydrate rejects anything outside the approved
            // set, and dbo.RepairStatus is the table these ids are a foreign key to.
            status: (RepairStatus)RepairStatusId,
            createdUtc: CreatedUtc,
            updatedUtc: UpdatedUtc);
    }
}
