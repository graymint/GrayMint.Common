using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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
            var dbItemKeyValue = dbItemKeyProp.GetValue(dbItem, null) ??
                                 throw new InvalidOperationException("LookupId can not be null");
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
            var newDbItem =
                (T)Activator.CreateInstance(typeof(T), Enum.Parse(typeof(TEnum), item.Value!, true), item.Value)!;
            dbSet.Add(newDbItem);
        }
    }

    private static bool IsPostgres(DatabaseFacade database) =>
        database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true ||
        database.ProviderName?.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Sets a default value on a property. The constraint name is used by SQL Server for named default constraints
    /// and is ignored by PostgreSQL and other providers that do not support named default constraints.
    /// </summary>
    public static PropertyBuilder<TProperty> HasDefaultValueWithConstraintName<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        object? defaultValue,
        string constraintName)
    {
        propertyBuilder.HasDefaultValue(defaultValue);
        // "Relational:DefaultConstraintName" is the annotation key read by the SQL Server provider at runtime.
        // Other providers ignore unknown annotations, so no SQL Server NuGet reference is required.
        propertyBuilder.HasAnnotation("Relational:DefaultConstraintName", constraintName);
        return propertyBuilder;
    }

    public static async Task EnsureTablesCreated(DatabaseFacade database)
    {
        try
        {
            var databaseCreator = (RelationalDatabaseCreator)database.GetService<IDatabaseCreator>();
            await databaseCreator.CreateTablesAsync();
        }
        catch (DbException ex) when (
            ex.ErrorCode == 2714 ||                                                           // SQL Server: object already exists
            ex.SqlState == "42P07" ||                                                         // PostgreSQL: duplicate_table
            ex.SqlState == "42710" ||                                                         // PostgreSQL: duplicate_object
            ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("already an object", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
        {
            // ignore
        }
    }

    public static async Task<bool> SqlFunctionExists(DatabaseFacade database, string schema, string functionName)
    {
        string sql;
        if (IsPostgres(database))
        {
            sql = $"SELECT COUNT(1) FROM information_schema.routines " +
                  $"WHERE routine_schema = '{schema}' AND routine_name = '{functionName}' " +
                  $"AND routine_type = 'FUNCTION'";
        }
        else
        {
            // ReSharper disable StringLiteralTypo
            sql = $"SELECT COUNT(1) FROM sys.objects WHERE object_id=OBJECT_ID(N'[{schema}].[{functionName}]') " +
                  "AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' )";
            // ReSharper restore StringLiteralTypo
        }

        var res = await ExecuteScalar(database, sql);
        return res > 0;
    }

    public static async Task<bool> SqlTableExists(DatabaseFacade database, string schema, string tableName)
    {
        string sql;
        if (IsPostgres(database))
        {
            sql = $"SELECT COUNT(1) FROM information_schema.tables " +
                  $"WHERE table_schema = '{schema}' AND table_name = '{tableName}' " +
                  $"AND table_type = 'BASE TABLE'";
        }
        else
        {
            // ReSharper disable StringLiteralTypo
            sql = $"SELECT COUNT(1) FROM sys.objects WHERE object_id=OBJECT_ID(N'[{schema}].[{tableName}]') " +
                  "AND type IN ( N'U' )";
            // ReSharper restore StringLiteralTypo
        }

        var res = await ExecuteScalar(database, sql);
        return res > 0;
    }

    private static async Task<int> ExecuteScalar(DatabaseFacade database, string sql)
    {
        await using var cmd = database.GetDbConnection().CreateCommand();
        if (database.CurrentTransaction != null) cmd.Transaction = database.CurrentTransaction.GetDbTransaction();

        ArgumentNullException.ThrowIfNull(cmd.Connection);
        var connection = cmd.Connection;
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var scalar = (await command.ExecuteScalarAsync())!;
        var res = Convert.ToInt32(scalar);
        return res;
    }
}