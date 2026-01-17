namespace Catalog.API.Models
{
    public class AuditLogEventDto
    {
        public Guid AuditLogEventId { get; set; }
        public Guid PlateId { get; set; }
        public Guid UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public AuditAction Status { get; set; }
        public List<AuditLogEventChangeDto> Changes { get; set; } = new();
    }

    public class AuditLogEventChangeDto
    {
        public Guid AuditLogEventChangeId { get; set; }
        public string FieldName { get; set; } = "";
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}
