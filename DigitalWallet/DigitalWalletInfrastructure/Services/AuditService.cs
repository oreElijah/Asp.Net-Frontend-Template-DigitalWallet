using DigitalWalletCore.Entities;
using DigitalWalletCore.Interfaces;
using DigitalWalletInfrastructure.Data;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DigitalWalletInfrastructure.Services
{

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task RecordAsync(string action, string entityType, string entityId, string? actorUserId = null, string? details = null, CancellationToken cancellationToken = default)
        {
            _context.AuditLog.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = string.IsNullOrWhiteSpace(details) ? "{}" : details,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Audit event {Action} recorded for {EntityType} {EntityId}", action, entityType, entityId);
        }
    }
}
