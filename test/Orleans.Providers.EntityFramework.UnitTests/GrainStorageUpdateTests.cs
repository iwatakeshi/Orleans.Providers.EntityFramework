using System;
using System.Threading.Tasks;
using Orleans.Providers.EntityFramework.UnitTests.Fixtures;
using Orleans.Providers.EntityFramework.UnitTests.Grains;
using Orleans.Providers.EntityFramework.UnitTests.Internal;
using Orleans.Providers.EntityFramework.UnitTests.Models;
using Orleans.Runtime;
using Orleans.Storage;
using Xunit;

namespace Orleans.Providers.EntityFramework.UnitTests
{
    [Collection(GrainStorageCollection.Name)]
    public class GrainStorageUpdateTests
    {
        private readonly IGrainStorage _storage;
        private readonly IServiceProvider _serviceProvider;

        public GrainStorageUpdateTests(GrainStorageFixture storageFixture)
        {
            _storage = storageFixture.Storage;
            _serviceProvider = storageFixture.ServiceProvider;
        }

        [Fact]
        public Task UpdateGuidKeyState()
        {
            return TestUpdateAsync<GrainWithGuidKey, EntityWithGuidKey, Guid>();

        }

        [Fact]
        public Task UpdateGuidCompoundKeyState()
        {
            return TestUpdateAsync<GrainWithGuidCompoundKey, EntityWithGuidCompoundKey, Guid>();
        }

        [Fact]
        public Task UpdateIntegerKeyState()
        {
            return TestUpdateAsync<GrainWithIntegerKey, EntityWithIntegerKey, long>();
        }

        [Fact]
        public Task UpdateIntegerCompoundKeyState()
        {
            return TestUpdateAsync<GrainWithIntegerCompoundKey, EntityWithIntegerCompoundKey, long>();

        }

        [Fact]
        public Task UpdateStringKeyState()
        {
            return TestUpdateAsync<GrainWithStringKey, EntityWithStringKey, string>();
        }

        [Fact]
        public async Task UpdateCustomGetterGrainState()
        {
            var entity = new EntityWithGuidKey();
            Internal.Utils.StoreGrainState(_serviceProvider, entity);
            entity.Title += "UPDATED";
            var state = new GrainStateWrapper<EntityWithGuidKey>()
            {
                Value = entity
            };
            var grainState = new TestGrainState<GrainStateWrapper<EntityWithGuidKey>>()
            {
                State = state
            };

            GrainId grainId
                = TestGrainId.Create(entity);

            await _storage.WriteStateAsync(typeof(GrainStateWrapper<EntityWithGuidKey>).FullName,
                grainId,
                grainState
            );

            Internal.Utils.AssertEntityEqualityVsDb(
                _serviceProvider, grainState.State?.Value);

        }

        private async Task TestUpdateAsync<TGrain, TState, TKey>()
            where TState : Entity<TKey>, new()
            where TGrain : Grain<TState>
        {
            TestGrainState<TState> grainState = Internal.Utils.CreateAndStoreGrainState<TState>(_serviceProvider);
            grainState.State.Title += "UPDATED";

            GrainId grainId
                = TestGrainId.Create(grainState.State);

            await _storage.WriteStateAsync(typeof(TState).FullName,
                grainId,
                grainState
            );

            Internal.Utils.AssertEntityEqualityVsDb(_serviceProvider, grainState.State);
        }
    }
}