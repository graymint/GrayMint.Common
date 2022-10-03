using Microsoft.EntityFrameworkCore;

// ReSharper disable UnusedMember.Global
namespace GrayMint.Common.EntityFrameworkCore;
public static class EfCoreUtil
{
    public static Task UpdateEnums<T, TEnum>(DbSet<T> dbSet)
        where T : class
        where TEnum : Enum
    {
        return UpdateEnums<T, TEnum, byte>(dbSet);
    }

    public static async Task UpdateEnums<T, TEnum, TEnumType>(DbSet<T> dbSet)
        where T : class
        where TEnum : Enum
        where TEnumType : notnull
    {
        var enumItems =
            Enum.GetValues(typeof(TEnum)).Cast<TEnumType>()
                .ToDictionary(item => item, item => Enum.GetName(typeof(TEnum), item));

        var oldEnumItemKeys = new List<TEnumType>();
        foreach (var dbItem in await dbSet.ToArrayAsync())
        {
            // find 
            var dbItemKeyProp = dbItem.GetType().GetProperties().Single(x => x.Name.EndsWith("Id"));
            var dbItemKeyValue = dbItemKeyProp.GetValue(dbItem, null) ?? throw new InvalidOperationException("LookupId can not be null");
            var dbItemNameProp = dbItem.GetType().GetProperties().Single(x => x.Name.EndsWith("Name"));

            if (enumItems.TryGetValue((TEnumType)dbItemKeyValue, out var itemValue))
            {
                dbItemNameProp.SetValue(dbItem, itemValue);
                oldEnumItemKeys.Add((TEnumType)dbItemKeyValue);
            }
            else
            {
                dbSet.Remove(dbItem);
            }
        }

        // add new Items
        var newEnumItems = enumItems.ExceptBy(oldEnumItemKeys, x => x.Key);
        foreach (var item in newEnumItems)
        {
            var newDbItem = (T)Activator.CreateInstance(typeof(T), Enum.Parse(typeof(TEnum), item.Value!, true), item.Value)!;
            dbSet.Add(newDbItem);
        }

    }
}