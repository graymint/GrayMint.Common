using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

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

    public static async Task EnsureTablesCreated(DbContext dbContext)
    {
        try
        {
            var databaseCreator = (RelationalDatabaseCreator)dbContext.Database.GetService <IDatabaseCreator>();
            await databaseCreator.CreateTablesAsync();

        }
        catch (SqlException ex) when (ex.Number == 2714) // already exists exception
        {
            // ignore
        }
    }

    public static async Task<bool> SqlFunctionExists(DatabaseFacade database, string schema, string functionName)
    {
        // ReSharper disable StringLiteralTypo
        var sql = $"SELECT COUNT(1) FROM sys.objects WHERE object_id=OBJECT_ID(N'[{schema}].[{functionName}]') " +
                  "AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' )";
        // ReSharper restore StringLiteralTypo

        var res = await ExecuteScalar(database, sql);
        return res > 0;
    }

    public static async Task<bool> SqlTableExists(DatabaseFacade database, string schema, string tableName)
    {
        // ReSharper disable StringLiteralTypo
        var sql = $"SELECT COUNT(1) FROM sys.objects WHERE object_id=OBJECT_ID(N'[{schema}].[{tableName}]') " +
                  "AND type IN ( N'U' )";
        // ReSharper restore StringLiteralTypo

        var res = await ExecuteScalar(database, sql);
        return res > 0;
    }

    private static async Task<int> ExecuteScalar(DatabaseFacade database, string sql)
    {
        await using var cmd = (SqlCommand)database.GetDbConnection().CreateCommand();
        if (database.CurrentTransaction != null) cmd.Transaction = (SqlTransaction)database.CurrentTransaction.GetDbTransaction();

        var connection = cmd.Connection;
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var res = (int)(await command.ExecuteScalarAsync())!;
        return res;
    }

}