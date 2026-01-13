using Catalog.API.Models;
using Catalog.API.Repositories;
using Catalog.Domain;
using IntegrationEvents;
using MassTransit;

namespace Catalog.API.Services;

public class PlateService : IPlateService
{
    private readonly IPlateRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<PlateService> _logger;

    public PlateService(
        IPlateRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<PlateService> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<PagedResult<Plate>> GetPlatesAsync(
        string? search,
        string? letters,
        int? numbers,
        PlateStatus? status,
        string? sortBy,
        int page = 1,
        int pageSize = 20)
    {
        // Default to ForSale if no status specified (User Story 5)
        status ??= PlateStatus.ForSale;

        var result = await _repository.GetPagedAsync(search, letters, numbers, status, sortBy, page, pageSize);
        
        // Ensure SalePrice is calculated with 20% markup (User Story 1)
        foreach (var plate in result.Items)
        {
            if (plate.SalePrice == 0 || plate.SalePrice != plate.CalculateSalePrice())
            {
                plate.SalePrice = plate.CalculateSalePrice();
            }
        }

        return result;
    }

    public async Task<Plate?> GetPlateByIdAsync(Guid id)
    {
        var plate = await _repository.GetByIdAsync(id);
        if (plate != null && (plate.SalePrice == 0 || plate.SalePrice != plate.CalculateSalePrice()))
        {
            plate.SalePrice = plate.CalculateSalePrice();
        }
        return plate;
    }

    public async Task<Plate> CreatePlateAsync(Plate plate)
    {
        plate.SalePrice = plate.CalculateSalePrice();
        await _repository.AddAsync(plate);
        await _repository.SaveChangesAsync();
        return plate;
    }

    public async Task<Plate> UpdatePlateAsync(Guid id, Plate plate)
    {
        if (id != plate.Id)
            throw new ArgumentException("ID mismatch");

        if (!await _repository.ExistsAsync(id))
            throw new KeyNotFoundException($"Plate with ID {id} not found");

        plate.SalePrice = plate.CalculateSalePrice();
        await _repository.UpdateAsync(plate);
        await _repository.SaveChangesAsync();
        return plate;
    }

    public async Task DeletePlateAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
        await _repository.SaveChangesAsync();
    }

    public async Task<Plate> ReservePlateAsync(Guid id)
    {
        var plate = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Plate with ID {id} not found");

        // Call domain method (will throw if invalid state)
        plate.Reserve();

        await _repository.UpdateAsync(plate);
        await _repository.SaveChangesAsync();

        // Publish integration event
        await _publishEndpoint.Publish(new PlateReservedIntegrationEvent
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            PlateId = plate.Id,
            Registration = plate.Registration ?? string.Empty,
            SalePrice = plate.CalculateSalePrice(),
            ReservedDate = plate.ReservedDate!.Value
        });

        _logger.LogInformation("Plate {PlateId} ({Registration}) reserved", plate.Id, plate.Registration);

        return plate;
    }

    public async Task<Plate> UnreservePlateAsync(Guid id)
    {
        var plate = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Plate with ID {id} not found");

        // Call domain method (will throw if invalid state)
        plate.Unreserve();

        await _repository.UpdateAsync(plate);
        await _repository.SaveChangesAsync();

        // Publish integration event
        await _publishEndpoint.Publish(new PlateUnreservedIntegrationEvent
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            PlateId = plate.Id,
            Registration = plate.Registration ?? string.Empty
        });

        _logger.LogInformation("Plate {PlateId} ({Registration}) unreserved", plate.Id, plate.Registration);

        return plate;
    }

    public async Task<Plate> SellPlateAsync(Guid id, string? promoCode)
    {
        var plate = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Plate with ID {id} not found");

        // Calculate final price with promo code (User Story 7)
        var salePrice = plate.CalculateSalePrice();
        var finalPrice = ApplyPromoCode(salePrice, promoCode);

        // Call domain method (will validate 90% minimum - User Story 8)
        plate.Sell(finalPrice, promoCode);

        await _repository.UpdateAsync(plate);
        await _repository.SaveChangesAsync();

        // Publish integration event (User Story 6)
        await _publishEndpoint.Publish(new PlateSoldIntegrationEvent
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            PlateId = plate.Id,
            Registration = plate.Registration ?? string.Empty,
            PurchasePrice = plate.PurchasePrice,
            SalePrice = salePrice,
            SoldPrice = finalPrice,
            PromoCode = promoCode,
            ProfitMargin = plate.CalculateProfitMargin(),
            SoldDate = plate.SoldDate!.Value
        });

        _logger.LogInformation(
            "Plate {PlateId} ({Registration}) sold for £{FinalPrice} (Original: £{SalePrice}, Promo: {PromoCode})",
            plate.Id, plate.Registration, finalPrice, salePrice, promoCode ?? "None");

        return plate;
    }

    public async Task<decimal> CalculatePriceAsync(Guid id, string? promoCode)
    {
        var plate = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Plate with ID {id} not found");

        var salePrice = plate.CalculateSalePrice();
        return ApplyPromoCode(salePrice, promoCode);
    }

    public async Task<RevenueStatistics> GetRevenueStatisticsAsync()
    {
        return await _repository.GetRevenueStatisticsAsync();
    }

    private decimal ApplyPromoCode(decimal salePrice, string? promoCode)
    {
        if (string.IsNullOrWhiteSpace(promoCode))
            return salePrice;

        return promoCode.ToUpperInvariant() switch
        {
            "DISCOUNT" => salePrice - 25m,  // £25 fixed discount
            "PERCENTOFF" => salePrice * 0.85m,  // 15% off
            _ => throw new InvalidOperationException($"Invalid promo code: {promoCode}")
        };
    }
}
