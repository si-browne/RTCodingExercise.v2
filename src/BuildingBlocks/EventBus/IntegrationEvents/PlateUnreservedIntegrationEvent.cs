namespace IntegrationEvents;

public class PlateUnreservedIntegrationEvent : IntegrationEvent
{
    public Guid PlateId { get; set; }
    public string Registration { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
