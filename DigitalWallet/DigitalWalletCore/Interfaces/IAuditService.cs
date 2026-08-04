namespace DigitalWalletCore.Interfaces;

public interface IAuditService
{
    Task RecordAsync(string action, string entityType, string entityId, string? actorUserId = null, object? details = null, CancellationToken cancellationToken = default);
}
