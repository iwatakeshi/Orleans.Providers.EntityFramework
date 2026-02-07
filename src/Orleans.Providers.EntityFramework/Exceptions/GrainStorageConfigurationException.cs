namespace Orleans.Providers.EntityFramework.Exceptions;

/// <summary>
/// Exception thrown when grain storage configuration is invalid.
/// </summary>
public class GrainStorageConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GrainStorageConfigurationException"/> class.
    /// </summary>
    public GrainStorageConfigurationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GrainStorageConfigurationException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public GrainStorageConfigurationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GrainStorageConfigurationException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public GrainStorageConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}