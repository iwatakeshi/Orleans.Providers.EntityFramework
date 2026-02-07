using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Orleans.Providers.EntityFramework.Exceptions;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework.Extensions
{
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
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TState"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="options"></param>
        /// <param name="value"></param>
        /// <returns></returns>
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
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TState"></typeparam>
        /// <param name="options"></param>
        /// <param name="readStateAsyncFunc"></param>
        /// <returns></returns>
        public static GrainStorageOptions<TContext, TState, TEntity> ConfigureReadState<TContext, TState, TEntity>(
            this GrainStorageOptions<TContext, TState, TEntity> options,
            Func<TContext, GrainId, Task<TEntity>> readStateAsyncFunc)
            where TContext : DbContext
            where TEntity : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            options.ReadStateAsync = readStateAsyncFunc ?? throw new ArgumentNullException(nameof(readStateAsyncFunc));
            return options;
        }


        /// <summary>
        /// Instruct the storage that the current entity should use etags.
        /// If no valid properties were found on the entity and exception would be thrown.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TState"></typeparam>
        /// <param name="options"></param>
        /// <returns></returns>
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
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (expression == null) throw new ArgumentNullException(nameof(expression));

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
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

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
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var memberExpression = expression.Body as MemberExpression
                                   ?? throw new ArgumentException(
                                       $"{nameof(expression)} must be a MemberExpression.");

            options.KeyPropertyName = memberExpression.Member.Name;

            return options;
        }

        public static GrainStorageOptions<TContext, TState, TEntity> UseKey<TContext, TState, TEntity>(
            this GrainStorageOptions<TContext, TState, TEntity> options,
            Expression<Func<TEntity, long>> expression)
            where TContext : DbContext
            where TEntity : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var memberExpression = expression.Body as MemberExpression
                                   ?? throw new GrainStorageConfigurationException(
                                       $"{nameof(expression)} must be a MemberExpression.");

            options.KeyPropertyName = memberExpression.Member.Name;

            return options;
        }

        public static GrainStorageOptions<TContext, TState, TEntity> UseKey<TContext, TState, TEntity>(
            this GrainStorageOptions<TContext, TState, TEntity> options,
            Expression<Func<TEntity, string>> expression)
            where TContext : DbContext
            where TEntity : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var memberExpression = expression.Body as MemberExpression
                                   ?? throw new ArgumentException(
                                       $"{nameof(expression)} must be a MemberExpression.");

            options.KeyPropertyName = memberExpression.Member.Name;

            return options;
        }

        public static GrainStorageOptions<TContext, TState, TEntity> UseKey<TContext, TState, TEntity>(
            this GrainStorageOptions<TContext, TState, TEntity> options,
            string propertyName)
            where TContext : DbContext
            where TEntity : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

            options.KeyPropertyName = propertyName;

            return options;
        }

        public static GrainStorageOptions<TContext, TState, TEntity> UseKeyExt<TContext, TState, TEntity>(
            this GrainStorageOptions<TContext, TState, TEntity> options,
            Expression<Func<TEntity, string>> expression)
            where TContext : DbContext
            where TEntity : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var memberExpression = expression.Body as MemberExpression
                                   ?? throw new ArgumentException(
                                       $"{nameof(expression)} must be a MemberExpression.");

            options.KeyExtPropertyName = memberExpression.Member.Name;

            return options;
        }

        public static GrainStorageOptions<TContext, TState, TEntity> UseKeyExt<TContext, TState, TEntity>(
            this GrainStorageOptions<TContext, TState, TEntity> options,
            string propertyName)
            where TContext : DbContext
            where TEntity : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

            options.KeyExtPropertyName = propertyName;

            return options;
        }

        public static GrainStorageOptions<TContext, TState, TEntity> CheckPersistenceOn<TContext, TState, TEntity, TProperty>(
            this GrainStorageOptions<TContext, TState, TEntity> options,
            Expression<Func<TEntity, TProperty>> expression)
            where TContext : DbContext
            where TEntity : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var memberExpression = expression.Body as MemberExpression
                                   ?? throw new ArgumentException(
                                       $"{nameof(expression)} must be a MemberExpression.");

            options.PersistenceCheckPropertyName = memberExpression.Member.Name;

            return options;
        }

        public static GrainStorageOptions<TContext, TState, TEntity> CheckPersistenceOn<TContext, TState, TEntity>(
            this GrainStorageOptions<TContext, TState, TEntity> options,
            string propertyName)
            where TContext : DbContext
            where TEntity : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

            options.PersistenceCheckPropertyName = propertyName;

            return options;
        }
    }
}