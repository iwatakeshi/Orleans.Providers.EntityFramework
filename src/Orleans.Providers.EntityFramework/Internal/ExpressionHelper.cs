using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Orleans.Providers.EntityFramework.Internal;

public static class ExpressionHelper
{
    private static MethodInfo GetSingleOrDefaultAsyncMethod()
        => typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .Single(mi =>
                mi.Name == nameof(EntityFrameworkQueryableExtensions.SingleOrDefaultAsync) &&
                mi.GetParameters().Length == 3 &&
                mi.GetParameters()[1].ParameterType.IsGenericType &&
                mi.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>));

    private static MethodInfo GetSingleOrDefaultMethod()
        => typeof(Queryable)
            .GetMethods()
            .Single(mi =>
                mi.Name == nameof(Queryable.SingleOrDefault) &&
                mi.GetParameters().Length == 2 &&
                mi.GetParameters()[1].ParameterType.IsGenericType &&
                mi.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>));

    public static Func<TContext, TKey, Task<TEntity>> CreateQuery<TContext, TState, TEntity, TKey>(
        GrainStorageOptions<TContext, TState, TEntity> options)
        where TContext : DbContext
        where TEntity : class
    {
        var contextParameter = Expression.Parameter(typeof(TContext), "context");
        var stateParameter = Expression.Parameter(typeof(TEntity), "state");
        var keyParameter = Expression.Parameter(typeof(TKey), "grainKey");

        var keyProperty = Expression.Property(stateParameter, options.KeyPropertyName!);
        var keyEqualsExp = Expression.Equal(keyProperty, keyParameter);
        var predicate = Expression.Lambda(keyEqualsExp, stateParameter);

        var queryable = Expression.Call(
            options.DbSetAccessor!.Method,
            Expression.Constant(options.DbSetAccessor),
            contextParameter);

        var compiledLambdaBody = Expression.Call(
            GetSingleOrDefaultAsyncMethod().MakeGenericMethod(typeof(TEntity)),
            queryable,
            Expression.Quote(predicate),
            Expression.Constant(default(CancellationToken), typeof(CancellationToken)));

        var lambdaExpression = Expression.Lambda<Func<TContext, TKey, Task<TEntity>>>(
            compiledLambdaBody, contextParameter, keyParameter);

        return lambdaExpression.Compile();
    }

    public static Func<TContext, TKey, string, Task<TEntity>> CreateCompoundQuery<TContext, TState, TEntity, TKey>(
        GrainStorageOptions<TContext, TState, TEntity> options)
        where TContext : DbContext
        where TEntity : class
    {
        var contextParameter = Expression.Parameter(typeof(TContext), "context");
        var stateParameter = Expression.Parameter(typeof(TEntity), "state");
        var keyParameter = Expression.Parameter(typeof(TKey), "grainKey");
        var keyExtParameter = Expression.Parameter(typeof(string), "grainKeyExt");

        var keyProperty = Expression.Property(stateParameter, options.KeyPropertyName!);
        var keyExtProperty = Expression.Property(stateParameter, options.KeyExtPropertyName!);

        var equalsExp = Expression.And(
            Expression.Equal(keyProperty, keyParameter),
            Expression.Equal(keyExtProperty, keyExtParameter));

        var predicate = Expression.Lambda(equalsExp, stateParameter);

        var queryable = Expression.Call(
            options.DbSetAccessor!.Method,
            Expression.Constant(options.DbSetAccessor),
            contextParameter);

        var compiledLambdaBody = Expression.Call(
            GetSingleOrDefaultAsyncMethod().MakeGenericMethod(typeof(TEntity)),
            queryable,
            Expression.Quote(predicate),
            Expression.Constant(default(CancellationToken), typeof(CancellationToken)));

        var lambdaExpression = Expression.Lambda<Func<TContext, TKey, string, Task<TEntity>>>(
            compiledLambdaBody, contextParameter, keyParameter, keyExtParameter);

        return lambdaExpression.Compile();
    }

    public static Func<TContext, TKey, Task<TEntity>> CreateCompiledQuery<TContext, TState, TEntity, TKey>(
        GrainStorageOptions<TContext, TState, TEntity> options)
        where TContext : DbContext
        where TEntity : class
    {
        var contextParameter = Expression.Parameter(typeof(TContext), "context");
        var keyParameter = Expression.Parameter(typeof(TKey), "grainKey");
        var predicate = CreateKeyPredicate<TEntity, TKey>(options, keyParameter);

        var queryable = Expression.Call(
            options.DbSetAccessor!.Method,
            Expression.Constant(options.DbSetAccessor),
            contextParameter);

        var compiledLambdaBody = Expression.Call(
            GetSingleOrDefaultMethod().MakeGenericMethod(typeof(TEntity)),
            queryable,
            Expression.Quote(predicate));

        var lambdaExpression = Expression.Lambda<Func<TContext, TKey, TEntity>>(
            compiledLambdaBody, contextParameter, keyParameter);

        return EF.CompileAsyncQuery(lambdaExpression);
    }

    public static Expression<Func<TEntity, bool>> CreateKeyPredicate<TEntity, TKey>(
        GrainStorageOptions options,
        ParameterExpression grainKeyParameter)
    {
        var stateParam = Expression.Parameter(typeof(TEntity), "state");
        var stateKeyParam = Expression.Property(stateParam, options.KeyPropertyName!);
        var equals = Expression.Equal(grainKeyParameter, stateKeyParam);

        return Expression.Lambda<Func<TEntity, bool>>(equals, stateParam);
    }

    public static Func<TContext, TKey, string, Task<TEntity>> CreateCompiledCompoundQuery<TContext, TState, TEntity, TKey>(
        GrainStorageOptions<TContext, TState, TEntity> options)
        where TContext : DbContext
        where TEntity : class
    {
        var contextParameter = Expression.Parameter(typeof(TContext), "context");
        var keyParameter = Expression.Parameter(typeof(TKey), "grainKey");
        var keyExtParameter = Expression.Parameter(typeof(string), "grainKeyExt");
        var predicate = CreateCompoundKeyPredicate<TEntity, TKey>(
            options, keyParameter, keyExtParameter);

        var queryable = Expression.Call(
            options.DbSetAccessor!.Method,
            Expression.Constant(options.DbSetAccessor),
            contextParameter);

        var compiledLambdaBody = Expression.Call(
            GetSingleOrDefaultMethod().MakeGenericMethod(typeof(TEntity)),
            queryable,
            Expression.Quote(predicate));

        var lambdaExpression = Expression.Lambda<Func<TContext, TKey, string, TEntity>>(
            compiledLambdaBody, contextParameter, keyParameter, keyExtParameter);

        return EF.CompileAsyncQuery(lambdaExpression);
    }

    public static Expression<Func<TEntity, bool>> CreateCompoundKeyPredicate<TEntity, TKey>(
        GrainStorageOptions options,
        ParameterExpression grainKeyParam,
        ParameterExpression grainKeyExtParam)
    {
        var stateParam = Expression.Parameter(typeof(TEntity), "state");
        var stateKeyParam = Expression.Property(stateParam, options.KeyPropertyName!);
        var stateKeyExtParam = Expression.Property(stateParam, options.KeyExtPropertyName!);

        var equals = Expression.And(
            Expression.Equal(grainKeyParam, stateKeyParam),
            Expression.Equal(grainKeyExtParam, stateKeyExtParam));

        return Expression.Lambda<Func<TEntity, bool>>(equals, stateParam);
    }
}