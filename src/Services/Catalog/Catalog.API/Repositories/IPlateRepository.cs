using Catalog.API.Models;
using Catalog.Domain;

namespace Catalog.API.Repositories;

public interface IPlateRepository
{
    Task<IEnumerable<Plate>> GetAllAsync();
    Task<PagedResult<Plate>> GetPagedAsync(
        string? search,
        string? letters,
        int? numbers,
        PlateStatus? status,
        string? sortBy,
        int page,
        int pageSize);
    Task<Plate?> GetByIdAsync(Guid id);
    Task<RevenueStatistics> GetRevenueStatisticsAsync();
    Task<Plate> AddAsync(Plate plate);
    Task UpdateAsync(Plate plate);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task SaveChangesAsync();
}
