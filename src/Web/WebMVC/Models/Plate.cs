namespace RTCodingExercise.Microservices.Models;

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
    public string? Letters { get; set; }
    public int? Numbers { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public PlateStatus Status { get; set; }
    public DateTime? ReservedDate { get; set; }
    public DateTime? SoldDate { get; set; }
    public decimal? SoldPrice { get; set; }
    public string? PromoCodeUsed { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public class RevenueStatistics
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalProfit { get; set; }
    public int PlatesSold { get; set; }
    public decimal AverageProfitMargin { get; set; }
}
