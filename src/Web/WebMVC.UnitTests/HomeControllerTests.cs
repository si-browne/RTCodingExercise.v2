using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using RTCodingExercise.Microservices.Controllers;
using RTCodingExercise.Microservices.Models;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace WebMVC.UnitTests;

/// <summary>
/// Unit tests for HomeController, verifying integration with Catalog API
/// according to the Regtransfers Code Exercise requirements
/// </summary>
public class HomeControllerTests
{
    private readonly Mock<ILogger<HomeController>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _mockLogger = new Mock<ILogger<HomeController>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        _mockHttpClientFactory
            .Setup(f => f.CreateClient("CatalogApi"))
            .Returns(httpClient);

        _controller = new HomeController(
            _mockLogger.Object,
            _mockHttpClientFactory.Object,
            _mockConfiguration.Object);

        // Initialize TempData with a simple dictionary
        _controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());
    }

    #region User Story 1 - Display Plates with Pagination

    [Fact]
    public async Task Index_ShouldReturnPlatesWithPageSize20()
    {
        // Arrange
        var plates = CreateTestPlates(25);
        var pagedResult = new PagedResult<Plate>
        {
            Items = plates.Take(20).ToList(),
            TotalCount = 25,
            Page = 1,
            PageSize = 20
        };

        SetupHttpResponse("/api/plates?status=0&page=1&pageSize=20", pagedResult);
        SetupHttpResponse("/api/plates/statistics/revenue", new RevenueStatistics());

        // Act
        var result = await _controller.Index(null, null, null, null, null, 1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PagedResult<Plate>>(viewResult.Model);
        Assert.Equal(20, model.Items.Count);
        Assert.Equal(25, model.TotalCount);
    }

    [Fact]
    public async Task Index_Page2_ShouldReturnRemainingPlates()
    {
        // Arrange
        var plates = CreateTestPlates(25);
        var pagedResult = new PagedResult<Plate>
        {
            Items = plates.Skip(20).Take(5).ToList(),
            TotalCount = 25,
            Page = 2,
            PageSize = 20
        };

        SetupHttpResponse("/api/plates?status=0&page=2&pageSize=20", pagedResult);
        SetupHttpResponse("/api/plates/statistics/revenue", new RevenueStatistics());

        // Act
        var result = await _controller.Index(null, null, null, null, null, 2);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PagedResult<Plate>>(viewResult.Model);
        Assert.Equal(5, model.Items.Count);
        Assert.Equal(2, model.Page);
    }

    #endregion

    #region User Story 2 - Sort by Price

    [Fact]
    public async Task Index_WithPriceSortAsc_ShouldPassSortParameter()
    {
        // Arrange
        var pagedResult = new PagedResult<Plate> { Items = new List<Plate>(), TotalCount = 0, Page = 1, PageSize = 20 };
        SetupHttpResponse("/api/plates?status=0&sortBy=price_asc&page=1&pageSize=20", pagedResult);
        SetupHttpResponse("/api/plates/statistics/revenue", new RevenueStatistics());

        // Act
        var result = await _controller.Index(null, null, null, null, "price_asc", 1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("price_asc", _controller.ViewBag.SortBy);
    }

    [Fact]
    public async Task Index_WithPriceSortDesc_ShouldPassSortParameter()
    {
        // Arrange
        var pagedResult = new PagedResult<Plate> { Items = new List<Plate>(), TotalCount = 0, Page = 1, PageSize = 20 };
        SetupHttpResponse("/api/plates?status=0&sortBy=price_desc&page=1&pageSize=20", pagedResult);
        SetupHttpResponse("/api/plates/statistics/revenue", new RevenueStatistics());

        // Act
        var result = await _controller.Index(null, null, null, null, "price_desc", 1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("price_desc", _controller.ViewBag.SortBy);
    }

    #endregion

    #region User Story 3 - Filter by Letters and Numbers

    [Fact]
    public async Task Index_WithLettersFilter_ShouldPassToApi()
    {
        // Arrange
        var pagedResult = new PagedResult<Plate> { Items = new List<Plate>(), TotalCount = 0, Page = 1, PageSize = 20 };
        SetupHttpResponse("/api/plates?letters=ABC&status=0&page=1&pageSize=20", pagedResult);
        SetupHttpResponse("/api/plates/statistics/revenue", new RevenueStatistics());

        // Act
        var result = await _controller.Index(null, "ABC", null, null, null, 1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("ABC", _controller.ViewBag.Letters);
    }

    [Fact]
    public async Task Index_WithNumbersFilter_ShouldPassToApi()
    {
        // Arrange
        var pagedResult = new PagedResult<Plate> { Items = new List<Plate>(), TotalCount = 0, Page = 1, PageSize = 20 };
        SetupHttpResponse("/api/plates?numbers=123&status=0&page=1&pageSize=20", pagedResult);
        SetupHttpResponse("/api/plates/statistics/revenue", new RevenueStatistics());

        // Act
        var result = await _controller.Index(null, null, 123, null, null, 1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(123, _controller.ViewBag.Numbers);
    }

    [Fact]
    public async Task Index_WithSearchFilter_ShouldPassToApi()
    {
        // Arrange
        var pagedResult = new PagedResult<Plate> { Items = new List<Plate>(), TotalCount = 0, Page = 1, PageSize = 20 };
        SetupHttpResponse("/api/plates?search=AB12&status=0&page=1&pageSize=20", pagedResult);
        SetupHttpResponse("/api/plates/statistics/revenue", new RevenueStatistics());

        // Act
        var result = await _controller.Index("AB12", null, null, null, null, 1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("AB12", _controller.ViewBag.Search);
    }

    #endregion

    #region User Story 4 - Reserve/Unreserve Plates

    [Fact]
    public async Task Reserve_ValidPlate_ShouldRedirectWithSuccessMessage()
    {
        // Arrange
        var plateId = Guid.NewGuid();
        SetupHttpPostResponse($"/api/plates/{plateId}/reserve", HttpStatusCode.OK);

        // Act
        var result = await _controller.Reserve(plateId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Plate reserved successfully!", _controller.TempData["Success"]);
    }

    [Fact]
    public async Task Unreserve_ValidPlate_ShouldRedirectWithSuccessMessage()
    {
        // Arrange
        var plateId = Guid.NewGuid();
        SetupHttpPostResponse($"/api/plates/{plateId}/unreserve", HttpStatusCode.OK);

        // Act
        var result = await _controller.Unreserve(plateId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Plate unreserved successfully!", _controller.TempData["Success"]);
    }

    #endregion

    #region User Story 5 - Default to ForSale

    [Fact]
    public async Task Index_WithNoStatus_ShouldDefaultToForSale()
    {
        // Arrange
        var pagedResult = new PagedResult<Plate> { Items = new List<Plate>(), TotalCount = 0, Page = 1, PageSize = 20 };
        SetupHttpResponse("/api/plates?status=0&page=1&pageSize=20", pagedResult);
        SetupHttpResponse("/api/plates/statistics/revenue", new RevenueStatistics());

        // Act
        var result = await _controller.Index(null, null, null, null, null, 1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(PlateStatus.ForSale, _controller.ViewBag.Status);
    }

    #endregion

    #region User Story 6 - Sell Plates

    [Fact]
    public async Task Sell_ValidPlate_ShouldRedirectWithSuccessMessage()
    {
        // Arrange
        var plateId = Guid.NewGuid();
        SetupHttpPostResponse($"/api/plates/{plateId}/sell", HttpStatusCode.OK);

        // Act
        var result = await _controller.Sell(plateId, null);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Plate sold successfully!", _controller.TempData["Success"]);
    }

    [Fact]
    public async Task Sell_WithPromoCode_ShouldIncludePromoCodeInRequest()
    {
        // Arrange
        var plateId = Guid.NewGuid();
        SetupHttpPostResponse($"/api/plates/{plateId}/sell?promoCode=DISCOUNT", HttpStatusCode.OK);

        // Act
        var result = await _controller.Sell(plateId, "DISCOUNT");

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Plate sold successfully!", _controller.TempData["Success"]);
    }

    [Fact]
    public async Task Index_ShouldDisplayRevenueStatistics()
    {
        // Arrange
        var pagedResult = new PagedResult<Plate> { Items = new List<Plate>(), TotalCount = 0, Page = 1, PageSize = 20 };
        var stats = new RevenueStatistics
        {
            TotalRevenue = 1200.00m,
            TotalProfit = 200.00m,
            PlatesSold = 10,
            AverageProfitMargin = 0.1667m
        };

        SetupHttpResponse("/api/plates?status=0&page=1&pageSize=20", pagedResult);
        SetupHttpResponse("/api/plates/statistics/revenue", stats);

        // Act
        var result = await _controller.Index(null, null, null, null, null, 1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var viewBagStats = _controller.ViewBag.Statistics as RevenueStatistics;
        Assert.NotNull(viewBagStats);
        Assert.Equal(1200.00m, viewBagStats.TotalRevenue);
        Assert.Equal(200.00m, viewBagStats.TotalProfit);
        Assert.Equal(10, viewBagStats.PlatesSold);
        Assert.Equal(0.1667m, viewBagStats.AverageProfitMargin);
    }

    #endregion

    #region User Story 7 - Promo Code Price Calculator

    [Fact]
    public async Task CalculatePrice_WithoutPromoCode_ShouldReturnFullPrice()
    {
        // Arrange
        var plateId = Guid.NewGuid();
        SetupHttpResponse($"/api/plates/{plateId}/calculate-price", 120.00m);

        // Act
        var result = await _controller.CalculatePrice(plateId, null);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        dynamic? value = jsonResult.Value;
        Assert.NotNull(value);
        Assert.True(value.GetType().GetProperty("success")?.GetValue(value, null) as bool?);
        Assert.Equal(120.00m, value.GetType().GetProperty("price")?.GetValue(value, null));
    }

    [Fact]
    public async Task CalculatePrice_WithDISCOUNTPromoCode_ShouldReturn25Discount()
    {
        // Arrange
        var plateId = Guid.NewGuid();
        SetupHttpResponse($"/api/plates/{plateId}/calculate-price?promoCode=DISCOUNT", 95.00m);

        // Act
        var result = await _controller.CalculatePrice(plateId, "DISCOUNT");

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        dynamic? value = jsonResult.Value;
        Assert.NotNull(value);
        Assert.True(value.GetType().GetProperty("success")?.GetValue(value, null) as bool?);
        Assert.Equal(95.00m, value.GetType().GetProperty("price")?.GetValue(value, null));
    }

    #endregion

    #region Helper Methods

    private List<Plate> CreateTestPlates(int count)
    {
        var plates = new List<Plate>();
        for (int i = 0; i < count; i++)
        {
            plates.Add(new Plate
            {
                Id = Guid.NewGuid(),
                Registration = $"AB{i:D2} CDE",
                PurchasePrice = 100 + (i * 10),
                SalePrice = 120 + (i * 12),
                Status = PlateStatus.ForSale
            });
        }
        return plates;
    }

    private void SetupHttpResponse<T>(string requestUri, T responseData)
    {
        var jsonResponse = JsonSerializer.Serialize(responseData);
        var httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri != null &&
                    req.RequestUri.PathAndQuery.Contains(requestUri)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);
    }

    private void SetupHttpPostResponse(string requestUri, HttpStatusCode statusCode)
    {
        var httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.PathAndQuery.Contains(requestUri)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);
    }

    #endregion
}
