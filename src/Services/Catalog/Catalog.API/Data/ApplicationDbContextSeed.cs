using Newtonsoft.Json;

namespace Catalog.API.Data
{
    public class ApplicationDbContextSeed
    {
        public async Task SeedAsync(ApplicationDbContext context, IWebHostEnvironment env, ILogger<ApplicationDbContextSeed> logger, IOptions<AppSettings> settings, int? retry = 0)
        {
            int retryForAvaiability = retry ?? 0;

            try
            {
                await SeedCustomData(context, env, logger);
            }
            catch (Exception ex)
            {
                // used for initilisaton of docker containers
                if (retryForAvaiability < 10)
                {
                    retryForAvaiability++;

                    logger.LogError(ex.Message, $"There is an error migrating data for ApplicationDbContext");

                    await SeedAsync(context, env, logger, settings, retryForAvaiability);
                }
            }
        }

        public async Task SeedCustomData(ApplicationDbContext context, IWebHostEnvironment env, ILogger<ApplicationDbContextSeed> logger)
        {
            try
            {
                var plates = ReadApplicationRoleFromJson(env.ContentRootPath, logger);

                await context.Plates.AddRangeAsync(plates);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
                throw;
            }
        }

        public List<Plate> ReadApplicationRoleFromJson(string contentRootPath, ILogger<ApplicationDbContextSeed> logger)
        {
            string filePath = Path.Combine(contentRootPath, "Setup", "plates.json");
            string json = File.ReadAllText(filePath);

            var plates = JsonConvert.DeserializeObject<List<Plate>>(json) ?? new List<Plate>();

            return plates;
        }
    }
}
