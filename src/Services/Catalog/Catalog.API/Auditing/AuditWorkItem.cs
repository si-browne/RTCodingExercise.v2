namespace Catalog.API.Auditing
{
    // x2 records to enable write pipeline
    public record AuditFieldChange(string FieldName, string? OldValue, string? NewValue);
    public record AuditWorkItem(Guid PlateId, Guid UserId, DateTime TimestampUtc, AuditAction Status, IReadOnlyList<AuditFieldChange> Changes);
}
