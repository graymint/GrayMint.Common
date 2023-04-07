namespace GrayMint.Common.AspNetCore.SimpleUserControllers.Exceptions;

public class UnregisteredUser : Exception
{
    public UnregisteredUser(string message) : base(message)
    {
    }
}