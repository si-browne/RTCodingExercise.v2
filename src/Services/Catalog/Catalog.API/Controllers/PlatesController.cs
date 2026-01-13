using Catalog.API.Models;
using Catalog.API.Services;
using Catalog.Domain;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlatesController : ControllerBase
{
    private readonly IPlateService _plateService;
    private readonly ILogger<PlatesController> _logger;

    public PlatesController(IPlateService plateService, ILogger<PlatesController> logger)
    {
        _plateService = plateService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of plates with filtering and sorting (User Stories 1-5)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Plate>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<Plate>>> GetPlates(
        [FromQuery] string? search,
        [FromQuery] string? letters,
        [FromQuery] int? numbers,
        [FromQuery] PlateStatus? status,
        [FromQuery] string? sortBy,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _plateService.GetPlatesAsync(search, letters, numbers, status, sortBy, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific plate by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Plate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Plate>> GetPlate(Guid id)
    {
        var plate = await _plateService.GetPlateByIdAsync(id);
        if (plate == null)
            return NotFound();

        return Ok(plate);
    }

    /// <summary>
    /// Create a new plate
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Plate), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Plate>> CreatePlate([FromBody] Plate plate)
    {
        var created = await _plateService.CreatePlateAsync(plate);
        return CreatedAtAction(nameof(GetPlate), new { id = created.Id }, created);
    }

    /// <summary>
    /// Update an existing plate
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Plate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Plate>> UpdatePlate(Guid id, [FromBody] Plate plate)
    {
        try
        {
            var updated = await _plateService.UpdatePlateAsync(id, plate);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Delete a plate
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePlate(Guid id)
    {
        try
        {
            await _plateService.DeletePlateAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Reserve a plate (User Story - Reserve functionality)
    /// </summary>
    [HttpPost("{id}/reserve")]
    [ProducesResponseType(typeof(Plate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Plate>> ReservePlate(Guid id)
    {
        try
        {
            var plate = await _plateService.ReservePlateAsync(id);
            return Ok(plate);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Unreserve a plate (User Story - Unreserve functionality)
    /// </summary>
    [HttpPost("{id}/unreserve")]
    [ProducesResponseType(typeof(Plate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Plate>> UnreservePlate(Guid id)
    {
        try
        {
            var plate = await _plateService.UnreservePlateAsync(id);
            return Ok(plate);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Sell a plate with optional promo code (User Stories 7-8)
    /// </summary>
    [HttpPost("{id}/sell")]
    [ProducesResponseType(typeof(Plate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Plate>> SellPlate(Guid id, [FromQuery] string? promoCode)
    {
        try
        {
            var plate = await _plateService.SellPlateAsync(id, promoCode);
            return Ok(plate);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Calculate final price with promo code (User Story 7 - Price Calculator)
    /// </summary>
    [HttpGet("{id}/calculate-price")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<decimal>> CalculatePrice(Guid id, [FromQuery] string? promoCode)
    {
        try
        {
            var price = await _plateService.CalculatePriceAsync(id, promoCode);
            return Ok(price);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get revenue statistics (User Story 6)
    /// </summary>
    [HttpGet("statistics/revenue")]
    [ProducesResponseType(typeof(RevenueStatistics), StatusCodes.Status200OK)]
    public async Task<ActionResult<RevenueStatistics>> GetRevenueStatistics()
    {
        var stats = await _plateService.GetRevenueStatisticsAsync();
        return Ok(stats);
    }
}
