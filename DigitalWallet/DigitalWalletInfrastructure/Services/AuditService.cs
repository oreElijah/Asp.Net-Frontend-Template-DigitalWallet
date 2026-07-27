using DigitalWalletCore.Entities;
using DigitalWalletCore.Interfaces;
using DigitalWalletInfrastructure.Data;
using System.Text.Json;

namespace DigitalWalletInfrastructure.Services;

public class AuditService(ApplicationDbContext context, ILogger<AuditService> logger) : IAuditService
{
    public async Task RecordAsync(string action, string entityType, string entityId, string? actorUserId = null, string? details = null, CancellationToken cancellationToken = default)
    {
        context.AuditLog.Add(new AuditLog
        {
            Id = Guid.NewGuid(), ActorUserId = actorUserId, Action = action,
            EntityType = entityType, EntityId = entityId,
            Details = string.IsNullOrWhiteSpace(details) ? "{}" : details,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Audit event {Action} recorded for {EntityType} {EntityId}", action, entityType, entityId);
    }
}
