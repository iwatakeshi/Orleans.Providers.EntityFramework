using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Providers.EntityFramework
{
    internal class GrainStorage<TContext, TState, TEntity> : IGrainStorage
        where TContext : DbContext
        where TState : class
        where TEntity : class
    {
        private readonly GrainStorageOptions<TContext, TState, TEntity> _options;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GrainStorage<TContext, TState, TEntity>> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IGrainStateEntryConfigurator<TContext, TEntity> _entryConfigurator;

        public GrainStorage(string stateName, IServiceProvider serviceProvider)
        {
            if (stateName == null) throw new ArgumentNullException(nameof(stateName));

            _serviceProvider = serviceProvider
                               ?? throw new ArgumentNullException(nameof(serviceProvider));

            _entryConfigurator = (IGrainStateEntryConfigurator<TContext, TEntity>)serviceProvider.GetRequiredService(
                typeof(IGrainStateEntryConfigurator<TContext, TEntity>));

            var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
            _logger = loggerFactory?.CreateLogger<GrainStorage<TContext, TState, TEntity>>()
                      ?? NullLogger<GrainStorage<TContext, TState, TEntity>>.Instance;

            _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            _options = GetOrCreateDefaultOptions(stateName);
        }

        public Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            if (grainState is not IGrainState<TState> typedState)
                throw new ArgumentException($"Unexpected grain state type {typeof(T).FullName}.", nameof(grainState));

            return ReadStateAsync(stateName, grainId, typedState);
        }

        public Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            if (grainState is not IGrainState<TState> typedState)
                throw new ArgumentException($"Unexpected grain state type {typeof(T).FullName}.", nameof(grainState));

            return WriteStateAsync(stateName, grainId, typedState);
        }

        public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            if (grainState is not IGrainState<TState> typedState)
                throw new ArgumentException($"Unexpected grain state type {typeof(T).FullName}.", nameof(grainState));

            return ClearStateAsync(stateName, grainId, typedState);
        }

        private async Task ReadStateAsync(string stateName, GrainId grainId, IGrainState<TState> grainState)
        {
            using (IServiceScope scope = _scopeFactory.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<TContext>())
            {
                TEntity entity = await _options.ReadStateAsync(context, grainId)
                    .ConfigureAwait(false);

                grainState.RecordExists = entity != null;

                if (entity != null)
                {
                    _options.SetEntity(grainState, entity);

                    if (_options.CheckForETag)
                        grainState.ETag = _options.GetETagFunc(entity);
                }
            }
        }

        private async Task WriteStateAsync(string stateName, GrainId grainId, IGrainState<TState> grainState)
        {
            TEntity entity = _options.GetEntity(grainState);

            using (IServiceScope scope = _scopeFactory.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<TContext>())
            {
                if (GrainStorageContext<TEntity>.IsConfigured)
                {
                    EntityEntry<TEntity> entry = context.Entry(entity);
                    GrainStorageContext<TEntity>.ConfigureStateDelegate(entry);
                }
                else
                {
                    bool isPersisted = _options.IsPersistedFunc(entity);

                    _entryConfigurator.ConfigureSaveEntry(
                        new ConfigureSaveEntryContext<TContext, TEntity>(
                            context, entity)
                        {
                            IsPersisted = isPersisted
                        });
                }

                try
                {
                    await context.SaveChangesAsync()
                        .ConfigureAwait(false);

                    grainState.RecordExists = true;
                    if (_options.CheckForETag)
                        grainState.ETag = _options.GetETagFunc(entity);
                }
                catch (DbUpdateConcurrencyException e)
                {
                    if (!_options.CheckForETag)
                        throw new InconsistentStateException(e.Message, e);

                    object storedETag = e.Entries.First().OriginalValues[_options.ETagProperty];
                    throw new InconsistentStateException(e.Message,
                        _options.ConvertETagObjectToStringFunc(storedETag),
                        grainState.ETag,
                        e);
                }
            }
        }

        private async Task ClearStateAsync(string stateName, GrainId grainId, IGrainState<TState> grainState)
        {
            TEntity entity = _options.GetEntity(grainState);
            using (IServiceScope scope = _scopeFactory.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<TContext>())
            {
                if (entity != null)
                {
                    context.Remove(entity);
                    await context.SaveChangesAsync()
                        .ConfigureAwait(false);
                }

                grainState.RecordExists = false;
                grainState.ETag = null;
            }
        }


        private GrainStorageOptions<TContext, TState, TEntity> GetOrCreateDefaultOptions(string stateName)
        {
            var options
                = _serviceProvider.GetOptionsByName<GrainStorageOptions<TContext, TState, TEntity>>(stateName);

            if (options.IsConfigured)
                return options;

            string fallbackName = typeof(TState).FullName;
            if (!string.IsNullOrWhiteSpace(fallbackName) && !string.Equals(stateName, fallbackName, StringComparison.Ordinal))
            {
                var fallbackOptions
                    = _serviceProvider.GetOptionsByName<GrainStorageOptions<TContext, TState, TEntity>>(fallbackName);
                if (fallbackOptions.IsConfigured)
                    return fallbackOptions;
            }

            // Try generating a default options for the grain

            Type optionsType = typeof(GrainStoragePostConfigureOptions<,,>)
                .MakeGenericType(
                    typeof(TContext),
                    typeof(TState),
                    typeof(TEntity));

            var postConfigure = (IPostConfigureOptions<GrainStorageOptions<TContext, TState, TEntity>>)
                Activator.CreateInstance(optionsType, _serviceProvider);

            postConfigure.PostConfigure(stateName, options);

            _logger.LogInformation($"GrainStorageOptions is not configured for state {stateName} " +
                                   "and default options will be used. If default configuration is not desired, " +
                                   "consider configuring options for state using " +
                                   "IServiceCollection.ConfigureGrainStorageOptions<TContext, TState, TEntity> extension method.");

            return options;
        }
    }
}