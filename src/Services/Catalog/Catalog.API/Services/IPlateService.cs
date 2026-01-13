using Catalog.API.Models;
using Catalog.Domain;

namespace Catalog.API.Services;

public interface IPlateService
{
    Task<PagedResult<Plate>> GetPlatesAsync(
        string? search,
        string? letters,
        int? numbers,
        PlateStatus? status,
        string? sortBy,
        int page = 1,
        int pageSize = 20);
    Task<Plate?> GetPlateByIdAsync(Guid id);
    Task<Plate> CreatePlateAsync(Plate plate);
    Task<Plate> UpdatePlateAsync(Guid id, Plate plate);
    Task DeletePlateAsync(Guid id);
    Task<Plate> ReservePlateAsync(Guid id);
    Task<Plate> UnreservePlateAsync(Guid id);
    Task<Plate> SellPlateAsync(Guid id, string? promoCode);
    Task<decimal> CalculatePriceAsync(Guid id, string? promoCode);
    Task<RevenueStatistics> GetRevenueStatisticsAsync();
}
