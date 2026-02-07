using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework.Conventions;

/// <summary>
/// Default convention for configuring grain storage with Entity Framework.
/// </summary>
public interface IGrainStorageConvention
{
    /// <summary>
    /// Creates a method that returns an <see cref="IQueryable{TEntity}"/>
    /// against the <typeparamref name="TContext"/> type.
    /// </summary>
    Func<TContext, IQueryable<TEntity>>
        CreateDefaultDbSetAccessorFunc<TContext, TEntity>()
        where TContext : DbContext
        where TEntity : class;

    Func<TContext, GrainId, Task<TEntity>>
        CreateDefaultReadStateFunc<TContext, TState, TEntity>(
            GrainStorageOptions<TContext, TState, TEntity> options)
        where TContext : DbContext
        where TEntity : class;

    Func<TContext, GrainId, Task<TEntity>>
        CreatePreCompiledDefaultReadStateFunc<TContext, TState, TEntity>(
            GrainStorageOptions<TContext, TState, TEntity> options)
        where TContext : DbContext
        where TEntity : class;

    void SetDefaultKeySelectors<TContext, TState, TEntity>(
        GrainStorageOptions<TContext, TState, TEntity> options)
        where TContext : DbContext
        where TEntity : class;

    /// <summary>
    /// Creates a method that determines if a state object is persisted in the database.
    /// This is used to decide whether an insert or an update operation is needed.
    /// </summary>
    Func<TEntity, bool> CreateIsPersistedFunc<TEntity>(GrainStorageOptions options)
        where TEntity : class;

    /// <summary>
    /// Tries to find and configure an ETag property on the state model.
    /// </summary>
    void FindAndConfigureETag<TContext, TState, TEntity>(
        GrainStorageOptions<TContext, TState, TEntity> options,
        bool throwIfNotFound)
        where TContext : DbContext
        where TEntity : class;

    /// <summary>
    /// Configures the ETag property using the provided property name.
    /// </summary>
    void ConfigureETag<TContext, TState, TEntity>(
        string propertyName,
        GrainStorageOptions<TContext, TState, TEntity> options)
        where TContext : DbContext
        where TEntity : class;

    Action<IGrainState<TState>, TEntity> GetSetterFunc<TState, TEntity>()
        where TEntity : class;

    Func<IGrainState<TState>, TEntity> GetGetterFunc<TState, TEntity>()
        where TEntity : class;
}

/// <summary>
/// Type-specific convention for configuring grain storage.
/// </summary>
public interface IGrainStorageConvention<TContext, TState, TEntity>
    where TContext : DbContext
    where TEntity : class
{
    /// <summary>
    /// Creates a method that returns an <see cref="IQueryable{TEntity}"/>
    /// against the <typeparamref name="TContext"/> type.
    /// </summary>
    Func<TContext, IQueryable<TEntity>>
        CreateDefaultDbSetAccessorFunc();

    /// <summary>
    /// Creates a method that generates an expression to be used by Entity Framework to
    /// fetch a single state.
    /// </summary>
    Func<GrainId, Expression<Func<TEntity, bool>>>
        CreateGrainStateQueryExpressionGeneratorFunc();

    Func<TContext, GrainId, Task<TEntity>> CreateDefaultReadStateFunc();

    Func<TContext, GrainId, Task<TEntity>> CreatePreCompiledDefaultReadStateFunc(
         GrainStorageOptions<TContext, TState, TEntity> options);

    void SetDefaultKeySelector(GrainStorageOptions<TContext, TState, TEntity> options);

    Action<IGrainState<TState>, TEntity> GetSetterFunc();

    Func<IGrainState<TState>, TEntity> GetGetterFunc();
}