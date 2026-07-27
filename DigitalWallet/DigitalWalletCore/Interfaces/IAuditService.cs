namespace DigitalWalletCore.Interfaces;

public interface IAuditService
{
    Task RecordAsync(string action, string entityType, string entityId, string? actorUserId = null, string? details = null, CancellationToken cancellationToken = default);
}
