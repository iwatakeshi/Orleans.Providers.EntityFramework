using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework;

/// <summary>
/// Delegate signature for reading or writing grain state.
/// </summary>
/// <typeparam name="TState">The grain state type.</typeparam>
/// <param name="stateName">The state name.</param>
/// <param name="grainId">The grain identifier.</param>
/// <param name="grainState">The grain state.</param>
/// <param name="storageOptions">The storage options instance.</param>
internal delegate Task ReadWriteStateAsyncDelegate<TState>(
    string stateName,
    GrainId grainId,
    IGrainState<TState> grainState,
    object storageOptions);