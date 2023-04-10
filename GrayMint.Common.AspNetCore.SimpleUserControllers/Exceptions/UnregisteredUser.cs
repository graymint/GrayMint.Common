namespace GrayMint.Common.AspNetCore.SimpleUserControllers.Exceptions;

public class UnregisteredUser : Exception
{
    public UnregisteredUser() : base("User has not been registered.")
    {
    }
}