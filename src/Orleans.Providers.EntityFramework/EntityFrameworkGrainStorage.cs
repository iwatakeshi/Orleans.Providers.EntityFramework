using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Providers.EntityFramework.Exceptions;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Providers.EntityFramework;

public class EntityFrameworkGrainStorage<TContext>(
    IServiceProvider serviceProvider,
    IEntityTypeResolver entityTypeResolver) : IGrainStorage
    where TContext : DbContext
{
    private readonly ConcurrentDictionary<StateStorageKey, IGrainStorage> _storage = new();

    public Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        IGrainStorage storage = GetOrCreateStorage(stateName, grainState);
        return storage.ReadStateAsync(stateName, grainId, grainState);
    }

    public Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        IGrainStorage storage = GetOrCreateStorage(stateName, grainState);
        return storage.WriteStateAsync(stateName, grainId, grainState);
    }

    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        IGrainStorage storage = GetOrCreateStorage(stateName, grainState);
        return storage.ClearStateAsync(stateName, grainId, grainState);
    }

    private IGrainStorage GetOrCreateStorage<T>(string stateName, IGrainState<T> grainState)
    {
        Type stateType = grainState.State?.GetType() ?? typeof(T);
        var key = new StateStorageKey(stateName, stateType);
        return _storage.GetOrAdd(key, _ => CreateStorage(stateName, stateType));
    }

    private IGrainStorage CreateStorage(string stateName, Type stateType)
    {
        ArgumentNullException.ThrowIfNull(stateType);

        Type entityType = entityTypeResolver.ResolveEntityType(stateType);

        Type storageType = typeof(GrainStorage<,,>)
            .MakeGenericType(typeof(TContext), stateType, entityType);

        IGrainStorage storage;

        try
        {
            storage = (IGrainStorage)Activator.CreateInstance(storageType, stateName, serviceProvider)!;
        }
        catch (Exception e) when (e.InnerException is GrainStorageConfigurationException)
        {
            throw e.InnerException;
        }

        return storage;
    }

    private readonly struct StateStorageKey(string stateName, Type stateType) : IEquatable<StateStorageKey>
    {
        public string StateName { get; } = stateName ?? string.Empty;

        public Type StateType { get; } = stateType ?? throw new ArgumentNullException(nameof(stateType));

        public bool Equals(StateStorageKey other)
            => StringComparer.Ordinal.Equals(StateName, other.StateName) && StateType == other.StateType;

        public override bool Equals(object? obj)
            => obj is StateStorageKey other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(StringComparer.Ordinal.GetHashCode(StateName), StateType);
    }
}