namespace Catalog.API.Models;

public class RevenueStatistics
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalProfit { get; set; }
    public int PlatesSold { get; set; }
    public decimal AverageProfitMargin { get; set; }
}
