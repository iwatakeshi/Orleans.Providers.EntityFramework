using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Providers.EntityFramework.Conventions;

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

        // TODO: Validate options

        options.IsConfigured = true;
    }
}