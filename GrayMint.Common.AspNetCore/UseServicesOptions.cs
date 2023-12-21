namespace GrayMint.Common.AspNetCore;

public class UseServicesOptions
{
    public bool UseCors { get; set; } = true;
    public bool MapControllers { get; set; } = true;
    public bool UseAppExceptions { get; set; } = true;
    public bool UseHttpsRedirection { get; set; } = true;
}