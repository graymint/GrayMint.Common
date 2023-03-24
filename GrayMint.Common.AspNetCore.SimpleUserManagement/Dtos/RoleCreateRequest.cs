namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

public class RoleCreateRequest
{
    public required string RoleName { get; init; }
    public Guid? RoleId { get; init; }
    public string? Description { get; init; }
}