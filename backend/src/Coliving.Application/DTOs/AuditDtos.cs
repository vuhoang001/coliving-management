namespace Coliving.Application.DTOs;

public record AuditLogDto
{
    public int Id { get; init; }
    public int? UserId { get; init; }
    public string? UserEmail { get; init; }
    public string Action { get; init; } = default!;
    public string EntityType { get; init; } = default!;
    public int? EntityId { get; init; }
    public string? Detail { get; init; }
    public DateTime CreatedAt { get; init; }
}
