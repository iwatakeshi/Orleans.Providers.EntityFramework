# Orleans.Providers.EntityFramework

An Entity Framework Core implementation of Orleans Grain Storage.

[![CI](https://github.com/alirezajm/Orleans.Providers.EntityFramework/actions/workflows/ci.yml/badge.svg)](https://github.com/alirezajm/Orleans.Providers.EntityFramework/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Orleans.Providers.EntityFramework.svg)](https://www.nuget.org/packages/Orleans.Providers.EntityFramework)

## Requirements

- .NET 10+
- Orleans 10.x
- Entity Framework Core 10.x

## Usage

```
dotnet add package Orleans.Providers.EntityFramework
```

Configure the storage provider on the silo builder:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.UseOrleans(siloBuilder =>
{
    siloBuilder.AddEfGrainStorage<FrogsDbContext>("ef");
});
```

This requires your `DbContext` to be registered as well:

```csharp
builder.Services.AddDbContextPool<FrogsDbContext>(options => { });
```

The storage provider creates a new DI scope and resolves the `DbContext` per operation, so you won't have long-lived contexts. Using `AddDbContextPool` is recommended for performance.

## Configuration

By default the provider will search for key properties on your data models that match your grain interfaces,
but you can change the default behavior:

```csharp
builder.Services.Configure<GrainStorageConventionOptions>(options =>
{
    options.DefaultGrainKeyPropertyName = "Id";
    options.DefaultPersistenceCheckPropertyName = "Id";
    options.DefaultGrainKeyExtPropertyName = "KeyExt";
});
```

**DefaultPersistenceCheckPropertyName** is used to determine whether a model needs to be inserted or updated.
The value of the property is compared against the default value of its type.

The following model works out of the box for a grain implementing `IGrainWithGuidCompoundKey` with no additional configuration:

```csharp
public class Box
{
    public Guid Id { get; set; }
    public string KeyExt { get; set; }
    public byte[] ETag { get; set; }
}
```

_If you use conventions, your context should contain `DbSet` properties for your models:_

```csharp
public DbSet<Box> Boxes { get; set; }
```

### Configuring custom keys

To map a model with non-standard key properties:

```csharp
builder.Services
    .ConfigureGrainStorageOptions<FatDbContext, SpecialBoxGrain, SpecialBox>(
        options => options
            .UseKey(box => box.WeirdId)
            .UseKeyExt(box => box.Type)
    );
```

### Custom read state queries

You can provide a fully custom read delegate:

```csharp
builder.Services
    .ConfigureGrainStorageOptions<FatDbContext, SpecialBoxGrain, SpecialBox>(
        options => options
            .ConfigureReadState(async (context, grainId) =>
            {
                GrainIdKeyExtensions.TryGetGuidKey(grainId, out Guid key, out _);
                return await context.SpecialBoxes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == key);
            })
    );
```

### Loading additional data on read

You can load navigation properties by customizing the query source:

```csharp
builder.Services
    .ConfigureGrainStorageOptions<FatDbContext, SpecialBoxGrain, SpecialBox>(
        options => options
            .UseQuery(context => context.SpecialBoxes
                .AsNoTracking()
                .Include(box => box.Gems)
                .ThenInclude(gem => gem.Map))
    );
```

### Custom persistence check

When using Guids as primary keys you may have a separate auto-incremented cluster index.
That field can be used to check if the state already exists in the database:

```csharp
builder.Services
    .ConfigureGrainStorageOptions<FatDbContext, SpecialBoxGrain, SpecialBox>(
        options => options
            .ConfigureIsPersisted(box => box.ClusterIndexId > 0)
    );
```

or

```csharp
builder.Services
    .ConfigureGrainStorageOptions<FatDbContext, SpecialBoxGrain, SpecialBox>(
        options => options
            .CheckPersistenceOn(box => box.ClusterIndexId)
    );
```

To change the default for all models:

```csharp
builder.Services.Configure<GrainStorageConventionOptions>(options =>
{
    options.DefaultPersistenceCheckPropertyName = "ClusterIndexId";
});
```

### ETags

By default models are searched for ETags. If a property on a model is marked as a **ConcurrencyToken**, the storage will pick it up automatically.

Using the EF Core fluent API:

```csharp
modelBuilder.Entity<SpecialBox>()
    .Property(e => e.ETag)
    .IsConcurrencyToken();
```

You can also explicitly configure ETags using extensions:

```csharp
// Auto-detect — throws if no valid concurrency token is found
builder.Services
    .ConfigureGrainStorageOptions<FatDbContext, SpecialBoxGrain, SpecialBox>(
        options => options.UseETag()
    );

// Explicit property — throws if not marked as ConcurrencyCheck
builder.Services
    .ConfigureGrainStorageOptions<FatDbContext, SpecialBoxGrain, SpecialBox>(
        options => options.UseETag(box => box.ETag)
    );
```

When a concurrency conflict occurs during `WriteStateAsync`, the provider throws `InconsistentStateException` (the standard Orleans exception for ETag violations), wrapping the underlying `DbUpdateConcurrencyException`.

## Controlling how the state is saved

When calling `WriteStateAsync`, the state object is attached to the context and its entry state is set to `Added` or `Modified`.

There are two ways to override this behavior:

### GrainStorageContext

```csharp
GrainStorageContext<Box>.ConfigureEntryState(
    entry => entry.Property(e => e.Title).IsModified = true);
```

This way only the `Title` field would be updated.

Things to consider:

- When configuring the entry manually, the storage provider only attaches the state to the context and doesn't set the entry state. For example, `GrainStorageContext<Box>.ConfigureEntryState(entry => { });` would be a no-op.
- Because `GrainStorageContext` uses async locals, you must call `GrainStorageContext<Box>.Clear()` if you want to do multiple writes in the same asynchronous operation.

### IGrainStateEntryConfigurator

Implement `IGrainStateEntryConfigurator<TContext, TEntity>` and register it.

The default implementation simply sets the entry state:

```csharp
public void ConfigureSaveEntry(ConfigureSaveEntryContext<TContext, TEntity> context)
{
    EntityEntry<TEntity> entry = context.DbContext.Entry(context.Entity);

    entry.State = context.IsPersisted
        ? EntityState.Modified
        : EntityState.Added;
}
```

## Precompiled Queries

By default all queries are precompiled, unless overridden via `ConfigureReadState`.

You can disable precompilation per state type:

```csharp
builder.Services
    .ConfigureGrainStorageOptions<FatDbContext, SpecialBoxGrain, SpecialBox>(
        options => options.PreCompileReadQuery(false)
    );
```

## Conventions

You can change the conventions by implementing `IGrainStorageConvention` or inheriting from `GrainStorageConvention` (used for all types), and `IGrainStorageConvention<TContext, TGrain, TEntity>` for a specific grain type (no default implementation).

## Custom Grain State Setter/Getter

You can implement `IEntityTypeResolver` or inherit from `EntityTypeResolver` to have different grain state and storage models. This is useful for abstract states or models without public default constructors, which is a constraint on Orleans grain states.

For example:

```csharp
class GenericGrainState<TEntity>
{
    public TEntity Value { get; set; }
}
```

Using a custom `EntityTypeResolver`, you can tell the storage that `TEntity` is the persistent model.

## Multi-Tenancy

The library is fully compatible with multi-tenant architectures. Because a new DI scope and `DbContext` instance is resolved for every storage operation, any tenant-aware `DbContext` factory works transparently.

### Query filter approach (single database or schema-per-tenant)

Configure EF Core global query filters that read the current tenant from a scoped service:

```csharp
builder.Services.AddScoped<ITenantProvider, RequestContextTenantProvider>();

builder.Services.AddDbContextPool<AppDbContext>((sp, options) =>
{
    var tenant = sp.GetRequiredService<ITenantProvider>();
    options.UseSqlServer(tenant.ConnectionString);
});
```

In your `DbContext`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Box>().HasQueryFilter(b => b.TenantId == _tenantProvider.TenantId);
}
```

### Connection-per-tenant approach

Use Orleans `RequestContext` to propagate the tenant identifier from the grain call:

```csharp
// In the client or grain call
RequestContext.Set("TenantId", tenantId);

// Scoped tenant provider
public class RequestContextTenantProvider : ITenantProvider
{
    public string TenantId => RequestContext.Get("TenantId") as string
        ?? throw new InvalidOperationException("Tenant not set.");
}
```

No changes to the storage library are needed — the tenant resolution happens entirely in the DI and `DbContext` configuration layer.

## Known Issues and Limitations

- Since entity types must be configured in the `DbContext` model, arbitrary types cannot use this provider. This causes issues with Orleans internal grains like `VersionStoreGrain`, so this storage provider should be registered as a **named** provider rather than the default grain storage.
