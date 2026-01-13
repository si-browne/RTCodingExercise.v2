namespace IntegrationEvents;

public class PlateReservedIntegrationEvent : IntegrationEvent
{
    public Guid PlateId { get; set; }
    public string Registration { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public DateTime ReservedDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
