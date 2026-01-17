using WebMVC.enums;

namespace RTCodingExercise.Microservices.Models
{
    public class AuditLogEvent
    {
        public Guid AuditLogEventId { get; set; }
        public Guid PlateId { get; set; }
        public Guid UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public AuditAction Status { get; set; } // didnt want to hook up domain
        public List<AuditLogEventChange> Changes { get; set; } = new();
    }

    public class AuditLogEventChange
    {
        public Guid AuditLogEventChangeId { get; set; }
        public string FieldName { get; set; } = "";
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}
