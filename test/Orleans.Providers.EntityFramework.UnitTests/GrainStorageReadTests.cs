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
    public class GrainStorageReadTests
    {
        private readonly IGrainStorage _storage;
        private readonly IServiceProvider _serviceProvider;

        public GrainStorageReadTests(GrainStorageFixture storageFixture)
        {
            _storage = storageFixture.Storage;
            _serviceProvider = storageFixture.ServiceProvider;
        }

        [Fact]
        public Task ReadGuidKeyState()
        {
            return TestReadAsync<GrainWithGuidKey, EntityWithGuidKey, Guid>();
        }

        [Fact]
        public Task ReadGuidCompoundKeyState()
        {
            return TestReadAsync<GrainWithGuidCompoundKey, EntityWithGuidCompoundKey, Guid>();
        }

        [Fact]
        public Task ReadIntegerKeyState()
        {
            return TestReadAsync<GrainWithIntegerKey, EntityWithIntegerKey, long>();
        }

        [Fact]
        public Task ReadIntegerCompoundKeyState()
        {
            return TestReadAsync<GrainWithIntegerCompoundKey, EntityWithIntegerCompoundKey, long>();
        }

        [Fact]
        public Task ReadStringKeyState()
        {
            return TestReadAsync<GrainWithStringKey, EntityWithStringKey, string>();
        }

        [Fact]
        public async Task ReadCustomGetterGrainState()
        {
            var entity = new EntityWithGuidKey();
            Internal.Utils.StoreGrainState(_serviceProvider, entity);

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

            grainState.State = null;

            await _storage.ReadStateAsync(typeof(GrainStateWrapper<EntityWithGuidKey>).FullName,
                grainId,
                grainState
            );

            Internal.Utils.AssertEntityEqualityVsDb(
                _serviceProvider,
                grainState.State?.Value);

        }

        [Fact]
        public Task ReadGuidKeyStateNoPreCompile()
        {
            return TestReadAsync<GrainWithGuidKeyNoPreCompile, EntityWithGuidKey, Guid>();
        }

        [Fact]
        public Task ReadGuidCompoundKeyStateNoPreCompile()
        {
            return TestReadAsync<GrainWithGuidCompoundKeyNoPreCompile, EntityWithGuidCompoundKey, Guid>();
        }

        [Fact]
        public Task ReadIntegerKeyStateNoPreCompile()
        {
            return TestReadAsync<GrainWithIntegerKeyNoPreCompile, EntityWithIntegerKey, long>();
        }

        [Fact]
        public Task ReadIntegerCompoundKeyStateNoPreCompile()
        {
            return TestReadAsync<GrainWithIntegerCompoundKeyNoPreCompile, EntityWithIntegerCompoundKey, long>();
        }

        [Fact]
        public Task ReadStringKeyStateNoPreCompile()
        {
            return TestReadAsync<GrainWithStringKey, EntityWithStringKey, string>();
        }

        [Fact]
        public async Task ReadCustomGetterGrainStateNoPreCompile()
        {
            var entity = new EntityWithGuidKey();
            Internal.Utils.StoreGrainState(_serviceProvider, entity);

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

            grainState.State = null;

            await _storage.ReadStateAsync(typeof(GrainStateWrapper<EntityWithGuidKey>).FullName,
                grainId,
                grainState
            );

            Internal.Utils.AssertEntityEqualityVsDb(
                _serviceProvider,
                grainState.State?.Value);

        }

        private async Task TestReadAsync<TGrain, TState, TKey>()
            where TState : Entity<TKey>, new()
            where TGrain : Grain<TState>
        {
            TestGrainState<TState> grainState = Internal.Utils.CreateAndStoreGrainState<TState>(_serviceProvider);

            GrainId grainId
                = TestGrainId.Create(grainState.State);

            grainState.State = null;

            await _storage.ReadStateAsync(typeof(TState).FullName,
                grainId,
                grainState
            );

            Internal.Utils.AssertEntityEqualityVsDb(_serviceProvider, grainState.State);
        }
    }
}