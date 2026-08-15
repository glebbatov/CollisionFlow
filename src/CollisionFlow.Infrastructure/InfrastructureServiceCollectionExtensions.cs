using CollisionFlow.Domain;
using CollisionFlow.Domain.Abstractions;
using CollisionFlow.Infrastructure.Data;
using CollisionFlow.Infrastructure.Repositories;
using CollisionFlow.Infrastructure.Workflow;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CollisionFlow.Infrastructure;

/// <summary>
/// One place where infrastructure declares what it provides.
/// </summary>
/// <remarks>
/// The API project calls <c>AddInfrastructure()</c> and never names a concrete repository.
/// Whether the application talks to SQL Server or to a list in memory is decided here, by
/// configuration, and nowhere else.
/// </remarks>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        // TimeProvider instead of DateTime.UtcNow: time becomes an injected dependency,
        // so tests can control it rather than sleep through it.
        services.TryAddSingleton(TimeProvider.System);

        // The live policy starts on the domain's compiled-in workflow and adopts the
        // database's version at startup. Registered twice so callers can depend on the
        // interface while the startup task holds the concrete type it needs to replace.
        services.TryAddSingleton<MutableStatusTransitionPolicy>();
        services.TryAddSingleton<IStatusTransitionPolicy>(
            sp => sp.GetRequiredService<MutableStatusTransitionPolicy>());

        var databaseOptions = configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();

        // Published so the UI can tell the user which store answered.
        services.TryAddSingleton<DataSourceStatus>();
        services.TryAddSingleton<IDataSourceStatus>(sp => sp.GetRequiredService<DataSourceStatus>());

        if (databaseOptions.IsConfigured)
        {
            services.TryAddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
            services.TryAddSingleton<SqlScriptRunner>();
            services.TryAddSingleton<SqlWorkflowLoader>();
            services.AddHostedService<DatabaseStartupTask>();

            // Both concrete repositories are registered, and the decorator is what the
            // application resolves. Nothing above this line knows there are two.
            services.TryAddSingleton<SqlRepairJobRepository>();
            services.TryAddSingleton<InMemoryRepairJobRepository>();
            services.TryAddSingleton<IRepairJobRepository, ResilientRepairJobRepository>();
        }
        else
        {
            // No connection string: there is nothing to fall back from, so the fallback
            // is simply the store. Wrapping it would only add a circuit breaker around
            // an in-process list.
            services.TryAddSingleton<IRepairJobRepository, InMemoryRepairJobRepository>();
        }

        return services;
    }
}
