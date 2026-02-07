using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans.Providers.EntityFramework.Conventions;
using Orleans.Runtime.Hosting;
using Orleans.Storage;

namespace Orleans.Providers.EntityFramework.Extensions;

/// <summary>
/// Service collection extensions for configuring Entity Framework grain storage.
/// </summary>
public static class GrainStorageServiceCollectionExtensions
{
    /// <summary>
    /// Configures grain storage options for a specific grain state and entity type.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">An optional configuration action.</param>
    /// <param name="stateName">The state name; defaults to the state type full name.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection ConfigureGrainStorageOptions<TContext, TState, TEntity>(
        this IServiceCollection services,
        Action<GrainStorageOptions<TContext, TState, TEntity>>? configureOptions = null,
        string? stateName = null)
        where TContext : DbContext
        where TState : class
        where TEntity : class
    {
        string optionsName = stateName ?? typeof(TState).FullName!;

        return services
            .AddSingleton<IPostConfigureOptions<GrainStorageOptions<TContext, TState, TEntity>>,
                GrainStoragePostConfigureOptions<TContext, TState, TEntity>>()
            .Configure<GrainStorageOptions<TContext, TState, TEntity>>(optionsName, options =>
            {
                configureOptions?.Invoke(options);
            });
    }

    /// <summary>
    /// Configures grain storage options when the state and entity types are the same.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">An optional configuration action.</param>
    /// <param name="stateName">The state name; defaults to the state type full name.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection ConfigureGrainStorageOptions<TContext, TState>(
        this IServiceCollection services,
        Action<GrainStorageOptions<TContext, TState, TState>>? configureOptions = null,
        string? stateName = null)
        where TContext : DbContext
        where TState : class
        => services.ConfigureGrainStorageOptions<TContext, TState, TState>(configureOptions, stateName);

    /// <summary>
    /// Registers the Entity Framework grain storage provider.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="providerName">The storage provider name.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddEfGrainStorage<TContext>(
        this IServiceCollection services,
        string providerName = StorageProviderConstants.DefaultStorageProviderName)
        where TContext : DbContext
    {
        services.TryAddSingleton(typeof(IEntityTypeResolver), typeof(EntityTypeResolver));
        services.TryAddSingleton(typeof(IGrainStorageConvention), typeof(GrainStorageConvention));
        services.TryAddSingleton(typeof(IGrainStateEntryConfigurator<,>),
            typeof(DefaultGrainStateEntryConfigurator<,>));
        services.AddSingleton(typeof(EntityFrameworkGrainStorage<TContext>));

        // Use Orleans's AddGrainStorage helper for proper named provider registration.
        // This handles keyed DI, default provider fallback, and ILifecycleParticipant auto-registration.
        services.AddGrainStorage(providerName,
            (sp, _) => sp.GetRequiredService<EntityFrameworkGrainStorage<TContext>>());

        return services;
    }
}