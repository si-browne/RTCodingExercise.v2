
namespace Catalog.Domain
{
    /// <summary>
    /// Entity represents the entity based on a user interaction with number plate data.
    /// </summary>
    public class AuditLogEvent
    {
        public Guid AuditLogEventId { get; set; } // PK
        public Guid PlateId { get; set; } // FK
        public Guid UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public AuditAction Status { get; set; } // this is the status, started with string ended with enum
        public ICollection<AuditLogEventChange> Changes { get; set; } = new List<AuditLogEventChange>();
    }

    /// <summary>
    /// State object to track user interaction.
    /// </summary>
    public class AuditLogEventChange
    {
        public Guid AuditLogEventChangeId { get; set; } // PK
        public Guid AuditLogEventId { get; set; } // FK
        public AuditLogEvent AuditLogEvent { get; set; } = null!;
        public string FieldName { get; set; } = ""; // name of field detected on state change, instantiated
        public string? OldValue { get; set; } // generic name considering efficiency
        public string? NewValue { get; set; }
    }
}
