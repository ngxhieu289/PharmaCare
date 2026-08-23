namespace PharmaCare.Api.Dtos;

public record AuditLogResponse(
    long Id, Guid UserId, string UserName, string Action,
    string EntityName, string EntityId, string? OldValues,
    string? NewValues, string? IpAddress, DateTimeOffset CreatedAt);
