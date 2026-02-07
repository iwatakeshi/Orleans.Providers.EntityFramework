using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Providers.EntityFramework.Conventions;
using Orleans.Providers.EntityFramework.Exceptions;

namespace Orleans.Providers.EntityFramework;

public class GrainStoragePostConfigureOptions<TContext, TState, TEntity>(IServiceProvider serviceProvider)
    : IPostConfigureOptions<GrainStorageOptions<TContext, TState, TEntity>>
    where TContext : DbContext
    where TState : class
    where TEntity : class
{
    public IGrainStorageConvention<TContext, TState, TEntity>? Convention { get; } =
        serviceProvider.GetService<IGrainStorageConvention<TContext, TState, TEntity>>();

    public IGrainStorageConvention DefaultConvention { get; } =
        serviceProvider.GetRequiredService<IGrainStorageConvention>();

    public void PostConfigure(string? name, GrainStorageOptions<TContext, TState, TEntity> options)
    {
        options.IsPersistedFunc ??= DefaultConvention.CreateIsPersistedFunc<TEntity>(options);

        // Configure ETag
        if (options.ShouldUseETag && !string.IsNullOrWhiteSpace(options.ETagPropertyName))
        {
            DefaultConvention.ConfigureETag(options.ETagPropertyName, options);
        }

        if (options.ReadStateAsync is null)
        {
            options.DbSetAccessor ??= Convention?.CreateDefaultDbSetAccessorFunc()
                                     ?? DefaultConvention.CreateDefaultDbSetAccessorFunc<TContext, TEntity>();

            if (Convention is not null)
                Convention.SetDefaultKeySelector(options);
            else
                DefaultConvention.SetDefaultKeySelectors(options);

            options.ReadStateAsync = options.PreCompileReadQuery
                ? Convention?.CreatePreCompiledDefaultReadStateFunc(options)
                  ?? DefaultConvention.CreatePreCompiledDefaultReadStateFunc(options)
                : Convention?.CreateDefaultReadStateFunc()
                  ?? DefaultConvention.CreateDefaultReadStateFunc(options);
        }

        options.SetEntity ??= Convention?.GetSetterFunc()
                              ?? DefaultConvention.GetSetterFunc<TState, TEntity>();

        options.GetEntity ??= Convention?.GetGetterFunc()
                              ?? DefaultConvention.GetGetterFunc<TState, TEntity>();

        DefaultConvention.FindAndConfigureETag(options, options.ShouldUseETag);

        ValidateOptions(options, name);

        options.IsConfigured = true;
    }

    private static void ValidateOptions(GrainStorageOptions<TContext, TState, TEntity> options, string? name)
    {
        string displayName = name ?? typeof(TState).FullName ?? typeof(TState).Name;

        if (options.ReadStateAsync is null)
            throw new GrainStorageConfigurationException(
                $"ReadStateAsync delegate is not configured for grain storage '{displayName}'.");

        if (options.SetEntity is null)
            throw new GrainStorageConfigurationException(
                $"SetEntity delegate is not configured for grain storage '{displayName}'.");

        if (options.GetEntity is null)
            throw new GrainStorageConfigurationException(
                $"GetEntity delegate is not configured for grain storage '{displayName}'.");

        if (options.IsPersistedFunc is null)
            throw new GrainStorageConfigurationException(
                $"IsPersistedFunc delegate is not configured for grain storage '{displayName}'.");

        if (options.CheckForETag)
        {
            if (options.GetETagFunc is null)
                throw new GrainStorageConfigurationException(
                    $"ETag is enabled but GetETagFunc is not configured for grain storage '{displayName}'.");

            if (options.ConvertETagObjectToStringFunc is null)
                throw new GrainStorageConfigurationException(
                    $"ETag is enabled but ConvertETagObjectToStringFunc is not configured for grain storage '{displayName}'.");
        }
    }
}