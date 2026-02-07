using System.Linq.Expressions;
using System.Reflection;
using Orleans.Providers.EntityFramework.Exceptions;

namespace Orleans.Providers.EntityFramework.Internal;

internal static class ReflectionHelper
{
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

    public static Func<T, TProperty> GetAccessorDelegate<T, TProperty>(PropertyInfo pInfo)
        => (Func<T, TProperty>)Delegate.CreateDelegate(
            typeof(Func<T, TProperty>),
            null,
            pInfo.GetMethod!);

    public static Expression<Func<T, TProperty>> GetAccessorExpression<T, TProperty>(PropertyInfo pInfo)
    {
        var paramExp = Expression.Parameter(typeof(T), "target");
        var propertyExp = Expression.Property(paramExp, pInfo);

        return Expression.Lambda<Func<T, TProperty>>(propertyExp, paramExp);
    }
}