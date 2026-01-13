using Catalog.API.Data;
using Catalog.API.Models;
using Catalog.API.Services;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Repositories;

public class PlateRepository : IPlateRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PlateRepository> _logger;
    private readonly IPlateMatchingService _matchingService;

    public PlateRepository(
        ApplicationDbContext context, 
        ILogger<PlateRepository> logger,
        IPlateMatchingService matchingService)
    {
        _context = context;
        _logger = logger;
        _matchingService = matchingService;
    }

    public async Task<IEnumerable<Plate>> GetAllAsync()
    {
        return await _context.Plates.ToListAsync();
    }

    public async Task<PagedResult<Plate>> GetPagedAsync(
        string? search,
        string? letters,
        int? numbers,
        PlateStatus? status,
        string? sortBy,
        int page,
        int pageSize)
    {
        var query = _context.Plates.AsNoTracking().AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            // Basic contains search
            query = query.Where(p => p.Registration != null && p.Registration.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(letters))
        {
            query = query.Where(p => p.Letters == letters);
        }

        if (numbers.HasValue)
        {
            query = query.Where(p => p.Numbers == numbers.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        // Apply sorting
        query = sortBy?.ToLower() switch
        {
            "price_asc" => query.OrderBy(p => p.SalePrice),
            "price_desc" => query.OrderByDescending(p => p.SalePrice),
            "registration_asc" => query.OrderBy(p => p.Registration),
            "registration_desc" => query.OrderByDescending(p => p.Registration),
            _ => query.OrderBy(p => p.SalePrice) // Default sort
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Advanced name matching (User Story 3 Advanced)
        // If search looks like a name (contains letters and spaces), apply phonetic matching
        if (!string.IsNullOrWhiteSpace(search) && ContainsNamePattern(search))
        {
            _logger.LogInformation("Applying advanced name matching for search term: {Search}", search);
            
            // Get all matching plates and score them
            var allPlates = await _context.Plates
                .AsNoTracking()
                .Where(p => status.HasValue ? p.Status == status.Value : p.Status == PlateStatus.ForSale)
                .ToListAsync();

            var scoredPlates = allPlates
                .Select(p => new
                {
                    Plate = p,
                    Score = _matchingService.CalculateMatchScore(p.Registration ?? "", search)
                })
                .Where(x => x.Score >= 0.6) // Only include good matches
                .OrderByDescending(x => x.Score)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Plate)
                .ToList();

            if (scoredPlates.Any())
            {
                _logger.LogInformation("Found {Count} plates matching name pattern '{Search}'", scoredPlates.Count, search);
                
                return new PagedResult<Plate>
                {
                    Items = scoredPlates,
                    TotalCount = scoredPlates.Count,
                    Page = page,
                    PageSize = pageSize
                };
            }
        }

        return new PagedResult<Plate>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Determines if a search term looks like a name (for advanced matching).
    /// </summary>
    private bool ContainsNamePattern(string search)
    {
        // Name pattern: Contains mostly letters, may have spaces
        var letterCount = search.Count(char.IsLetter);
        var totalChars = search.Replace(" ", "").Length;
        
        return letterCount >= 3 && (double)letterCount / totalChars >= 0.7;
    }

    public async Task<Plate?> GetByIdAsync(Guid id)
    {
        return await _context.Plates.FindAsync(id);
    }

    public async Task<RevenueStatistics> GetRevenueStatisticsAsync()
    {
        var soldPlates = await _context.Plates
            .AsNoTracking()
            .Where(p => p.Status == PlateStatus.Sold && p.SoldPrice.HasValue)
            .ToListAsync();

        if (!soldPlates.Any())
        {
            return new RevenueStatistics
            {
                TotalRevenue = 0,
                TotalProfit = 0,
                PlatesSold = 0,
                AverageProfitMargin = 0
            };
        }

        var totalRevenue = soldPlates.Sum(p => p.SoldPrice!.Value);
        var totalProfit = soldPlates.Sum(p => p.CalculateProfit());
        var averageProfitMargin = soldPlates.Average(p => p.CalculateProfitMargin());

        return new RevenueStatistics
        {
            TotalRevenue = totalRevenue,
            TotalProfit = totalProfit,
            PlatesSold = soldPlates.Count,
            AverageProfitMargin = averageProfitMargin
        };
    }

    public async Task<Plate> AddAsync(Plate plate)
    {
        await _context.Plates.AddAsync(plate);
        return plate;
    }

    public Task UpdateAsync(Plate plate)
    {
        _context.Entry(plate).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var plate = await GetByIdAsync(id);
        if (plate != null)
        {
            _context.Plates.Remove(plate);
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Plates.AnyAsync(p => p.Id == id);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
