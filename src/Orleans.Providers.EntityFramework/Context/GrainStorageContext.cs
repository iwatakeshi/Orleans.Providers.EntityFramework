namespace Orleans.Providers.EntityFramework;

/// <summary>
/// An async local context to apply modifications to current behavior of write and clear operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public static class GrainStorageContext<TEntity>
    where TEntity : class
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly AsyncLocal<bool> IsConfiguredLocal = new();

    private static readonly AsyncLocal<ConfigureEntryStateDelegate<TEntity>?>
        ConfigureStateDelegateLocal = new();

    internal static bool IsConfigured => IsConfiguredLocal.Value;

    internal static ConfigureEntryStateDelegate<TEntity>? ConfigureStateDelegate
        => ConfigureStateDelegateLocal.Value;

    /// <summary>
    /// Configures the entry state.
    /// Use it to modify what gets changed during the write operations.
    /// </summary>
    /// <param name="configureState">The delegate to be called before saving context's state.</param>
    public static void ConfigureEntryState(ConfigureEntryStateDelegate<TEntity> configureState)
    {
        ConfigureStateDelegateLocal.Value = configureState;
        IsConfiguredLocal.Value = true;
    }

    /// <summary>
    /// Clears the configured entry state override for the current async context.
    /// </summary>
    public static void Clear()
    {
        ConfigureStateDelegateLocal.Value = null;
        IsConfiguredLocal.Value = false;
    }
}