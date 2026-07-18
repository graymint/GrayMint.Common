using GrayMint.Common.Exceptions;

namespace GrayMint.Common.Test;

[TestClass]
public class ExceptionsTest
{
    [TestMethod]
    public void AlreadyExists_Is_should_detect_provider_duplicate_errors()
    {
        // its own type
        Assert.IsTrue(AlreadyExistsException.Is(new AlreadyExistsException("Users")));

        // SQL Server style: HelpLink.EvtID carries the native error number
        var sqlServerEx = new Exception("Cannot insert duplicate key row.");
        sqlServerEx.Data["HelpLink.EvtID"] = "2601";
        Assert.IsTrue(AlreadyExistsException.Is(sqlServerEx));

        // SQLite style: message carries the constraint failure (error 19)
        var sqliteEx = new Exception("SQLite Error 19: 'UNIQUE constraint failed: Users.Email'.");
        Assert.IsTrue(AlreadyExistsException.Is(sqliteEx));

        // wrapped as inner exception (as DbUpdateException does)
        Assert.IsTrue(AlreadyExistsException.Is(new Exception("Saving failed.", sqliteEx)));

        // unrelated errors stay false
        Assert.IsFalse(AlreadyExistsException.Is(new Exception("Some other failure.")));
    }
}
