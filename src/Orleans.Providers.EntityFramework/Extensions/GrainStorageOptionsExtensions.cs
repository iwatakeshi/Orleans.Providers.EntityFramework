using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orleans.Providers.EntityFramework.Exceptions;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework.Extensions;

public static class GrainStorageOptionsExtensions
{
    public static GrainStorageOptions<TContext, TState, TEntity> UseQuery<TContext, TState, TEntity>(
        this GrainStorageOptions<TContext, TState, TEntity> options,
        Func<TContext, IQueryable<TEntity>> queryFunc)
        where TContext : DbContext
        where TEntity : class
    {
        options.DbSetAccessor = queryFunc;
        return options;
    }

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
    public static GrainStorageOptions<TContext, TState, TEntity> UseETag<TContext, TState, TEntity>(
        this GrainStorageOptions<TContext, TState, TEntity> options)
        where TContext : DbContext
        where TEntity : class
    {
        options.ShouldUseETag = true;
        return options;
    }

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