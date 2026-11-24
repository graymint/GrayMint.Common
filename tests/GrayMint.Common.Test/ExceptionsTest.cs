using System.Data.Common;
using GrayMint.Common.Exceptions;

namespace GrayMint.Common.Test;

[TestClass]
public class ExceptionsTest : TestBase
{
    [TestMethod]
    public void AlreadyExists_Is_should_detect_provider_duplicate_errors()
    {
        // its own type
        Assert.IsTrue(AlreadyExistsException.Is(new AlreadyExistsException("Users")));

        // SQL Server style: HelpLink.EvtID carries the native error number
        var sqlServerEx = new Exception("Cannot insert duplicate key row.")
        {
            Data =
            {
                ["HelpLink.EvtID"] = "2601"
            }
        };

        Assert.IsTrue(AlreadyExistsException.Is(sqlServerEx));

        // SQLite style: message carries the constraint failure (error 19)
        var sqliteEx = new Exception("SQLite Error 19: 'UNIQUE constraint failed: Users.Email'.");
        Assert.IsTrue(AlreadyExistsException.Is(sqliteEx));

        // PostgreSQL style: the SQLSTATE of a unique violation; another SQLSTATE is not one
        Assert.IsTrue(AlreadyExistsException.Is(new SqlStateException("23505")));
        Assert.IsFalse(AlreadyExistsException.Is(new SqlStateException("23503")));

        // wrapped as inner exception (as DbUpdateException does)
        Assert.IsTrue(AlreadyExistsException.Is(new Exception("Saving failed.", sqliteEx)));
        Assert.IsTrue(AlreadyExistsException.Is(new Exception("Saving failed.", new SqlStateException("23505"))));

        // unrelated errors stay false
        Assert.IsFalse(AlreadyExistsException.Is(new Exception("Some other failure.")));
    }

    private sealed class SqlStateException(string sqlState) : DbException("A provider error.")
    {
        public override string SqlState => sqlState;
    }
}
