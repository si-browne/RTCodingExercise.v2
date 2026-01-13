using RTCodingExercise.Microservices.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace RTCodingExercise.Microservices.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public HomeController(
            ILogger<HomeController> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index(
            string? search,
            string? letters,
            int? numbers,
            RTCodingExercise.Microservices.Models.PlateStatus? status,
            string? sortBy,
            int page = 1)
        {
            var client = _httpClientFactory.CreateClient("CatalogApi");
            
            // Build query string
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"search={Uri.EscapeDataString(search)}");
            if (!string.IsNullOrWhiteSpace(letters))
                queryParams.Add($"letters={Uri.EscapeDataString(letters)}");
            if (numbers.HasValue)
                queryParams.Add($"numbers={numbers.Value}");
            if (status.HasValue)
                queryParams.Add($"status={status.Value}");
            else
                queryParams.Add("status=0"); // Default to ForSale
            if (!string.IsNullOrWhiteSpace(sortBy))
                queryParams.Add($"sortBy={sortBy}");
            queryParams.Add($"page={page}");
            queryParams.Add("pageSize=20");

            var queryString = string.Join("&", queryParams);
            
            // Get plates
            var platesResponse = await client.GetAsync($"/api/plates?{queryString}");
            platesResponse.EnsureSuccessStatusCode();
            var platesResult = await platesResponse.Content.ReadFromJsonAsync<PagedResult<Plate>>();

            // Get revenue statistics
            var statsResponse = await client.GetAsync("/api/plates/statistics/revenue");
            statsResponse.EnsureSuccessStatusCode();
            var stats = await statsResponse.Content.ReadFromJsonAsync<RevenueStatistics>();

            ViewBag.Statistics = stats;
            ViewBag.Search = search;
            ViewBag.Letters = letters;
            ViewBag.Numbers = numbers;
            ViewBag.Status = status ?? RTCodingExercise.Microservices.Models.PlateStatus.ForSale;
            ViewBag.SortBy = sortBy;

            return View(platesResult);
        }

        [HttpPost]
        public async Task<IActionResult> Reserve(Guid id)
        {
            var client = _httpClientFactory.CreateClient("CatalogApi");
            var response = await client.PostAsync($"/api/plates/{id}/reserve", null);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Failed to reserve plate: {error}";
            }
            else
            {
                TempData["Success"] = "Plate reserved successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Unreserve(Guid id)
        {
            var client = _httpClientFactory.CreateClient("CatalogApi");
            var response = await client.PostAsync($"/api/plates/{id}/unreserve", null);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Failed to unreserve plate: {error}";
            }
            else
            {
                TempData["Success"] = "Plate unreserved successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Sell(Guid id, string? promoCode)
        {
            var client = _httpClientFactory.CreateClient("CatalogApi");
            var url = $"/api/plates/{id}/sell";
            if (!string.IsNullOrWhiteSpace(promoCode))
                url += $"?promoCode={Uri.EscapeDataString(promoCode)}";

            var response = await client.PostAsync(url, null);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Failed to sell plate: {error}";
            }
            else
            {
                TempData["Success"] = "Plate sold successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CalculatePrice(Guid id, string? promoCode)
        {
            var client = _httpClientFactory.CreateClient("CatalogApi");
            var url = $"/api/plates/{id}/calculate-price";
            if (!string.IsNullOrWhiteSpace(promoCode))
                url += $"?promoCode={Uri.EscapeDataString(promoCode)}";

            var response = await client.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var price = await response.Content.ReadFromJsonAsync<decimal>();
                return Json(new { success = true, price });
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, error });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}