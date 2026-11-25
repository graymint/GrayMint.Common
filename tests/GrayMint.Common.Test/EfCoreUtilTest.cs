using System.Data.Common;
using GrayMint.Common.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GrayMint.Common.Test;

// EfCoreUtil's probes on the two servers compose.yaml starts, each in a database of the test's own
[TestClass]
public class EfCoreUtilTest : TestBase
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task A_table_and_a_function_are_found_inside_a_transaction(bool postgreSql)
    {
        await using var database = await TestDatabase.Create(postgreSql, TestCt);
        await using var context = database.CreateContext();
        if (postgreSql) {
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE public.items (id int)", TestCt);
            await context.Database.ExecuteSqlRawAsync(
                "CREATE FUNCTION public.answer() RETURNS int AS 'SELECT 42' LANGUAGE sql", TestCt);
        }
        else {
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE dbo.items (id int)", TestCt);
            await context.Database.ExecuteSqlRawAsync(
                "CREATE FUNCTION dbo.answer() RETURNS int AS BEGIN RETURN 42 END", TestCt);
        }

        // SQL Server refuses a command outside the transaction its connection has open
        await using var transaction = await context.Database.BeginTransactionAsync(TestCt);
        Assert.IsTrue(await EfCoreUtil.SqlTableExists(context.Database, database.Schema, "items", TestCt));
        Assert.IsTrue(await EfCoreUtil.SqlFunctionExists(context.Database, database.Schema, "answer", TestCt));
        Assert.IsFalse(await EfCoreUtil.SqlTableExists(context.Database, database.Schema, "answer", TestCt));
        Assert.IsFalse(await EfCoreUtil.SqlFunctionExists(context.Database, database.Schema, "items", TestCt));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task A_name_is_looked_up_as_data_never_run_as_sql(bool postgreSql)
    {
        await using var database = await TestDatabase.Create(postgreSql, TestCt);
        await using var context = database.CreateContext();

        Assert.IsFalse(await EfCoreUtil.SqlTableExists(context.Database, database.Schema, "it's", TestCt));
        Assert.IsFalse(await EfCoreUtil.SqlTableExists(context.Database, database.Schema, "x' OR '1'='1", TestCt));
        Assert.IsFalse(await EfCoreUtil.SqlFunctionExists(context.Database, database.Schema, "x' OR '1'='1", TestCt));
    }

    // a database of the test's own on one of the two servers, dropped with it
    private sealed class TestDatabase : IAsyncDisposable
    {
        // by address: "localhost" is tried over IPv6 first, where the containers don't listen
        private const string PostgreSqlServer = "Host=127.0.0.1;Port=5443;Username=postgres;Password=postgres";
        private const string SqlServerServer =
            "Server=127.0.0.1,14343;User ID=sa;Password=GrayMint-Common-2026;TrustServerCertificate=true";

        private readonly bool _postgreSql;
        private readonly string _name = $"test_{Guid.CreateVersion7():N}";

        public string Schema => _postgreSql ? "public" : "dbo";

        private TestDatabase(bool postgreSql)
        {
            _postgreSql = postgreSql;
        }

        public static async Task<TestDatabase> Create(bool postgreSql, CancellationToken cancellationToken)
        {
            var database = new TestDatabase(postgreSql);
            await database.ExecuteOnServer($"CREATE DATABASE {database._name}", cancellationToken);
            return database;
        }

        public DbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder();
            if (_postgreSql)
                options.UseNpgsql($"{PostgreSqlServer};Database={_name}");
            else
                options.UseSqlServer($"{SqlServerServer};Database={_name}");

            return new DbContext(options.Options);
        }

        public async ValueTask DisposeAsync()
        {
            // the test's pooled connections would keep the database in use; the drop gets a deadline of its own,
            // as it runs after a cancelled test too
            if (_postgreSql)
                NpgsqlConnection.ClearAllPools();
            else
                SqlConnection.ClearAllPools();

            using var dropTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            await ExecuteOnServer(_postgreSql
                    ? $"DROP DATABASE {_name} WITH (FORCE)"
                    : $"ALTER DATABASE [{_name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_name}]",
                dropTimeout.Token);
        }

        private async Task ExecuteOnServer(string sql, CancellationToken cancellationToken)
        {
            await using DbConnection connection = _postgreSql
                ? new NpgsqlConnection(PostgreSqlServer)
                : new SqlConnection(SqlServerServer);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
