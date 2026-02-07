using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework
{
    internal delegate Task ReadWriteStateAsyncDelegate<TState>(string stateName, GrainId grainId,
        IGrainState<TState> grainState, object storageOptions);
}