using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Providers.EntityFramework.Exceptions;
using Orleans.Providers.EntityFramework.Internal;
using Orleans;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework.Conventions
{
    public class GrainStorageConvention : IGrainStorageConvention
    {
        private readonly GrainStorageConventionOptions _options;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public GrainStorageConvention(IOptions<GrainStorageConventionOptions> options,
            IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _options = options.Value;
        }

        public virtual Action<IGrainState<TState>, TEntity> GetSetterFunc<TState, TEntity>() where TEntity : class
        {
            return (state, entity) =>
            {
                if (entity is TState typed)
                    state.State = typed;
                else
                    throw new GrainStorageConfigurationException(
                        $"State type \"{typeof(TState).FullName}\" is not assignable from \"{typeof(TEntity).FullName}\".");
            };
        }

        public virtual Func<IGrainState<TState>, TEntity> GetGetterFunc<TState, TEntity>() where TEntity : class
        {
            return state => state.State as TEntity;
        }


        #region Default Query

        public virtual Func<TContext, IQueryable<TEntity>> CreateDefaultDbSetAccessorFunc<TContext, TEntity>()
            where TContext : DbContext
            where TEntity : class
        {
            Type contextType = typeof(TContext);

            // Find a dbSet<TEntity> as default
            PropertyInfo dbSetPropertyInfo =
                contextType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(pInfo => pInfo.PropertyType == typeof(DbSet<TEntity>));

            if (dbSetPropertyInfo == null)
                throw new GrainStorageConfigurationException(
                    $"Could not find A property of type \"{typeof(DbSet<TEntity>).FullName}\" " +
                    $"on context with type \"{typeof(TContext).FullName}\"");

            var dbSetDelegate = (Func<TContext, IQueryable<TEntity>>)Delegate.CreateDelegate(
                typeof(Func<TContext, IQueryable<TEntity>>),
                null,
                dbSetPropertyInfo.GetMethod);

            // set queries as no tracking
            MethodInfo noTrackingMethodInfo = (typeof(GrainStorageConvention).GetMethod(nameof(AsNoTracking))
                                               ?? throw new Exception("Impossible"))
                .MakeGenericMethod(typeof(TContext), typeof(TEntity));

            // create final delegate which chains dbSet getter and no tracking delegates
            return (Func<TContext, IQueryable<TEntity>>)Delegate.CreateDelegate(
                typeof(Func<TContext, IQueryable<TEntity>>),
                dbSetDelegate,
                noTrackingMethodInfo);
        }

        public static IQueryable<TEntity> AsNoTracking<TContext, TEntity>(
            Func<TContext, IQueryable<TEntity>> func,
            TContext context)
            where TContext : DbContext
            where TEntity : class
            => func(context).AsNoTracking();

        public virtual Func<TContext, GrainId, Task<TEntity>>
            CreateDefaultReadStateFunc<TContext, TState, TEntity>(
                GrainStorageOptions<TContext, TState, TEntity> options)
            where TContext : DbContext
            where TEntity : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            // Determine entity key type at setup time to avoid mismatched key extraction at runtime.
            Type keyType = GetEntityKeyType<TEntity>(options);

            if (keyType == typeof(Guid))
            {
                Func<TContext, Guid, Task<TEntity>> guidQuery = null;
                Func<TContext, Guid, string, Task<TEntity>> guidCompoundQuery = null;

                return (TContext context, GrainId grainId) =>
                {
                    GrainIdKeyExtensions.TryGetGuidKey(grainId, out Guid guidKey, out string guidKeyExt);

                    if (!string.IsNullOrWhiteSpace(guidKeyExt))
                    {
                        EnsureKeyExtConfigured(options);
                        guidCompoundQuery ??= ExpressionHelper.CreateCompoundQuery<TContext, TState, TEntity, Guid>(options);
                        return guidCompoundQuery(context, guidKey, guidKeyExt);
                    }

                    guidQuery ??= ExpressionHelper.CreateQuery<TContext, TState, TEntity, Guid>(options);
                    return guidQuery(context, guidKey);
                };
            }

            if (keyType == typeof(long))
            {
                Func<TContext, long, Task<TEntity>> longQuery = null;
                Func<TContext, long, string, Task<TEntity>> longCompoundQuery = null;

                return (TContext context, GrainId grainId) =>
                {
                    GrainIdKeyExtensions.TryGetIntegerKey(grainId, out long longKey, out string longKeyExt);

                    if (!string.IsNullOrWhiteSpace(longKeyExt))
                    {
                        EnsureKeyExtConfigured(options);
                        longCompoundQuery ??= ExpressionHelper.CreateCompoundQuery<TContext, TState, TEntity, long>(options);
                        return longCompoundQuery(context, longKey, longKeyExt);
                    }

                    longQuery ??= ExpressionHelper.CreateQuery<TContext, TState, TEntity, long>(options);
                    return longQuery(context, longKey);
                };
            }

            // String key
            Func<TContext, string, Task<TEntity>> stringQuery = null;

            return (TContext context, GrainId grainId) =>
            {
                string stringKey = GetStringKey(grainId);
                stringQuery ??= ExpressionHelper.CreateQuery<TContext, TState, TEntity, string>(options);
                return stringQuery(context, stringKey);
            };
        }

        public virtual Func<TContext, GrainId, Task<TEntity>>
            CreatePreCompiledDefaultReadStateFunc<TContext, TState, TEntity>(
                GrainStorageOptions<TContext, TState, TEntity> options)
            where TContext : DbContext
            where TEntity : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            // Determine entity key type at setup time to avoid mismatched key extraction at runtime.
            Type keyType = GetEntityKeyType<TEntity>(options);

            if (keyType == typeof(Guid))
            {
                Func<TContext, Guid, Task<TEntity>> guidQuery = null;
                Func<TContext, Guid, string, Task<TEntity>> guidCompoundQuery = null;

                return (TContext context, GrainId grainId) =>
                {
                    GrainIdKeyExtensions.TryGetGuidKey(grainId, out Guid guidKey, out string guidKeyExt);

                    if (!string.IsNullOrWhiteSpace(guidKeyExt))
                    {
                        EnsureKeyExtConfigured(options);
                        guidCompoundQuery ??= ExpressionHelper.CreateCompiledCompoundQuery<TContext, TState, TEntity, Guid>(options);
                        return guidCompoundQuery(context, guidKey, guidKeyExt);
                    }

                    guidQuery ??= ExpressionHelper.CreateCompiledQuery<TContext, TState, TEntity, Guid>(options);
                    return guidQuery(context, guidKey);
                };
            }

            if (keyType == typeof(long))
            {
                Func<TContext, long, Task<TEntity>> longQuery = null;
                Func<TContext, long, string, Task<TEntity>> longCompoundQuery = null;

                return (TContext context, GrainId grainId) =>
                {
                    GrainIdKeyExtensions.TryGetIntegerKey(grainId, out long longKey, out string longKeyExt);

                    if (!string.IsNullOrWhiteSpace(longKeyExt))
                    {
                        EnsureKeyExtConfigured(options);
                        longCompoundQuery ??= ExpressionHelper.CreateCompiledCompoundQuery<TContext, TState, TEntity, long>(options);
                        return longCompoundQuery(context, longKey, longKeyExt);
                    }

                    longQuery ??= ExpressionHelper.CreateCompiledQuery<TContext, TState, TEntity, long>(options);
                    return longQuery(context, longKey);
                };
            }

            // String key
            Func<TContext, string, Task<TEntity>> stringQuery = null;

            return (TContext context, GrainId grainId) =>
            {
                string stringKey = GetStringKey(grainId);
                stringQuery ??= ExpressionHelper.CreateCompiledQuery<TContext, TState, TEntity, string>(options);
                return stringQuery(context, stringKey);
            };
        }

        public virtual void SetDefaultKeySelectors<TContext, TState, TEntity>(
            GrainStorageOptions<TContext, TState, TEntity> options)
            where TContext : DbContext
            where TEntity : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (options.KeyPropertyName == null)
                options.KeyPropertyName = _options.DefaultGrainKeyPropertyName;

            if (options.KeyExtPropertyName == null)
            {
                // Only apply default KeyExt property name if the entity type actually has it
                PropertyInfo defaultKeyExtProp = typeof(TEntity).GetProperty(
                    _options.DefaultGrainKeyExtPropertyName,
                    BindingFlags.Instance | BindingFlags.Public);

                if (defaultKeyExtProp != null)
                    options.KeyExtPropertyName = _options.DefaultGrainKeyExtPropertyName;
            }


            PropertyInfo idProperty = ReflectionHelper.GetPropertyInfo<TEntity>(
                options.KeyPropertyName ?? _options.DefaultGrainKeyPropertyName);

            Type idType = idProperty.PropertyType;
            if (idType == typeof(Guid))
            {
                if (options.GuidKeySelector == null)
                    options.GuidKeySelector = ReflectionHelper.GetAccessorExpression<TEntity, Guid>(idProperty);
            }
            else if (idType == typeof(long))
            {
                if (options.LongKeySelector == null)
                    options.LongKeySelector = ReflectionHelper.GetAccessorDelegate<TEntity, long>(idProperty);
            }
            else if (idType != typeof(string))
            {
                throw new GrainStorageConfigurationException(
                    $"Unsupported grain key type \"{idType.FullName}\" for {typeof(TEntity).FullName}.{idProperty.Name}.");
            }

            if (!string.IsNullOrWhiteSpace(options.KeyExtPropertyName))
            {
                PropertyInfo keyExtProperty = ReflectionHelper.GetPropertyInfo<TEntity>(options.KeyExtPropertyName);

                if (keyExtProperty.PropertyType != typeof(string))
                    throw new GrainStorageConfigurationException($"Can not use property \"{keyExtProperty.Name}\" " +
                                                                 $"on grain state type \"{typeof(TEntity)}\". " +
                                                                 "KeyExt property must be of type string.");

                if (options.KeyExtSelector == null)
                    options.KeyExtSelector = ReflectionHelper.GetAccessorExpression<TEntity, string>(keyExtProperty);
            }
        }

        private static Type GetEntityKeyType<TEntity>(GrainStorageOptions options)
        {
            PropertyInfo keyProperty = typeof(TEntity).GetProperty(
                options.KeyPropertyName, BindingFlags.Instance | BindingFlags.Public);

            return keyProperty?.PropertyType ?? typeof(string);
        }

        private static void EnsureKeyExtConfigured<TContext, TState, TEntity>(
            GrainStorageOptions<TContext, TState, TEntity> options)
            where TContext : DbContext
            where TEntity : class
        {
            if (string.IsNullOrWhiteSpace(options.KeyExtPropertyName))
                throw new GrainStorageConfigurationException("KeyExtPropertyName must be configured for compound keys.");
        }

        private static string GetStringKey(GrainId grainId)
        {
            return Encoding.UTF8.GetString(grainId.Key.AsSpan());
        }

        #endregion

        #region IsPersisted

        /// <summary>
        /// Creates a method that tests the value of the Id property to default of its type.
        /// </summary>
        /// <param name="options"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public virtual Func<TEntity, bool> CreateIsPersistedFunc<TEntity>(GrainStorageOptions options)
            where TEntity : class
        {
            PropertyInfo idProperty
                = ReflectionHelper.GetPropertyInfo<TEntity>(
                    options.PersistenceCheckPropertyName ?? _options.DefaultPersistenceCheckPropertyName);

            if (!idProperty.CanRead)
                throw new GrainStorageConfigurationException(
                    $"Property \"{idProperty.Name}\" of type \"{idProperty.PropertyType.FullName}\" " +
                    "must have a public getter.");

            MethodInfo methodInfo = typeof(GrainStorageConvention).GetMethod(
                idProperty.PropertyType.IsValueType
                    ? nameof(IsNotDefaultValueType)
                    : nameof(IsNotDefaultReferenceType),
                BindingFlags.Static | BindingFlags.Public);
            if (methodInfo == null)
                throw new Exception("Impossible");

            return (Func<TEntity, bool>)
                Delegate.CreateDelegate(typeof(Func<TEntity, bool>),
                    idProperty,
                    methodInfo.MakeGenericMethod(typeof(TEntity), idProperty.PropertyType));
        }

        public static bool IsNotDefaultValueType<TEntity, TProperty>(
            PropertyInfo propertyInfo, TEntity state)
            where TProperty : struct
        {
            return !((TProperty)propertyInfo.GetValue(state)).Equals(default(TProperty));
        }

        public static bool IsNotDefaultReferenceType<TEntity, TProperty>(
            PropertyInfo propertyInfo, TEntity state)
            where TProperty : class
        {
            return !((TProperty)propertyInfo.GetValue(state)).Equals(default(TProperty));
        }

        #endregion

        #region ETag

        public virtual void FindAndConfigureETag<TContext, TState, TEntity>(
            GrainStorageOptions<TContext, TState, TEntity> options,
            bool throwIfNotFound)
            where TContext : DbContext
            where TEntity : class
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            using (IServiceScope scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TContext>();

                IEntityType entityType = context.Model.FindEntityType(typeof(TEntity));

                if (entityType == null)
                    return;

                if (!FindAndConfigureETag(entityType, options) && throwIfNotFound)
                    throw new GrainStorageConfigurationException(
                        $"Could not find a valid ETag property on type \"{typeof(TEntity).FullName}\".");
            }
        }

        public virtual void ConfigureETag<TContext, TState, TEntity>(
            string propertyName,
            GrainStorageOptions<TContext, TState, TEntity> options)
            where TContext : DbContext
            where TEntity : class
        {
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            if (options == null) throw new ArgumentNullException(nameof(options));

            using (IServiceScope scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TContext>();

                IEntityType entityType = context.Model.FindEntityType(typeof(TEntity));

                if (entityType == null)
                    return;

                ConfigureETag(entityType, propertyName, options);
            }
        }

        private static bool FindAndConfigureETag<TContext, TState, TEntity>(
            IEntityType entityType,
            GrainStorageOptions<TContext, TState, TEntity> options)
            where TContext : DbContext
            where TEntity : class
        {
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));
            if (options == null) throw new ArgumentNullException(nameof(options));

            IEnumerable<IProperty> properties = entityType.GetProperties();

            foreach (IProperty property in properties)
            {
                if (!property.IsConcurrencyToken)
                    continue;

                ConfigureETag(property, options);

                return true;
            }

            return false;
        }


        private static void ConfigureETag<TContext, TState, TEntity>(
            IEntityType entityType,
            string propertyName,
            GrainStorageOptions<TContext, TState, TEntity> options)
            where TContext : DbContext
            where TEntity : class
        {
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            if (options == null) throw new ArgumentNullException(nameof(options));

            IProperty property = entityType.FindProperty(propertyName);

            if (property == null)
                throw new GrainStorageConfigurationException(
                    $"Property {propertyName} on model{typeof(TEntity).FullName} not found.");

            ConfigureETag(property, options);
        }


        private static void ConfigureETag<TContext, TState, TEntity>(
            IProperty property,
            GrainStorageOptions<TContext, TState, TEntity> options)
            where TContext : DbContext
            where TEntity : class
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            if (!property.IsConcurrencyToken)
                throw new GrainStorageConfigurationException($"Property {property.Name} is not a concurrency token.");

            options.CheckForETag = true;
            options.ETagPropertyName = property.Name;
            options.ETagProperty = property;
            options.ETagType = property.ClrType;

            options.GetETagFunc = CreateGetETagFunc<TEntity>(property.Name);
            options.ConvertETagObjectToStringFunc
                = CreateConvertETagObjectToStringFunc();
        }

        private static Func<TEntity, string> CreateGetETagFunc<TEntity>(string propertyName)
        {
            PropertyInfo propertyInfo = ReflectionHelper.GetPropertyInfo<TEntity>(propertyName);

            var getterDelegate = (Func<TEntity, object>)Delegate.CreateDelegate(
                typeof(Func<TEntity, object>),
                null,
                propertyInfo.GetMethod);

            return state => ConvertETagObjectToString(getterDelegate(state));
        }

        private static Func<object, string> CreateConvertETagObjectToStringFunc()
        {
            return ConvertETagObjectToString;
        }

        private static string ConvertETagObjectToString(object obj)
        {
            if (obj == null)
                return null;
            switch (obj)
            {
                case byte[] bytes:
                    return ByteToHexBitFiddle(bytes);
                default:
                    return obj.ToString();
            }
        }

        private static string ByteToHexBitFiddle(byte[] bytes)
        {
            var c = new char[bytes.Length * 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                int b = bytes[i] >> 4;
                c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
                b = bytes[i] & 0xF;
                c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
            }

            return new string(c);
        }

        #endregion
    }
}