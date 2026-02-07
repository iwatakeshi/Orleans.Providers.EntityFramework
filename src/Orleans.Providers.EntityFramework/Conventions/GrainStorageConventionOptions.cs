namespace Orleans.Providers.EntityFramework.Conventions;

/// <summary>
/// Options that control convention-based configuration for grain storage.
/// </summary>
public class GrainStorageConventionOptions
{
    /// <summary>
    /// Gets or sets the default property name used for the grain key.
    /// </summary>
    public string DefaultGrainKeyPropertyName { get; set; } = "Id";

    /// <summary>
    /// Gets or sets the default property name used for the grain key extension.
    /// </summary>
    public string DefaultGrainKeyExtPropertyName { get; set; } = "KeyExt";

    /// <summary>
    /// Gets or sets the default property name used to determine persistence.
    /// </summary>
    public string DefaultPersistenceCheckPropertyName { get; set; } = "Id";
}