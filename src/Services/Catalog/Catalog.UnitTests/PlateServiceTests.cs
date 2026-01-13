using Catalog.API.Models;
using Catalog.API.Repositories;
using Catalog.API.Services;
using Catalog.Domain;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using IntegrationEvents;

namespace Catalog.UnitTests;

/// <summary>
/// Unit tests for PlateService, verifying business logic and service layer operations
/// according to the Regtransfers Code Exercise requirements
/// </summary>
public class PlateServiceTests
{
    private readonly Mock<IPlateRepository> _mockRepository;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly Mock<ILogger<PlateService>> _mockLogger;
    private readonly PlateService _service;

    public PlateServiceTests()
    {
        _mockRepository = new Mock<IPlateRepository>();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockLogger = new Mock<ILogger<PlateService>>();
        _service = new PlateService(_mockRepository.Object, _mockPublishEndpoint.Object, _mockLogger.Object);
    }

    #region User Story 1 - List Plates with 20% Markup

    [Fact]
    public async Task GetPlatesAsync_ShouldReturnPlatesWithCorrectSalePrice()
    {
        // Arrange
        var plates = new List<Plate>
        {
            new() { Id = Guid.NewGuid(), Registration = "AB12 CDE", PurchasePrice = 100m, SalePrice = 0 },
            new() { Id = Guid.NewGuid(), Registration = "XY99 ZZZ", PurchasePrice = 200m, SalePrice = 0 }
        };

        var pagedResult = new PagedResult<Plate>
        {
            Items = plates,
            Page = 1,
            PageSize = 20,
            TotalCount = 2
        };

        _mockRepository.Setup(r => r.GetPagedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<PlateStatus?>(),
            It.IsAny<string>(),
            1, 20))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _service.GetPlatesAsync(null, null, null, null, null, 1, 20);

        // Assert
        Assert.Equal(120m, result.Items.ElementAt(0).SalePrice); // £100 + 20% = £120
        Assert.Equal(240m, result.Items.ElementAt(1).SalePrice); // £200 + 20% = £240
    }

    #endregion

    #region User Story 2 - Order by Price

    [Theory]
    [InlineData("price_asc")]
    [InlineData("price_desc")]
    public async Task GetPlatesAsync_WithSortBy_ShouldPassToRepository(string sortBy)
    {
        // Arrange
        var pagedResult = new PagedResult<Plate>
        {
            Items = new List<Plate>(),
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
        };

        _mockRepository.Setup(r => r.GetPagedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<PlateStatus?>(),
            sortBy,
            1, 20))
            .ReturnsAsync(pagedResult);

        // Act
        await _service.GetPlatesAsync(null, null, null, null, sortBy, 1, 20);

        // Assert
        _mockRepository.Verify(r => r.GetPagedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<PlateStatus?>(),
            sortBy,
            1, 20), Times.Once);
    }

    #endregion

    #region User Story 3 - Filter by Letters and Numbers

    [Fact]
    public async Task GetPlatesAsync_WithLettersFilter_ShouldPassToRepository()
    {
        // Arrange
        var pagedResult = new PagedResult<Plate>
        {
            Items = new List<Plate>(),
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
        };

        _mockRepository.Setup(r => r.GetPagedAsync(
            It.IsAny<string>(),
            "ABC",
            It.IsAny<int?>(),
            It.IsAny<PlateStatus?>(),
            It.IsAny<string>(),
            1, 20))
            .ReturnsAsync(pagedResult);

        // Act
        await _service.GetPlatesAsync(null, "ABC", null, null, null, 1, 20);

        // Assert
        _mockRepository.Verify(r => r.GetPagedAsync(
            It.IsAny<string>(),
            "ABC",
            It.IsAny<int?>(),
            It.IsAny<PlateStatus?>(),
            It.IsAny<string>(),
            1, 20), Times.Once);
    }

    [Fact]
    public async Task GetPlatesAsync_WithNumbersFilter_ShouldPassToRepository()
    {
        // Arrange
        var pagedResult = new PagedResult<Plate>
        {
            Items = new List<Plate>(),
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
        };

        _mockRepository.Setup(r => r.GetPagedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            123,
            It.IsAny<PlateStatus?>(),
            It.IsAny<string>(),
            1, 20))
            .ReturnsAsync(pagedResult);

        // Act
        await _service.GetPlatesAsync(null, null, 123, null, null, 1, 20);

        // Assert
        _mockRepository.Verify(r => r.GetPagedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            123,
            It.IsAny<PlateStatus?>(),
            It.IsAny<string>(),
            1, 20), Times.Once);
    }

    #endregion

    #region User Story 4 - Reserve/Unreserve Plates

    [Fact]
    public async Task ReservePlateAsync_ShouldChangeStatusAndPublishEvent()
    {
        // Arrange
        var plateId = Guid.NewGuid();
        var plate = new Plate
        {
            Id = plateId,
            Registration = "AB12 CDE",
            PurchasePrice = 100m,
            SalePrice = 120m
        };

        _mockRepository.Setup(r => r.GetByIdAsync(plateId)).ReturnsAsync(plate);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Plate>())).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.ReservePlateAsync(plateId);

        // Assert
        Assert.Equal(PlateStatus.Reserved, result.Status);
        Assert.NotNull(result.ReservedDate);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Plate>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        _mockPublishEndpoint.Verify(p => p.Publish(
            It.Is<PlateReservedIntegrationEvent>(e => e.PlateId == plateId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnreservePlateAsync_ShouldChangeStatusAndPublishEvent()
    {
        // Arrange
        var plateId = Guid.NewGuid();
        var plate = new Plate
        {
            Id = plateId,
            Registration = "AB12 CDE",
            PurchasePrice = 100m,
            SalePrice = 120m
        };
        plate.Reserve();

        _mockRepository.Setup(r => r.GetByIdAsync(plateId)).ReturnsAsync(plate);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Plate>())).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UnreservePlateAsync(plateId);

        // Assert
        Assert.Equal(PlateStatus.ForSale, result.Status);
        Assert.Null(result.ReservedDate);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Plate>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        _mockPublishEndpoint.Verify(p => p.Publish(
            It.Is<PlateUnreservedIntegrationEvent>(e => e.PlateId == plateId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region User Story 5 - Default to ForSale Status

    [Fact]
    public async Task GetPlatesAsync_WithNoStatusFilter_ShouldDefaultToForSale()
    {
        // Arrange
        var pagedResult = new PagedResult<Plate>
        {
            Items = new List<Plate>(),
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
        };

        _mockRepository.Setup(r => r.GetPagedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            PlateStatus.ForSale,
            It.IsAny<string>(),
            1, 20))
            .ReturnsAsync(pagedResult);

        // Act
        await _service.GetPlatesAsync(null, null, null, null, null, 1, 20);

        // Assert - Verify ForSale is passed when no status specified
        _mockRepository.Verify(r => r.GetPagedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            PlateStatus.ForSale,
            It.IsAny<string>(),
            1, 20), Times.Once);
    }

    #endregion

    #region User Story 6 - Sell Plates

    [Fact]
    public async Task SellPlateAsync_WithoutPromoCode_ShouldSellAtFullPrice()
    {
        // Arrange
        var plateId = Guid.NewGuid();
        var plate = new Plate
        {
            Id = plateId,
            Registration = "AB12 CDE",
            PurchasePrice = 100m,
            SalePrice = 120m
        };
        plate.Reserve();

        _mockRepository.Setup(r => r.GetByIdAsync(plateId)).ReturnsAsync(plate);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Plate>())).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.SellPlateAsync(plateId, null);

        // Assert
        Assert.Equal(PlateStatus.Sold, result.Status);
        Assert.Equal(120m, result.SoldPrice);
        Assert.Null(result.PromoCodeUsed);
        _mockPublishEndpoint.Verify(p => p.Publish(
            It.Is<PlateSoldIntegrationEvent>(e =>
                e.PlateId == plateId &&
                e.SoldPrice == 120m &&
                e.PromoCode == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SellPlateAsync_ForSalePlate_ShouldThrowException()
    {
        // Arrange
        var plateId = Guid.NewGuid();
        var plate = new Plate
        {
            Id = plateId,
            Registration = "AB12 CDE",
            PurchasePrice = 100m,
            SalePrice = 120m
        };
        // Plate is ForSale, not reserved

        _mockRepository.Setup(r => r.GetByIdAsync(plateId)).ReturnsAsync(plate);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SellPlateAsync(plateId, null));
    }

    #endregion

    #region User Story 7 - Promo Codes

    [Fact]
    public async Task SellPlateAsync_WithDISCOUNTPromoCode_ShouldApply25Discount()
    {
        // Arrange
        var plateId = Guid.NewGuid();
        var plate = new Plate
        {
            Id = plateId,
            Registration = "AB12 CDE",
            PurchasePrice = 300m, // Sale price = £360, with DISCOUNT = £335, 90% min = £324
            SalePrice = 360m
        };
        plate.Reserve();

        _mockRepository.Setup(r => r.GetByIdAsync(plateId)).ReturnsAsync(plate);
        _mockRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.SellPlateAsync(plateId, "DISCOUNT");

        // Assert
        Assert.Equal(PlateStatus.Sold, result.Status);
        Assert.Equal(335m, result.SoldPrice); // £360 - £25 = £335
        Assert.Equal("DISCOUNT", result.PromoCodeUsed);
    }

    [Fact]
    public async Task SellPlateAsync_WithPERCENTOFFPromoCode_ViolatesMinimumPrice()
    {
        // Arrange - NOTE: 15% off (PERCENTOFF) always brings price to 85%, which is below the 90% minimum
        // This test verifies that User Story 8 (90% minimum) is enforced even with User Story 7 (promo codes)
        var plateId = Guid.NewGuid();
        var plate = new Plate
        {
            Id = plateId,
            Registration = "AB12 CDE",
            PurchasePrice = 500m, // Sale price = £600, with PERCENTOFF (85%) = £510, 90% min = £540
            SalePrice = 600m
        };
        plate.Reserve();

        _mockRepository.Setup(r => r.GetByIdAsync(plateId)).ReturnsAsync(plate);

        // Act & Assert - Should throw exception because 85% < 90%
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.SellPlateAsync(plateId, "PERCENTOFF"));
    }

    [Fact]
    public async Task SellPlateAsync_WithInvalidPromoCode_ShouldThrowException()
    {
        // Arrange
        var plateId = Guid.NewGuid();
        var plate = new Plate
        {
            Id = plateId,
            Registration = "AB12 CDE",
            PurchasePrice = 100m,
            SalePrice = 120m
        };
        plate.Reserve();

        _mockRepository.Setup(r => r.GetByIdAsync(plateId)).ReturnsAsync(plate);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SellPlateAsync(plateId, "INVALIDCODE"));
        Assert.Contains("Invalid promo code", exception.Message);
    }

    [Fact]
    public async Task CalculatePriceAsync_WithDISCOUNTPromoCode_ShouldReturn25Discount()
    {
        // Arrange
        var plateId = Guid.NewGuid();
        var plate = new Plate
        {
            Id = plateId,
            Registration = "AB12 CDE",
            PurchasePrice = 100m,
            SalePrice = 120m
        };

        _mockRepository.Setup(r => r.GetByIdAsync(plateId)).ReturnsAsync(plate);

        // Act
        var price = await _service.CalculatePriceAsync(plateId, "DISCOUNT");

        // Assert
        Assert.Equal(95m, price); // £120 - £25 = £95
    }

    [Fact]
    public async Task CalculatePriceAsync_WithPERCENTOFFPromoCode_ShouldReturn15PercentDiscount()
    {
        // Arrange
        var plateId = Guid.NewGuid();
        var plate = new Plate
        {
            Id = plateId,
            Registration = "AB12 CDE",
            PurchasePrice = 100m,
            SalePrice = 120m
        };

        _mockRepository.Setup(r => r.GetByIdAsync(plateId)).ReturnsAsync(plate);

        // Act
        var price = await _service.CalculatePriceAsync(plateId, "PERCENTOFF");

        // Assert
        Assert.Equal(102m, price); // £120 * 0.85 = £102
    }

    #endregion

    #region User Story 8 - 90% Minimum Price Rule

    [Fact]
    public async Task SellPlateAsync_WithPriceBelowMinimum_ShouldThrowException()
    {
        // Arrange
        var plateId = Guid.NewGuid();
        var plate = new Plate
        {
            Id = plateId,
            Registration = "AB12 CDE",
            PurchasePrice = 200m, // Sale price = £240, minimum = £216 (90%)
            SalePrice = 240m
        };
        plate.Reserve();

        _mockRepository.Setup(r => r.GetByIdAsync(plateId)).ReturnsAsync(plate);

        // Manually create a promo that would bring price below 90%
        // We need to test the domain validation

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            // Simulate selling at a price below 90% by calling the domain method directly
            plate.Sell(215m, "TEST");
        });
        
        Assert.Contains("below the minimum allowed price", exception.Message);
    }

    [Fact]
    public async Task SellPlateAsync_WithPriceAt90Percent_ShouldSucceed()
    {
        // Arrange
        var plateId = Guid.NewGuid();
        var plate = new Plate
        {
            Id = plateId,
            Registration = "AB12 CDE",
            PurchasePrice = 200m, // Sale price = £240, 90% = £216
            SalePrice = 240m
        };
        plate.Reserve();

        _mockRepository.Setup(r => r.GetByIdAsync(plateId)).ReturnsAsync(plate);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Plate>())).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act - Sell at exactly 90%
        plate.Sell(216m, "CUSTOM");

        // Assert
        Assert.Equal(PlateStatus.Sold, plate.Status);
        Assert.Equal(216m, plate.SoldPrice);
    }

    #endregion

    #region Repository Integration

    [Fact]
    public async Task CreatePlateAsync_ShouldSetSalePriceAndSave()
    {
        // Arrange
        var plate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "AB12 CDE",
            PurchasePrice = 100m,
            SalePrice = 0 // Not yet calculated
        };

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Plate>())).ReturnsAsync(plate);
        _mockRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePlateAsync(plate);

        // Assert
        Assert.Equal(120m, result.SalePrice); // Should be calculated
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Plate>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetRevenueStatisticsAsync_ShouldReturnStatistics()
    {
        // Arrange
        var stats = new RevenueStatistics
        {
            TotalRevenue = 1000m,
            TotalProfit = 200m,
            PlatesSold = 5,
            AverageProfitMargin = 0.20m
        };

        _mockRepository.Setup(r => r.GetRevenueStatisticsAsync()).ReturnsAsync(stats);

        // Act
        var result = await _service.GetRevenueStatisticsAsync();

        // Assert
        Assert.Equal(1000m, result.TotalRevenue);
        Assert.Equal(200m, result.TotalProfit);
        Assert.Equal(5, result.PlatesSold);
        Assert.Equal(0.20m, result.AverageProfitMargin);
    }

    #endregion
}
