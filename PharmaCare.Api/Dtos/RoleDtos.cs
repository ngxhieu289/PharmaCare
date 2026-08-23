namespace PharmaCare.Api.Dtos;

public record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyCollection<string> Permissions);
