using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Providers.EntityFramework.Exceptions;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Providers.EntityFramework
{
    public class EntityFrameworkGrainStorage<TContext> : IGrainStorage
        where TContext : DbContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEntityTypeResolver _entityTypeResolver;

        private readonly ConcurrentDictionary<StateStorageKey, IGrainStorage> _storage
            = new ConcurrentDictionary<StateStorageKey, IGrainStorage>();

        public EntityFrameworkGrainStorage(
            IServiceProvider serviceProvider,
            IEntityTypeResolver entityTypeResolver)
        {
            _serviceProvider = serviceProvider;
            _entityTypeResolver = entityTypeResolver;
        }

        public Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            Type stateType = grainState.State?.GetType() ?? typeof(T);
            var key = new StateStorageKey(stateName, stateType);

            if (!_storage.TryGetValue(key, out IGrainStorage storage))
                storage = CreateStorage(stateName, stateType);

            return storage.ReadStateAsync(stateName, grainId, grainState);
        }

        public Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            Type stateType = grainState.State?.GetType() ?? typeof(T);
            var key = new StateStorageKey(stateName, stateType);

            if (!_storage.TryGetValue(key, out IGrainStorage storage))
                storage = CreateStorage(stateName, stateType);

            return storage.WriteStateAsync(stateName, grainId, grainState);
        }

        public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            Type stateType = grainState.State?.GetType() ?? typeof(T);
            var key = new StateStorageKey(stateName, stateType);

            if (!_storage.TryGetValue(key, out IGrainStorage storage))
                storage = CreateStorage(stateName, stateType);

            return storage.ClearStateAsync(stateName, grainId, grainState);
        }

        private IGrainStorage CreateStorage(string stateName, Type stateType)
        {
            if (stateType == null) throw new ArgumentNullException(nameof(stateType));

            Type entityType = _entityTypeResolver.ResolveEntityType(stateType);

            Type storageType = typeof(GrainStorage<,,>)
                .MakeGenericType(typeof(TContext), stateType, entityType);

            IGrainStorage storage;

            try
            {
                storage = (IGrainStorage)Activator.CreateInstance(storageType, stateName, _serviceProvider);
            }
            catch (Exception e) when (e.InnerException is GrainStorageConfigurationException)
            {
                throw e.InnerException;
            }

            _storage.TryAdd(new StateStorageKey(stateName, stateType), storage);
            return storage;
        }

        private readonly struct StateStorageKey : IEquatable<StateStorageKey>
        {
            public StateStorageKey(string stateName, Type stateType)
            {
                StateName = stateName ?? string.Empty;
                StateType = stateType ?? throw new ArgumentNullException(nameof(stateType));
            }

            public string StateName { get; }

            public Type StateType { get; }

            public bool Equals(StateStorageKey other)
            {
                return StringComparer.Ordinal.Equals(StateName, other.StateName) && StateType == other.StateType;
            }

            public override bool Equals(object obj)
            {
                return obj is StateStorageKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(StringComparer.Ordinal.GetHashCode(StateName), StateType);
            }
        }
    }
}