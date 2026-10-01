using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ApiKit.Crud.Internal;

/// <summary>
/// Constructs EF Core primary-key expressions for generic CRUD operations.
/// </summary>
internal static class EfPrimaryKeyExpression
{
    public static Expression<Func<TEntity, bool>> CreatePredicate<TEntity, TKey>(
        IProperty property,
        TKey value)
    {
        ArgumentNullException.ThrowIfNull(property);

        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var propertyAccess = CreatePropertyAccess(parameter, property);
        var valueExpression = Expression.Constant(value, typeof(TKey));

        var comparableValue = typeof(TKey) == property.ClrType
            ? valueExpression
            : Expression.Convert(valueExpression, property.ClrType);

        return Expression.Lambda<Func<TEntity, bool>>(
            Expression.Equal(propertyAccess, comparableValue),
            parameter);
    }

    public static IQueryable<TEntity> OrderBy<TEntity>(
        IQueryable<TEntity> source,
        IProperty property)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(property);

        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var propertyAccess = CreatePropertyAccess(parameter, property);
        var selector = Expression.Lambda(propertyAccess, parameter);

        var call = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.OrderBy),
            [typeof(TEntity), property.ClrType],
            source.Expression,
            Expression.Quote(selector));

        return source.Provider.CreateQuery<TEntity>(call);
    }

    private static Expression CreatePropertyAccess(
        ParameterExpression parameter,
        IProperty property)
    {
        if (property.PropertyInfo is not null)
        {
            return Expression.Property(parameter, property.PropertyInfo);
        }

        if (property.FieldInfo is not null)
        {
            return Expression.Field(parameter, property.FieldInfo);
        }

        return Expression.Call(
            typeof(EF),
            nameof(EF.Property),
            [property.ClrType],
            parameter,
            Expression.Constant(property.Name));
    }
}
