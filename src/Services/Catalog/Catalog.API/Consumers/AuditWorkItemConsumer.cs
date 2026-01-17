using Catalog.API.Auditing;
using MassTransit;

public class AuditWorkItemConsumer : IConsumer<AuditWorkItem>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditWorkItemConsumer> _logger;

    public AuditWorkItemConsumer(IServiceScopeFactory scopeFactory, ILogger<AuditWorkItemConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AuditWorkItem> context)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var msg = context.Message;

            var audit = new AuditLogEvent
            {
                AuditLogEventId = Guid.NewGuid(),
                PlateId = msg.PlateId,
                UserId = msg.UserId,
                Timestamp = msg.TimestampUtc,
                Status = msg.Status,
                Changes = msg.Changes.Select(c => new AuditLogEventChange
                {
                    AuditLogEventChangeId = Guid.NewGuid(),
                    FieldName = c.FieldName,
                    OldValue = c.OldValue,
                    NewValue = c.NewValue
                }).ToList()
            };

            db.AuditLogEvents.Add(audit);
            await db.SaveChangesAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist audit event");
            throw; 
        }
    }
}
