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

        public Task RecordAsync(string action, string entityType, string entityId, string? actorUserId = null, object? details = null, CancellationToken cancellationToken = default)
        {
            var detailsJson = details is null ? "{}" : JsonSerializer.Serialize(details);

            _context.AuditLog.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = detailsJson,
                CreatedAt = DateTime.UtcNow
            });

            // Deliberately no SaveChangesAsync here. The caller commits this
            // together with the business change it's describing, in one
            // SaveChangesAsync — so the audit row and the change it records
            // can never exist independently of each other.
            _logger.LogInformation("Audit event {Action} queued for {EntityType} {EntityId}", action, entityType, entityId);

            return Task.CompletedTask;
        }
    }
}