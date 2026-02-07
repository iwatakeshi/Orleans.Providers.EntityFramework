using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    public class ConcurrencyTests
    {
        private readonly IGrainStorage _storage;
        private readonly IServiceProvider _serviceProvider;

        public ConcurrencyTests(GrainStorageFixture storageFixture)
        {
            _storage = storageFixture.Storage;
            _serviceProvider = storageFixture.ServiceProvider;
        }

        [Fact]
        public async Task StateShouldContainETag()
        {
            TestGrainState<EntityWithIntegerKeyWithEtag> grainState =
                Internal.Utils.CreateAndStoreGrainState<EntityWithIntegerKeyWithEtag>(_serviceProvider);

            GrainId grainId
                = TestGrainId.Create(grainState.State);

            await _storage.ReadStateAsync(typeof(EntityWithIntegerKeyWithEtag).FullName,
                grainId,
                grainState);

            string expected = BitConverter.ToString(grainState.State.ETag)
                .Replace("-", string.Empty);

            Assert.Equal(expected, grainState.ETag);
        }


        [Fact]
        public async Task WriteWithETagViolation()
        {
            TestGrainState<EntityWithIntegerKeyWithEtag> grainState =
                Internal.Utils.CreateAndStoreGrainState<EntityWithIntegerKeyWithEtag>(_serviceProvider);

            GrainId grainId
                = TestGrainId.Create(grainState.State);

            // Read to get the correct ETag from the database
            await _storage.ReadStateAsync(typeof(EntityWithIntegerKeyWithEtag).FullName,
                grainId,
                grainState);

            // Update the database via direct access (simulating a concurrent write)
            EntityWithIntegerKeyWithEtag clone = grainState.State.Clone();
            clone.Title = "Updated";
            using (var context = _serviceProvider.GetRequiredService<TestDbContext>())
            {
                context.Entry(clone).State = EntityState.Modified;
                context.SaveChanges();
            }

            // This should fail because the DB ETag has changed
            grainState.State.Title = "Failing Update";
            await Assert.ThrowsAsync<InconsistentStateException>(() =>
                _storage.WriteStateAsync(typeof(EntityWithIntegerKeyWithEtag).FullName,
                    grainId,
                    grainState));
        }

        [Fact]
        public async Task WriteWithETagSuccess()
        {
            TestGrainState<EntityWithIntegerKeyWithEtag> grainState =
                Internal.Utils.CreateAndStoreGrainState<EntityWithIntegerKeyWithEtag>(_serviceProvider);

            GrainId grainId
                = TestGrainId.Create(grainState.State);

            // Read to get the correct ETag from the database
            await _storage.ReadStateAsync(typeof(EntityWithIntegerKeyWithEtag).FullName,
                grainId,
                grainState);

            grainState.State.Title = "Updated";

            await _storage.WriteStateAsync(typeof(EntityWithIntegerKeyWithEtag).FullName,
                grainId,
                grainState);

            string expected = BitConverter.ToString(grainState.State.ETag)
                .Replace("-", string.Empty);

            Assert.Equal(expected, grainState.ETag);
        }

        [Fact]
        public async Task ReadTaggedEntityShouldSuccessForNullState()
        {
            TestGrainState<EntityWithIntegerKeyWithEtag> grainState =
                new TestGrainState<EntityWithIntegerKeyWithEtag>();

            GrainId grainId
                = TestGrainId.Create<GrainWithIntegerKeyWithEtag>(0);

            await _storage.ReadStateAsync(typeof(EntityWithIntegerKeyWithEtag).FullName,
                grainId,
                grainState);

            Assert.Null(grainState.ETag);
        }

        [Fact]
        public async Task ReadTaggedEntityShouldSuccessForNullEtag()
        {
            TestGrainState<EntityWithIntegerKeyWithEtag> grainState =
                Internal.Utils.StoreGrainState<EntityWithIntegerKeyWithEtag>(_serviceProvider,
                new EntityWithIntegerKeyWithEtag
                {
                    ETag = null
                });

            GrainId grainId
                = TestGrainId.Create(grainState.State);

            await _storage.ReadStateAsync(typeof(EntityWithIntegerKeyWithEtag).FullName,
                grainId,
                grainState);

            Assert.Null(grainState.ETag);
        }
    }
}