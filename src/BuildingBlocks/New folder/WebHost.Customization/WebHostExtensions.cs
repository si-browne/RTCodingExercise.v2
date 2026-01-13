using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using Microsoft.Data.SqlClient;

namespace Microsoft.AspNetCore.Hosting;

public static class IWebHostExtensions
{
    public static WebApplication MigrateDbContext<TContext>(this WebApplication app, Action<TContext, IServiceProvider> seeder) where TContext : DbContext
        {
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                MigrateDbContextInternal(services, seeder);
            }

            return app;
        }

        private static void MigrateDbContextInternal<TContext>(IServiceProvider services, Action<TContext, IServiceProvider> seeder) where TContext : DbContext
        {
            var logger = services.GetRequiredService<ILogger<TContext>>();
            var context = services.GetService<TContext>();

            try
            {
                logger.LogInformation("Migrating database associated with context {DbContextName}", typeof(TContext).Name);

                var retries = 10;
                
                // Polly v8 uses ResiliencePipelineBuilder
                var pipeline = new ResiliencePipelineBuilder()
                    .AddRetry(new RetryStrategyOptions
                    {
                        ShouldHandle = new PredicateBuilder().Handle<SqlException>(),
                        MaxRetryAttempts = retries,
                        Delay = TimeSpan.FromSeconds(2),
                        BackoffType = DelayBackoffType.Exponential,
                        OnRetry = args =>
                        {
                            var exception = args.Outcome.Exception;
                            logger.LogWarning(exception, "[{prefix}] Exception {ExceptionType} with message {Message} detected on attempt {retry} of {retries}", 
                                nameof(TContext), exception?.GetType().Name, exception?.Message, args.AttemptNumber, retries);
                            return default;
                        }
                    })
                    .Build();

                //if the sql server container is not created on run docker compose this
                //migration can't fail for network related exception. The retry options for DbContext only 
                //apply to transient exceptions
                // Note that this is NOT applied when running some orchestrators (let the orchestrator to recreate the failing service)
                pipeline.Execute(() => InvokeSeeder(seeder, context, services));

                logger.LogInformation("Migrated database associated with context {DbContextName}", typeof(TContext).Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while migrating the database used on context {DbContextName}", typeof(TContext).Name);
            }
        }

    private static void InvokeSeeder<TContext>(Action<TContext, IServiceProvider> seeder, TContext? context, IServiceProvider services)
        where TContext : DbContext
    {
        if (context is null)
        {
            throw new InvalidOperationException($"Database context {typeof(TContext).Name} is not registered.");
        }
        
        context.Database.Migrate();
        seeder(context, services);
    }
}
