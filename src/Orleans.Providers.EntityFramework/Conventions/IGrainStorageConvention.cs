using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Orleans;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework.Conventions
{
    public interface IGrainStorageConvention
    {
        /// <summary>
        /// Creates a method that returns an IQueryable'<typeparam name="TEntity"></typeparam>
        ///  against <typeparam name="TContext"></typeparam> type.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
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

        // todo: support composite key grains

        /// <summary>
        /// Creates a method that determines if a state object is persisted in the database.
        /// This is used to decide whether an insert or an update operation is needed.
        /// </summary>
        /// <param name="options"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        Func<TEntity, bool> CreateIsPersistedFunc<TEntity>(GrainStorageOptions options)
            where TEntity : class;

        /// <summary>
        /// Tries to find and configure an ETag property on the state model
        /// </summary>
        /// <param name="options"></param>
        /// <param name="throwIfNotFound">Indicates if failure of finding an ETag property should throw</param>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TState"></typeparam>
        void FindAndConfigureETag<TContext, TState, TEntity>(
            GrainStorageOptions<TContext, TState, TEntity> options,
            bool throwIfNotFound)
            where TContext : DbContext
            where TEntity : class;

        /// <summary>
        /// Configures the ETag property using the provided property name
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="options"></param>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TState"></typeparam>
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

    public interface IGrainStorageConvention<TContext, TState, TEntity>
        where TContext : DbContext
        where TEntity : class
    {
        /// <summary>
        /// Creates a method that returns an IQueryable'<typeparam name="TEntity"></typeparam>
        ///  against <typeparam name="TContext"></typeparam> type.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        Func<TContext, IQueryable<TEntity>>
            CreateDefaultDbSetAccessorFunc();

        /// <summary>
        /// Creates a method that generates an expression to be used by entity framework to 
        /// fetch a single state.
        /// </summary>
        /// <typeparam name="TEntity">Type of grain state</typeparam>
        /// <returns></returns>
        Func<GrainId, Expression<Func<TEntity, bool>>>
            CreateGrainStateQueryExpressionGeneratorFunc();

        Func<TContext, GrainId, Task<TEntity>> CreateDefaultReadStateFunc();

        Func<TContext, GrainId, Task<TEntity>> CreatePreCompiledDefaultReadStateFunc(
             GrainStorageOptions<TContext, TState, TEntity> options);

        void SetDefaultKeySelector(GrainStorageOptions<TContext, TState, TEntity> options);

        Action<IGrainState<TState>, TEntity> GetSetterFunc();

        Func<IGrainState<TState>, TEntity> GetGetterFunc();
    }
}