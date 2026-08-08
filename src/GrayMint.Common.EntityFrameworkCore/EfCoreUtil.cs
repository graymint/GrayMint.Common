using System.Data;
using System.Data.Common;
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

    private static bool IsSqlite(DatabaseFacade database) =>
        database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsPostgres(DatabaseFacade database) =>
        database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true ||
        database.ProviderName?.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase) == true;

    public static async Task EnsureTablesCreated(DatabaseFacade database)
    {
        // Creating unconditionally and swallowing the "already exists" error makes EF log a failed
        // DbCommand on every start once the tables are there, which reads like a real fault in the
        // log. Probe first and only emit DDL when a table is actually missing. The catch below is
        // kept as a backstop for the probe being unavailable and for start-up races.
        if (await AllTablesExist(database))
            return;

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

    // True when every table of this context's model is already present. Deliberately not
    // RelationalDatabaseCreator.HasTables(), which answers "does the database hold any table at
    // all" — several contexts share one database here, so the first one to create its tables would
    // make all the others skip. The probe itself is issued as a plain DbCommand rather than through
    // EF, so it never reaches EF's command log; any failure returns false and falls back to
    // create-and-catch.
    private static async Task<bool> AllTablesExist(DatabaseFacade database)
    {
        try
        {
            var tables = database.GetService<ICurrentDbContext>().Context.Model
                .GetRelationalModel().Tables.ToArray();

            if (tables.Length == 0)
                return true; // nothing to create

            // Open through the facade, not the raw connection, so EF keeps its own open-count
            // straight and we hand the connection back exactly as we found it.
            var connection = database.GetDbConnection();
            var wasClosed = connection.State != ConnectionState.Open;
            if (wasClosed)
                await database.OpenConnectionAsync();

            try
            {
                var isSqlite = IsSqlite(database);

                foreach (var table in tables)
                {
                    await using var command = connection.CreateCommand();

                    if (isSqlite)
                    {
                        // SQLite has no INFORMATION_SCHEMA and no schemas at all
                        command.CommandText =
                            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @name";
                        AddParameter(command, "@name", table.Name);
                    }
                    else
                    {
                        command.CommandText =
                            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES " +
                            "WHERE TABLE_NAME = @name AND (@schema IS NULL OR TABLE_SCHEMA = @schema)";
                        AddParameter(command, "@name", table.Name);
                        AddParameter(command, "@schema", table.Schema);
                    }

                    var count = Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0);
                    if (count == 0)
                        return false;
                }

                return true;
            }
            finally
            {
                if (wasClosed)
                    await database.CloseConnectionAsync();
            }
        }
        catch
        {
            // provider we cannot probe, or the probe failed for any other reason
            return false;
        }
    }

    private static void AddParameter(DbCommand command, string name, string? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        // DbType must be explicit: PostgreSQL cannot infer the type of an untyped null and fails
        // the whole statement with 42P08 (could not determine data type of parameter).
        parameter.DbType = DbType.String;
        parameter.Value = (object?)value ?? DBNull.Value;
        command.Parameters.Add(parameter);
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