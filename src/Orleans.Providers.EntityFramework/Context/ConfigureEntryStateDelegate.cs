using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Orleans.Providers.EntityFramework;

/// <summary>
/// Delegate used to configure entity entry state before saving.
/// </summary>
/// <typeparam name="TGrainState">The entity type.</typeparam>
/// <param name="entry">The tracked entry to configure.</param>
public delegate void ConfigureEntryStateDelegate<TGrainState>(EntityEntry<TGrainState> entry)
    where TGrainState : class;