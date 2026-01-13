using Catalog.Domain;
using Xunit;

namespace Catalog.UnitTests;

/// <summary>
/// Unit tests for the Plate domain entity, verifying business logic and rules
/// according to the Regtransfers Code Exercise requirements
/// </summary>
public class PlateTests
{
    #region User Story 1 - Sale Price Calculation with 20% Markup

    [Fact]
    public void CalculateSalePrice_ShouldApply20PercentMarkup()
    {
        // Arrange
        var plate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "AB12 CDE",
            PurchasePrice = 100.00m
        };

        // Act
        var salePrice = plate.CalculateSalePrice();

        // Assert
        Assert.Equal(120.00m, salePrice);
    }

    [Theory]
    [InlineData(50.00, 60.00)]
    [InlineData(100.00, 120.00)]
    [InlineData(250.50, 300.60)]
    [InlineData(1000.00, 1200.00)]
    public void CalculateSalePrice_VariousPrices_ShouldApply20PercentMarkup(decimal purchasePrice, decimal expectedSalePrice)
    {
        // Arrange
        var plate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "TEST",
            PurchasePrice = purchasePrice
        };

        // Act
        var salePrice = plate.CalculateSalePrice();

        // Assert
        Assert.Equal(expectedSalePrice, salePrice);
    }

    #endregion

    #region User Story 4 - Reserve Plates

    [Fact]
    public void Reserve_ForSalePlate_ShouldChangeStatusToReserved()
    {
        // Arrange
        var plate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "AB12 CDE",
            PurchasePrice = 100.00m
        };

        // Act
        plate.Reserve();

        // Assert
        Assert.Equal(PlateStatus.Reserved, plate.Status);
        Assert.NotNull(plate.ReservedDate);
    }

    [Fact]
    public void Reserve_AlreadyReservedPlate_ShouldThrowException()
    {
        // Arrange
        var plate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "AB12 CDE",
            PurchasePrice = 100.00m
        };
        plate.Reserve();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => plate.Reserve());
        Assert.Contains("Cannot reserve plate", exception.Message);
        Assert.Contains("Reserved", exception.Message);
    }

    [Fact]
    public void Reserve_SoldPlate_ShouldThrowException()
    {
        // Arrange
        var plate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "AB12 CDE",
            PurchasePrice = 100.00m
        };
        plate.Reserve();
        plate.Sell(120.00m, null);

        // Act & Assert - trying to reserve an already sold plate
        var newPlate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "XY99 ZZZ",
            PurchasePrice = 100.00m
        };
        newPlate.Reserve();
        newPlate.Sell(120.00m, null);

        var exception = Assert.Throws<InvalidOperationException>(() => newPlate.Reserve());
        Assert.Contains("Cannot reserve plate", exception.Message);
    }

    [Fact]
    public void Unreserve_ReservedPlate_ShouldChangeStatusToForSale()
    {
        // Arrange
        var plate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "AB12 CDE",
            PurchasePrice = 100.00m
        };
        plate.Reserve();

        // Act
        plate.Unreserve();

        // Assert
        Assert.Equal(PlateStatus.ForSale, plate.Status);
        Assert.Null(plate.ReservedDate);
    }

    [Fact]
    public void Unreserve_ForSalePlate_ShouldThrowException()
    {
        // Arrange
        var plate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "AB12 CDE",
            PurchasePrice = 100.00m
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => plate.Unreserve());
        Assert.Contains("Cannot unreserve plate", exception.Message);
        Assert.Contains("ForSale", exception.Message);
    }

    #endregion

    #region User Story 6 - Sell Plates

    [Fact]
    public void Sell_ReservedPlate_ShouldChangeStatusToSold()
    {
        // Arrange
        var plate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "AB12 CDE",
            PurchasePrice = 100.00m
        };
        plate.Reserve();

        // Act
        plate.Sell(120.00m, null);

        // Assert
        Assert.Equal(PlateStatus.Sold, plate.Status);
        Assert.Equal(120.00m, plate.SoldPrice);
        Assert.NotNull(plate.SoldDate);
    }

    [Fact]
    public void Sell_ForSalePlate_ShouldThrowException()
    {
        // Arrange
        var plate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "AB12 CDE",
            PurchasePrice = 100.00m
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => plate.Sell(120.00m, null));
        Assert.Contains("Cannot sell plate", exception.Message);
        Assert.Contains("ForSale", exception.Message);
    }

    [Fact]
    public void Sell_WithPromoCode_ShouldRecordPromoCode()
    {
        // Arrange
        var plate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "AB12 CDE",
            PurchasePrice = 200.00m // Sale price = £240, DISCOUNT = £215, min = £216 (90%)
        };
        plate.Reserve();

        // Act - Sell at a price that respects 90% minimum even with discount
        plate.Sell(220.00m, "DISCOUNT");

        // Assert
        Assert.Equal(PlateStatus.Sold, plate.Status);
        Assert.Equal("DISCOUNT", plate.PromoCodeUsed);
        Assert.Equal(220.00m, plate.SoldPrice);
    }

    [Fact]
    public void CalculateProfitMargin_SoldPlate_ShouldReturnCorrectMargin()
    {
        // Arrange
        var plate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "AB12 CDE",
            PurchasePrice = 100.00m // Purchase price (exc VAT)
        };
        plate.Reserve();
        plate.Sell(120.00m, null); // Sold price (inc VAT)

        // Act
        var profitMargin = plate.CalculateProfitMargin();

        // Assert
        // Profit = £120 - £100 = £20
        // Margin = £20 / £120 = 0.1667 (16.67%)
        Assert.Equal(0.1666666666666666666666666667m, profitMargin);
    }

    [Fact]
    public void CalculateProfit_SoldPlate_ShouldReturnCorrectProfit()
    {
        // Arrange
        var plate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "AB12 CDE",
            PurchasePrice = 100.00m
        };
        plate.Reserve();
        plate.Sell(120.00m, null);

        // Act
        var profit = plate.CalculateProfit();

        // Assert
        Assert.Equal(20.00m, profit);
    }

    [Fact]
    public void CalculateProfit_UnsoldPlate_ShouldReturnZero()
    {
        // Arrange
        var plate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "AB12 CDE",
            PurchasePrice = 100.00m
        };

        // Act
        var profit = plate.CalculateProfit();

        // Assert
        Assert.Equal(0m, profit);
    }

    #endregion

    #region User Story 8 - Minimum Price Validation (90% Rule)

    [Fact]
    public void Sell_PriceBelow90Percent_ShouldThrowException()
    {
        // Arrange
        var plate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "AB12 CDE",
            PurchasePrice = 100.00m // Sale price = £120
        };
        plate.Reserve();

        // Act & Assert - Trying to sell below £108 (90% of £120)
        var exception = Assert.Throws<InvalidOperationException>(() => plate.Sell(107.00m, null));
        Assert.Contains("below the minimum allowed price", exception.Message);
        Assert.Contains("90%", exception.Message);
    }

    [Fact]
    public void Sell_PriceAt90Percent_ShouldSucceed()
    {
        // Arrange
        var plate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "AB12 CDE",
            PurchasePrice = 100.00m // Sale price = £120, 90% = £108
        };
        plate.Reserve();

        // Act
        plate.Sell(108.00m, "DISCOUNT");

        // Assert
        Assert.Equal(PlateStatus.Sold, plate.Status);
        Assert.Equal(108.00m, plate.SoldPrice);
    }

    [Theory]
    [InlineData(200.00, 216.00)] // Sale price £240, 90% = £216
    [InlineData(200.00, 240.00)] // Full price
    [InlineData(200.00, 220.00)] // Above 90%
    public void Sell_PriceAt90PercentOrAbove_ShouldSucceed(decimal purchasePrice, decimal finalPrice)
    {
        // Arrange
        var plate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "AB12 CDE",
            PurchasePrice = purchasePrice
        };
        plate.Reserve();

        // Act
        plate.Sell(finalPrice, null);

        // Assert
        Assert.Equal(PlateStatus.Sold, plate.Status);
        Assert.Equal(finalPrice, plate.SoldPrice);
    }

    [Theory]
    [InlineData(200.00, 215.99)] // Just below 90% of £240 (90% = £216)
    [InlineData(200.00, 100.00)]  // Way below
    public void Sell_PriceBelowMinimum_ShouldThrowException(decimal purchasePrice, decimal finalPrice)
    {
        // Arrange
        var plate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "AB12 CDE",
            PurchasePrice = purchasePrice
        };
        plate.Reserve();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => plate.Sell(finalPrice, null));
        Assert.Contains("below the minimum allowed price", exception.Message);
    }

    #endregion

    #region Default Status

    [Fact]
    public void NewPlate_ShouldDefaultToForSaleStatus()
    {
        // Arrange & Act
        var plate = new Plate
        {
            Id = Guid.NewGuid(),
            Registration = "AB12 CDE",
            PurchasePrice = 100.00m
        };

        // Assert
        Assert.Equal(PlateStatus.ForSale, plate.Status);
    }

    #endregion
}
