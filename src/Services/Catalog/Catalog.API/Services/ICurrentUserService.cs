namespace Catalog.API.Services
{
    public interface ICurrentUserService
    {
        Guid GetUserIdOrDefault();
    }
}
