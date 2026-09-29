using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ApiKit.Crud.Internal;

internal static class EfEntityMetadata
{
    public static IEntityType GetEntityType<TEntity>(DbContext dbContext)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        return dbContext.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException(
                $"Тип {typeof(TEntity).FullName} не зарегистрирован в EF Core model.");
    }

    public static IKey GetPrimaryKey<TEntity>(DbContext dbContext)
        where TEntity : class
    {
        return GetEntityType<TEntity>(dbContext).FindPrimaryKey()
            ?? throw new InvalidOperationException(
                $"Тип {typeof(TEntity).FullName} не имеет первичного ключа EF Core.");
    }

    public static IProperty GetSinglePrimaryKeyProperty<TEntity>(DbContext dbContext)
        where TEntity : class
    {
        var primaryKey = GetPrimaryKey<TEntity>(dbContext);

        if (primaryKey.Properties.Count != 1)
        {
            throw new InvalidOperationException(
                $"Стандартный CRUD поддерживает один primary key. " +
                $"Для {typeof(TEntity).Name} переопределите соответствующие CRUD actions.");
        }

        return primaryKey.Properties[0];
    }
}
