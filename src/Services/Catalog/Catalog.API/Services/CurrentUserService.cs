namespace Catalog.API.Services;

/// <summary>
/// Small service to get current userId as per reqs
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetUserIdOrDefault()
    {
        var context = _httpContextAccessor.HttpContext;

        if (context == null) return Guid.Empty;

        if (context.Request.Headers.TryGetValue("X-User-Id", out var values) && Guid.TryParse(values.ToString(), out var userId))
        {
            return userId;
        }

        return Guid.Empty;
    }
}
