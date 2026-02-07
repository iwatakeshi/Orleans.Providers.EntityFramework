using System.Linq.Expressions;
using System.Reflection;
using Orleans.Providers.EntityFramework.Exceptions;

namespace Orleans.Providers.EntityFramework.Internal;

/// <summary>
/// Reflection utilities used by grain storage conventions.
/// </summary>
internal static class ReflectionHelper
{
    /// <summary>
    /// Gets public instance property info by name with validation.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The property info.</returns>
    public static PropertyInfo GetPropertyInfo<T>(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);

        var stateType = typeof(T);

        var idProperty = stateType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new GrainStorageConfigurationException(
                $"Could not find \"{propertyName}\" property on type \"{stateType.FullName}\". " +
                "Either configure the state locator predicate manually or update your model.");

        if (!idProperty.CanRead)
            throw new GrainStorageConfigurationException(
                $"The property \"{propertyName}\" of type \"{stateType.FullName}\" must have a public getter.");

        return idProperty;
    }

    /// <summary>
    /// Creates a strongly typed accessor delegate for the provided property.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="pInfo">The property info.</param>
    /// <returns>The accessor delegate.</returns>
    public static Func<T, TProperty> GetAccessorDelegate<T, TProperty>(PropertyInfo pInfo)
        => (Func<T, TProperty>)Delegate.CreateDelegate(
            typeof(Func<T, TProperty>),
            null,
            pInfo.GetMethod!);

    /// <summary>
    /// Creates a strongly typed accessor expression for the provided property.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="pInfo">The property info.</param>
    /// <returns>The accessor expression.</returns>
    public static Expression<Func<T, TProperty>> GetAccessorExpression<T, TProperty>(PropertyInfo pInfo)
    {
        var paramExp = Expression.Parameter(typeof(T), "target");
        var propertyExp = Expression.Property(paramExp, pInfo);

        return Expression.Lambda<Func<T, TProperty>>(propertyExp, paramExp);
    }
}