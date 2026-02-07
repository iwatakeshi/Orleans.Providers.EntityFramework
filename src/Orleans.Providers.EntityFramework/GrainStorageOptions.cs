using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework;

/// <summary>
/// Base options for Entity Framework grain storage.
/// </summary>
public abstract class GrainStorageOptions
{
    internal string? KeyPropertyName { get; set; }

    internal string? KeyExtPropertyName { get; set; }

    internal string? ETagPropertyName { get; set; }

    internal string? PersistenceCheckPropertyName { get; set; }

    internal IProperty? ETagProperty { get; set; }

    internal bool CheckForETag { get; set; }

    internal Func<object, string>? ConvertETagObjectToStringFunc { get; set; }

    internal Type? ETagType { get; set; }

    /// <summary>
    /// Gets or sets a boolean that indicates if the storage provider should use ETags.
    /// </summary>
    public bool ShouldUseETag { get; set; }

    internal bool IsConfigured { get; set; }

    internal bool PreCompileReadQuery { get; set; } = true;
}

/// <summary>
/// Options for Entity Framework grain storage.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <typeparam name="TState">The grain state type.</typeparam>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class GrainStorageOptions<TContext, TState, TEntity> : GrainStorageOptions
    where TContext : DbContext
    where TEntity : class
{
    internal Func<TContext, IQueryable<TEntity>>? DbSetAccessor { get; set; }

    internal Func<TEntity, bool>? IsPersistedFunc { get; set; }

    internal Func<TEntity, string>? GetETagFunc { get; set; }

    internal Expression<Func<TEntity, Guid>>? GuidKeySelector { get; set; }

    internal Expression<Func<TEntity, string>>? KeyExtSelector { get; set; }

    internal Func<TEntity, long>? LongKeySelector { get; set; }

    internal Func<TContext, GrainId, Task<TEntity>>? ReadStateAsync { get; set; }

    internal Action<IGrainState<TState>, TEntity>? SetEntity { get; set; }

    internal Func<IGrainState<TState>, TEntity>? GetEntity { get; set; }
}