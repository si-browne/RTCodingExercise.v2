namespace IntegrationEvents;

public class PlateSoldIntegrationEvent : IntegrationEvent
{
    public Guid PlateId { get; set; }
    public string Registration { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal SoldPrice { get; set; }
    public string? PromoCode { get; set; }
    public decimal ProfitMargin { get; set; }
    public DateTime SoldDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
