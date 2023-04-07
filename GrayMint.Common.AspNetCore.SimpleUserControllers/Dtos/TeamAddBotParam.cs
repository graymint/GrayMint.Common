namespace GrayMint.Common.AspNetCore.SimpleUserControllers.Dtos;

public class TeamAddBotParam
{
    public required string Name { get; init; }
    public required Guid RoleId { get; init; }
}