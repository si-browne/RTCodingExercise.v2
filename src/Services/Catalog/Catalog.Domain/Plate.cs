namespace Catalog.Domain;

public enum PlateStatus
{
    ForSale = 0,
    Reserved = 1,
    Sold = 2
}

public class Plate
{
    public Guid Id { get; set; }
    public string? Registration { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public string? Letters { get; set; }
    public int Numbers { get; set; }
    
    // Business state properties
    public PlateStatus Status { get; private set; } = PlateStatus.ForSale;
    public DateTime? ReservedDate { get; private set; }
    public DateTime? SoldDate { get; private set; }
    public decimal? SoldPrice { get; private set; }
    public string? PromoCodeUsed { get; private set; }

    // Business methods
    public void Reserve()
    {
        if (Status != PlateStatus.ForSale)
            throw new InvalidOperationException($"Cannot reserve plate in {Status} status. Only ForSale plates can be reserved.");
        
        Status = PlateStatus.Reserved;
        ReservedDate = DateTime.UtcNow;
    }

    public void Unreserve()
    {
        if (Status != PlateStatus.Reserved)
            throw new InvalidOperationException($"Cannot unreserve plate in {Status} status. Only Reserved plates can be unreserved.");
        
        Status = PlateStatus.ForSale;
        ReservedDate = null;
    }

    public void Sell(decimal finalPrice, string? promoCode)
    {
        if (Status != PlateStatus.Reserved)
            throw new InvalidOperationException($"Cannot sell plate in {Status} status. Only Reserved plates can be sold.");

        var minimumPrice = CalculateSalePrice() * 0.90m; // 90% minimum
        if (finalPrice < minimumPrice)
            throw new InvalidOperationException($"Sale price £{finalPrice:N2} is below the minimum allowed price of £{minimumPrice:N2} (90% of sale price).");

        Status = PlateStatus.Sold;
        SoldPrice = finalPrice;
        SoldDate = DateTime.UtcNow;
        PromoCodeUsed = promoCode;
    }

    public decimal CalculateSalePrice()
    {
        // 20% markup on purchase price
        return PurchasePrice * 1.20m;
    }

    public decimal CalculateProfitMargin()
    {
        if (!SoldPrice.HasValue || SoldPrice.Value == 0)
            return 0;

        // Profit margin = (SoldPrice - PurchasePrice) / SoldPrice
        return (SoldPrice.Value - PurchasePrice) / SoldPrice.Value;
    }

    public decimal CalculateProfit()
    {
        if (!SoldPrice.HasValue)
            return 0;

        return SoldPrice.Value - PurchasePrice;
    }
}