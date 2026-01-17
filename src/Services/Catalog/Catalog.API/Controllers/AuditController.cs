using System.Text;
using Catalog.API.Models;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AuditController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Query audit history (per plate)
    /// </summary>
    /// <param name="plateId"></param>
    /// <param name="fromUtc"></param>
    /// <param name="toUtc"></param>
    /// <returns></returns>
    // GET /api/audit/plates/{plateId}
    [HttpGet("plates/{plateId:guid}")]
    [ProducesResponseType(typeof(List<AuditLogEventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AuditLogEventDto>>> GetPlateAudit(Guid plateId, [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc)
    {
        var query = _db.AuditLogEvents
            .AsNoTracking()
            .Include(e => e.Changes)
            .Where(e => e.PlateId == plateId);

        if (fromUtc.HasValue) query = query.Where(e => e.Timestamp >= DateTime.SpecifyKind(fromUtc.Value, DateTimeKind.Utc));
        if (toUtc.HasValue) query = query.Where(e => e.Timestamp <= DateTime.SpecifyKind(toUtc.Value, DateTimeKind.Utc));

        var items = await query
            .OrderBy(e => e.Timestamp)
            .Select(e => new AuditLogEventDto
            {
                AuditLogEventId = e.AuditLogEventId,
                PlateId = e.PlateId,
                UserId = e.UserId,
                Timestamp = e.Timestamp,
                Status = e.Status,
                Changes = e.Changes
                    .OrderBy(c => c.FieldName)
                    .Select(c => new AuditLogEventChangeDto
                    {
                        AuditLogEventChangeId = c.AuditLogEventChangeId,
                        FieldName = c.FieldName,
                        OldValue = c.OldValue,
                        NewValue = c.NewValue
                    }).ToList()
            })
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>
    /// export audit logs (date range considered)
    /// </summary>
    /// <param name="fromUtc"></param>
    /// <param name="toUtc"></param>
    /// <param name="format"></param>
    /// <param name="plateId"></param>
    /// <returns></returns>
    // GET /api/audit/export?fromUtc=...&toUtc=...&format=csv|json[&plateId=...]
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, [FromQuery] string format = "csv", [FromQuery] Guid? plateId = null)
    {
        var from = DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(toUtc, DateTimeKind.Utc);

        var query = _db.AuditLogEvents
            .AsNoTracking()
            .Include(e => e.Changes)
            .Where(e => e.Timestamp >= from && e.Timestamp <= to);

        if (plateId.HasValue) query = query.Where(e => e.PlateId == plateId.Value);

        var data = await query
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            var dto = data.Select(e => new AuditLogEventDto
            {
                AuditLogEventId = e.AuditLogEventId,
                PlateId = e.PlateId,
                UserId = e.UserId,
                Timestamp = e.Timestamp,
                Status = e.Status,
                Changes = e.Changes.Select(c => new AuditLogEventChangeDto
                {
                    AuditLogEventChangeId = c.AuditLogEventChangeId,
                    FieldName = c.FieldName,
                    OldValue = c.OldValue,
                    NewValue = c.NewValue
                }).ToList()
            }).ToList();

            // chosen file name, time unique
            var fileName = $"audit_{from:yyyyMMddHHmmss}_{to:yyyyMMddHHmmss}.json";

            return File(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(dto), "application/json", fileName);
        }

        // default CSV (flatten changes)
        var csv = new StringBuilder();
        csv.AppendLine("AuditLogEventId,PlateId,UserId,TimestampUtc,Status,FieldName,OldValue,NewValue");

        foreach (var e in data)
        {
            if (e.Changes == null || e.Changes.Count == 0)
            {
                csv.AppendLine(string.Join(",",
                    e.AuditLogEventId,
                    e.PlateId,
                    e.UserId,
                    e.Timestamp.ToString("O"),
                    e.Status,
                    "", "", ""
                ));
                continue;
            }

            foreach (var c in e.Changes)
            {
                csv.AppendLine(string.Join(",",
                    e.AuditLogEventId,
                    e.PlateId,
                    e.UserId,
                    e.Timestamp.ToString("O"),
                    e.Status,
                    c.FieldName,
                    c.OldValue,
                    c.NewValue
                ));
            }
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        var name = $"audit_{from:yyyyMMddHHmmss}_{to:yyyyMMddHHmmss}.csv";

        return File(bytes, "text/csv", name);
    }
}
