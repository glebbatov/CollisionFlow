using CollisionFlow.Domain;
using CollisionFlow.Domain.Abstractions;
using CollisionFlow.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CollisionFlow.Infrastructure;

/// <summary>
/// One place where infrastructure declares what it provides.
/// </summary>
/// <remarks>
/// The API project calls <c>AddInfrastructure()</c> and never names a concrete
/// repository. Swapping the in-memory store for the SQL one is a change here and
/// nowhere else.
/// </remarks>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // TimeProvider instead of DateTime.UtcNow: time becomes an injected
        // dependency, so tests can control it rather than sleep through it.
        services.TryAddSingleton(TimeProvider.System);

        services.TryAddSingleton<IStatusTransitionPolicy>(_ => StatusTransitionPolicy.Default);
        services.TryAddSingleton<IRepairJobRepository, InMemoryRepairJobRepository>();

        return services;
    }
}
