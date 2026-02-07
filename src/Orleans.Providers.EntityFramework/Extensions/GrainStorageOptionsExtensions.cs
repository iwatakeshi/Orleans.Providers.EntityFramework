using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orleans.Providers.EntityFramework.Exceptions;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework.Extensions;

/// <summary>
/// Extension methods for configuring grain storage options.
/// </summary>
public static class GrainStorageOptionsExtensions
{
    /// <summary>
    /// Configures the query used to retrieve the entity from the database.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="options">The options to configure.</param>
    /// <param name="queryFunc">The function to create the query.</param>
    /// <returns>The configured options.</returns>
    public static GrainStorageOptions<TContext, TState, TEntity> UseQuery<TContext, TState, TEntity>(
        this GrainStorageOptions<TContext, TState, TEntity> options,
        Func<TContext, IQueryable<TEntity>> queryFunc)
        where TContext : DbContext
        where TEntity : class
    {
        options.DbSetAccessor = queryFunc;
        return options;
    }

    /// <summary>
    /// Configures a predicate to determine if the entity is already persisted in the database.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="options">The options to configure.</param>
    /// <param name="isPersistedFunc">The predicate function.</param>
    /// <returns>The configured options.</returns>
    public static GrainStorageOptions<TContext, TState, TEntity> ConfigureIsPersisted<TContext, TState, TEntity>(
        this GrainStorageOptions<TContext, TState, TEntity> options,
        Func<TEntity, bool> isPersistedFunc)
        where TContext : DbContext
        where TEntity : class
    {
        options.IsPersistedFunc = isPersistedFunc;
        return options;
    }

    /// <summary>
    /// Instructs the storage provider to precompile read query.
    /// This will lead to better performance for complex queries.
    /// Default is to precompile.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="options">The options to configure.</param>
    /// <param name="value">True to precompile; otherwise false.</param>
    /// <returns>The configured options.</returns>
    public static GrainStorageOptions<TContext, TState, TEntity> PreCompileReadQuery<TContext, TState, TEntity>(
        this GrainStorageOptions<TContext, TState, TEntity> options,
        bool value = true)
        where TContext : DbContext
        where TEntity : class
    {
        options.PreCompileReadQuery = value;
        return options;
    }

    /// <summary>
    /// Overrides the default implementation used to query grain state from database.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="options">The options to configure.</param>
    /// <param name="readStateAsyncFunc">The read state function.</param>
    /// <returns>The configured options.</returns>
    public static GrainStorageOptions<TContext, TState, TEntity> ConfigureReadState<TContext, TState, TEntity>(
        this GrainStorageOptions<TContext, TState, TEntity> options,
        Func<TContext, GrainId, Task<TEntity>> readStateAsyncFunc)
        where TContext : DbContext
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(options);
        options.ReadStateAsync = readStateAsyncFunc ?? throw new ArgumentNullException(nameof(readStateAsyncFunc));
        return options;
    }

    /// <summary>
    /// Instruct the storage that the current entity should use ETags.
    /// If no valid properties were found on the entity, an exception would be thrown.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="options">The options to configure.</param>
    /// <returns>The configured options.</returns>
    public static GrainStorageOptions<TContext, TState, TEntity> UseETag<TContext, TState, TEntity>(
        this GrainStorageOptions<TContext, TState, TEntity> options)
        where TContext : DbContext
        where TEntity : class
    {
        options.ShouldUseETag = true;
        return options;
    }

    /// <summary>
    /// Configures the property to be used as ETag.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="options">The options to configure.</param>
    /// <param name="expression">The expression to select the property.</param>
    /// <returns>The configured options.</returns>
    public static GrainStorageOptions<TContext, TState, TEntity> UseETag<TContext, TState, TEntity, TProperty>(
        this GrainStorageOptions<TContext, TState, TEntity> options,
        Expression<Func<TEntity, TProperty>> expression)
        where TContext : DbContext
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(expression);

        var memberExpression = expression.Body as MemberExpression
                               ?? throw new ArgumentException(
                                   $"{nameof(expression)} must be a MemberExpression.");

        options.ETagPropertyName = memberExpression.Member.Name;
        options.ShouldUseETag = true;

        return options;
    }

    /// <summary>
    /// Configures the property to be used as ETag.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="options">The options to configure.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The configured options.</returns>
    public static GrainStorageOptions<TContext, TState, TEntity> UseETag<TContext, TState, TEntity>(
        this GrainStorageOptions<TContext, TState, TEntity> options,
        string propertyName)
        where TContext : DbContext
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(propertyName);

        options.ETagPropertyName = propertyName;
        options.ShouldUseETag = true;

        return options;
    }

    /// <summary>
    /// Configures the property to be used as the primary key.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="options">The options to configure.</param>
    /// <param name="expression">The expression to select the property.</param>
    /// <returns>The configured options.</returns>
    public static GrainStorageOptions<TContext, TState, TEntity> UseKey<TContext, TState, TEntity>(
        this GrainStorageOptions<TContext, TState, TEntity> options,
        Expression<Func<TEntity, Guid>> expression)
        where TContext : DbContext
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(expression);

        options.KeyPropertyName = GetMemberName(expression);
        return options;
    }

    /// <summary>
    /// Configures the property to be used as the primary key.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="options">The options to configure.</param>
    /// <param name="expression">The expression to select the property.</param>
    /// <returns>The configured options.</returns>
    public static GrainStorageOptions<TContext, TState, TEntity> UseKey<TContext, TState, TEntity>(
        this GrainStorageOptions<TContext, TState, TEntity> options,
        Expression<Func<TEntity, long>> expression)
        where TContext : DbContext
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(expression);

        options.KeyPropertyName = GetMemberName(expression);
        return options;
    }

    /// <summary>
    /// Configures the property to be used as the primary key.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="options">The options to configure.</param>
    /// <param name="expression">The expression to select the property.</param>
    /// <returns>The configured options.</returns>
    public static GrainStorageOptions<TContext, TState, TEntity> UseKey<TContext, TState, TEntity>(
        this GrainStorageOptions<TContext, TState, TEntity> options,
        Expression<Func<TEntity, string>> expression)
        where TContext : DbContext
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(expression);

        options.KeyPropertyName = GetMemberName(expression);
        return options;
    }

    /// <summary>
    /// Configures the property to be used as the primary key.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="options">The options to configure.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The configured options.</returns>
    public static GrainStorageOptions<TContext, TState, TEntity> UseKey<TContext, TState, TEntity>(
        this GrainStorageOptions<TContext, TState, TEntity> options,
        string propertyName)
        where TContext : DbContext
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(propertyName);

        options.KeyPropertyName = propertyName;
        return options;
    }

    /// <summary>
    /// Configures the property to be used as the extended key.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="options">The options to configure.</param>
    /// <param name="expression">The expression to select the property.</param>
    /// <returns>The configured options.</returns>
    public static GrainStorageOptions<TContext, TState, TEntity> UseKeyExt<TContext, TState, TEntity>(
        this GrainStorageOptions<TContext, TState, TEntity> options,
        Expression<Func<TEntity, string>> expression)
        where TContext : DbContext
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(expression);

        options.KeyExtPropertyName = GetMemberName(expression);
        return options;
    }

    /// <summary>
    /// Configures the property to be used as the extended key.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="options">The options to configure.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The configured options.</returns>
    public static GrainStorageOptions<TContext, TState, TEntity> UseKeyExt<TContext, TState, TEntity>(
        this GrainStorageOptions<TContext, TState, TEntity> options,
        string propertyName)
        where TContext : DbContext
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(propertyName);

        options.KeyExtPropertyName = propertyName;
        return options;
    }

    /// <summary>
    /// Configures the property to check for persistence (e.g. to determine if to Insert or Update).
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="options">The options to configure.</param>
    /// <param name="expression">The expression to select the property.</param>
    /// <returns>The configured options.</returns>
    public static GrainStorageOptions<TContext, TState, TEntity> CheckPersistenceOn<TContext, TState, TEntity, TProperty>(
        this GrainStorageOptions<TContext, TState, TEntity> options,
        Expression<Func<TEntity, TProperty>> expression)
        where TContext : DbContext
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(expression);

        options.PersistenceCheckPropertyName = GetMemberName(expression);
        return options;
    }

    /// <summary>
    /// Configures the property to check for persistence (e.g. to determine if to Insert or Update).
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TState">The grain state type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="options">The options to configure.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The configured options.</returns>
    public static GrainStorageOptions<TContext, TState, TEntity> CheckPersistenceOn<TContext, TState, TEntity>(
        this GrainStorageOptions<TContext, TState, TEntity> options,
        string propertyName)
        where TContext : DbContext
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(propertyName);

        options.PersistenceCheckPropertyName = propertyName;
        return options;
    }

    private static string GetMemberName<T, TProperty>(Expression<Func<T, TProperty>> expression)
        => expression.Body is MemberExpression member
            ? member.Member.Name
            : throw new ArgumentException($"{nameof(expression)} must be a MemberExpression.");
}