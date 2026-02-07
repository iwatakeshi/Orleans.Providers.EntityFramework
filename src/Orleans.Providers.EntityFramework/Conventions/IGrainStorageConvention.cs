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
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>A query accessor function.</returns>
    Func<TContext, IQueryable<TEntity>>
        CreateDefaultDbSetAccessorFunc<TContext, TEntity>()
        where TContext : DbContext
        where TEntity : class;

    /// <summary>
    /// Creates a default read state function used to fetch the grain state from the database.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="options">The storage options.</param>
    /// <returns>A function that reads the state.</returns>
    Func<TContext, GrainId, Task<TEntity>>
        CreateDefaultReadStateFunc<TContext, TState, TEntity>(
            GrainStorageOptions<TContext, TState, TEntity> options)
        where TContext : DbContext
        where TEntity : class;

    /// <summary>
    /// Creates a pre-compiled read state function (using EF Core compiled queries) for better performance.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="options">The storage options.</param>
    /// <returns>A function that reads the state.</returns>
    Func<TContext, GrainId, Task<TEntity>>
        CreatePreCompiledDefaultReadStateFunc<TContext, TState, TEntity>(
            GrainStorageOptions<TContext, TState, TEntity> options)
        where TContext : DbContext
        where TEntity : class;

    /// <summary>
    /// Configures the default key selectors based on conventions.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="options">The storage options.</param>
    void SetDefaultKeySelectors<TContext, TState, TEntity>(
        GrainStorageOptions<TContext, TState, TEntity> options)
        where TContext : DbContext
        where TEntity : class;

    /// <summary>
    /// Creates a method that determines if a state object is persisted in the database.
    /// This is used to decide whether an insert or an update operation is needed.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="options">The storage options.</param>
    /// <returns>A predicate that returns true when the entity is persisted.</returns>
    Func<TEntity, bool> CreateIsPersistedFunc<TEntity>(GrainStorageOptions options)
        where TEntity : class;

    /// <summary>
    /// Tries to find and configure an ETag property on the state model.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="options">The storage options.</param>
    /// <param name="throwIfNotFound">True to throw when no valid ETag is found.</param>
    void FindAndConfigureETag<TContext, TState, TEntity>(
        GrainStorageOptions<TContext, TState, TEntity> options,
        bool throwIfNotFound)
        where TContext : DbContext
        where TEntity : class;

    /// <summary>
    /// Configures the ETag property using the provided property name.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="propertyName">The ETag property name.</param>
    /// <param name="options">The storage options.</param>
    void ConfigureETag<TContext, TState, TEntity>(
        string propertyName,
        GrainStorageOptions<TContext, TState, TEntity> options)
        where TContext : DbContext
        where TEntity : class;

    /// <summary>
    /// Gets a function that sets the state on the grain state object from the entity.
    /// </summary>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>Action to set state.</returns>
    Action<IGrainState<TState>, TEntity> GetSetterFunc<TState, TEntity>()
        where TEntity : class;

    /// <summary>
    /// Gets a function that retrieves the entity from the grain state object.
    /// </summary>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>Function to get entity.</returns>
    Func<IGrainState<TState>, TEntity> GetGetterFunc<TState, TEntity>()
        where TEntity : class;
}

/// <summary>
/// Type-specific convention for configuring grain storage.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <typeparam name="TState">The grain state type.</typeparam>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IGrainStorageConvention<TContext, TState, TEntity>
    where TContext : DbContext
    where TEntity : class
{
    /// <summary>
    /// Creates a method that returns an <see cref="IQueryable{TEntity}"/>
    /// against the <typeparamref name="TContext"/> type.
    /// </summary>
    /// <returns>A query accessor function.</returns>
    Func<TContext, IQueryable<TEntity>>
        CreateDefaultDbSetAccessorFunc();

    /// <summary>
    /// Creates a method that generates an expression to be used by Entity Framework to
    /// fetch a single state.
    /// </summary>
    /// <returns>A function that generates the query predicate.</returns>
    Func<GrainId, Expression<Func<TEntity, bool>>>
        CreateGrainStateQueryExpressionGeneratorFunc();

    /// <summary>
    /// Creates a default read state function used to fetch the grain state from the database.
    /// </summary>
    /// <returns>A function that reads the state.</returns>
    Func<TContext, GrainId, Task<TEntity>> CreateDefaultReadStateFunc();

    /// <summary>
    /// Creates a pre-compiled read state function (using EF Core compiled queries) for better performance.
    /// </summary>
    /// <param name="options">The storage options.</param>
    /// <returns>A function that reads the state.</returns>
    Func<TContext, GrainId, Task<TEntity>> CreatePreCompiledDefaultReadStateFunc(
         GrainStorageOptions<TContext, TState, TEntity> options);

    /// <summary>
    /// Configures the default key selectors based on conventions.
    /// </summary>
    /// <param name="options">The storage options.</param>
    void SetDefaultKeySelector(GrainStorageOptions<TContext, TState, TEntity> options);

    /// <summary>
    /// Gets a function that sets the state on the grain state object from the entity.
    /// </summary>
    /// <returns>Action to set state.</returns>
    Action<IGrainState<TState>, TEntity> GetSetterFunc();

    /// <summary>
    /// Gets a function that retrieves the entity from the grain state object.
    /// </summary>
    /// <returns>Function to get entity.</returns>
    Func<IGrainState<TState>, TEntity> GetGetterFunc();
}