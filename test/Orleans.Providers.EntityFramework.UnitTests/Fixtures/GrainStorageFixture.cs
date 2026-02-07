using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Providers.EntityFramework.Conventions;
using Orleans.Providers.EntityFramework.Extensions;
using Orleans.Providers.EntityFramework.UnitTests.Grains;
using Orleans.Providers.EntityFramework.UnitTests.Internal;
using Orleans.Providers.EntityFramework.UnitTests.Models;
using Orleans.Storage;

namespace Orleans.Providers.EntityFramework.UnitTests.Fixtures
{
    public class GrainStorageFixture
    {
        public IServiceProvider ServiceProvider { get; }
        public IGrainStorage Storage { get; }

        public GrainStorageFixture()
        {
            var services = new ServiceCollection();

            services
                .AddLogging(logging => logging
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Trace)
                )

                // Entity framework
                .AddEntityFrameworkInMemoryDatabase()
                .AddDbContextPool<TestDbContext>(builder =>
                {
                    builder.UseInMemoryDatabase(Guid.NewGuid().ToString());
                    builder.EnableSensitiveDataLogging();
                })
                // Storage
                .AddEfGrainStorage<TestDbContext>()
                .AddSingleton<IGrainStorageConvention, TestGrainStorageConvention>()
                .AddSingleton<IEntityTypeResolver, TestEntityTypeResolver>();


            ConfigureGrainStorage(services);

            ServiceProvider = services.BuildServiceProvider();

            Storage = ServiceProvider.GetRequiredService<IGrainStorage>();


            using (IServiceScope scope = ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                // this is required to make sure data are seeded
                context.Database.EnsureCreated();
            }
        }

        private void ConfigureGrainStorage(IServiceCollection services)
        {
            services.ConfigureGrainStorageOptions<TestDbContext, ConfiguredEntityWithCustomGuidKey,
                    ConfiguredEntityWithCustomGuidKey>(
                    options =>
                    {
                        options
                            .UseKey(entity => entity.CustomKey);
                    },
                    typeof(ConfiguredGrainWithCustomGuidKey).FullName)
                .ConfigureGrainStorageOptions<TestDbContext, ConfiguredEntityWithCustomGuidKey,
                    ConfiguredEntityWithCustomGuidKey>(
                    options => options
                        .UseKey(entity => entity.CustomKey)
                        .UseKeyExt(entity => entity.CustomKeyExt)
                ,
                    typeof(ConfiguredGrainWithCustomGuidKey2).FullName)
                .ConfigureGrainStorageOptions<TestDbContext, InvalidConfiguredEntityWithCustomGuidKey,
                    InvalidConfiguredEntityWithCustomGuidKey>(
                    options => options
                        .UseKey(entity => entity.CustomKey)
                        .UseKeyExt(entity => entity.CustomKeyExt),
                    typeof(InvalidConfiguredGrainWithGuidKey).FullName)
                .Configure<GrainStorageConventionOptions>(options =>
                {
                    options.DefaultGrainKeyPropertyName = nameof(EntityWithGuidKey.Id);
                    options.DefaultGrainKeyExtPropertyName = nameof(EntityWithGuidKey.KeyExt);
                    options.DefaultPersistenceCheckPropertyName = nameof(EntityWithGuidKey.IsPersisted);
                });

            // No PreCompilation
            services
                .ConfigureGrainStorageOptions<TestDbContext, EntityWithGuidKey>(
                    options => options.PreCompileReadQuery(false))
                .ConfigureGrainStorageOptions<TestDbContext, EntityWithGuidCompoundKey>(
                    options => options.PreCompileReadQuery(false))
                .ConfigureGrainStorageOptions<TestDbContext, EntityWithIntegerKey>(
                    options => options.PreCompileReadQuery(false))
                .ConfigureGrainStorageOptions<TestDbContext, EntityWithIntegerCompoundKey>(
                    options => options.PreCompileReadQuery(false))
                .ConfigureGrainStorageOptions<TestDbContext, EntityWithStringKey>(
                    options => options.PreCompileReadQuery(false))
                .ConfigureGrainStorageOptions<TestDbContext, GrainStateWrapper<EntityWithGuidKey>>(
                    options => options.PreCompileReadQuery(false))
                ;

        }
    }

    public class TestEntityTypeResolver : EntityTypeResolver
    {
        public override Type ResolveEntityType(Type stateType)
        {
            if (stateType == typeof(GrainStateWrapper<EntityWithGuidKey>))
                return typeof(EntityWithGuidKey);

            return stateType;
        }
    }

    public class TestGrainStorageConvention : GrainStorageConvention
    {
        public TestGrainStorageConvention(
            IOptions<GrainStorageConventionOptions> options, IServiceScopeFactory serviceScopeFactory) : base(options,
            serviceScopeFactory)
        {
        }

        public override Func<IGrainState<TState>, TEntity> GetGetterFunc<TState, TEntity>()
        {
            if (typeof(TState) == typeof(GrainStateWrapper<TEntity>))
                return state =>
                    (state.State as GrainStateWrapper<TEntity>)?.Value;

            return stat => stat.State as TEntity;
        }

        public override Action<IGrainState<TState>, TEntity> GetSetterFunc<TState, TEntity>()
        {
            if (typeof(TState) == typeof(GrainStateWrapper<TEntity>))
                return (state, entity) =>
                {
                    if (state.State is GrainStateWrapper<TEntity> wrapper)
                        wrapper.Value = entity;
                    else
                        state.State = (TState)(object)new GrainStateWrapper<TEntity>
                        {
                            Value = entity
                        };
                };

            return (state, entity) =>
            {
                if (entity is TState typed)
                    state.State = typed;
                else
                    throw new InvalidOperationException(
                        $"State type \"{typeof(TState).FullName}\" is not assignable from \"{typeof(TEntity).FullName}\".");
            };
        }
    }
}