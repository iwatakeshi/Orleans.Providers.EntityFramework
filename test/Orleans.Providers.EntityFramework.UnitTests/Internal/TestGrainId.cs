using System;
using Orleans;
using Orleans.Providers.EntityFramework.UnitTests.Grains;
using Orleans.Providers.EntityFramework.UnitTests.Models;
using Orleans.Runtime;

namespace Orleans.Providers.EntityFramework.UnitTests.Internal
{
    public static class TestGrainId
    {
        public static GrainId Create<TKey>(Entity<TKey> state) => state switch
        {
            EntityWithGuidKey g => Create<GrainWithGuidKey>(g.Id),
            EntityWithGuidCompoundKey g => Create<GrainWithGuidCompoundKey>(g.Id, g.KeyExt),
            EntityWithIntegerKey g => Create<GrainWithIntegerKey>(g.Id),
            EntityWithIntegerCompoundKey g => Create<GrainWithIntegerCompoundKey>(g.Id, g.KeyExt),
            EntityWithStringKey g => Create<GrainWithStringKey>(g.Id),
            _ => throw new ArgumentException($"Unexpected type {state.GetType().Name}.", nameof(state))
        };

        public static GrainId Create<TGrain>(Guid guid)
            where TGrain : IGrainWithGuidKey
        {
            var grainType = GrainType.Create(typeof(TGrain).FullName);
            var key = GrainIdKeyExtensions.CreateGuidKey(guid);
            return GrainId.Create(grainType, key);
        }

        public static GrainId Create<TGrain>(Guid guid, string keyExt)
            where TGrain : IGrainWithGuidCompoundKey
        {
            var grainType = GrainType.Create(typeof(TGrain).FullName);
            var key = GrainIdKeyExtensions.CreateGuidKey(guid, keyExt);
            return GrainId.Create(grainType, key);
        }

        public static GrainId Create<TGrain>(long id)
            where TGrain : IGrainWithIntegerKey
        {
            var grainType = GrainType.Create(typeof(TGrain).FullName);
            var key = GrainIdKeyExtensions.CreateIntegerKey(id);
            return GrainId.Create(grainType, key);
        }

        public static GrainId Create<TGrain>(long id, string keyExt)
            where TGrain : IGrainWithIntegerCompoundKey
        {
            var grainType = GrainType.Create(typeof(TGrain).FullName);
            var key = GrainIdKeyExtensions.CreateIntegerKey(id, keyExt);
            return GrainId.Create(grainType, key);
        }

        public static GrainId Create<TGrain>(string stringKey)
            where TGrain : IGrainWithStringKey
        {
            var grainType = GrainType.Create(typeof(TGrain).FullName);
            var key = IdSpan.Create(stringKey);
            return GrainId.Create(grainType, key);
        }
    }
}