using MassTransit;
using RabbitMQ.Client;

const string AppName = "Catalog.API";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .Enrich.WithProperty("ApplicationContext", AppName)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Configuring web host ({ApplicationContext})...", AppName);
    
    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .MinimumLevel.Verbose()
        .Enrich.WithProperty("ApplicationContext", AppName)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Seq(context.Configuration["Serilog:SeqServerUrl"] ?? "http://seq")
        .ReadFrom.Configuration(context.Configuration));

    // Add services to the container
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration["ConnectionString"],
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.MigrationsAssembly(typeof(Program).Assembly.GetName().Name);
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
        }));

    // Register Repository and Service layers (Repository + Service Pattern)
    builder.Services.AddScoped<IPlateRepository, PlateRepository>();
    builder.Services.AddScoped<IPlateService, PlateService>();
    builder.Services.AddScoped<IPlateMatchingService, PlateMatchingService>();

    builder.Services.AddExceptionHandler<Catalog.API.Middleware.GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Regtransfers Coding Exercise - Catalog HTTP API",
            Version = "v1",
            Description = "The Catalog Microservice HTTP API for managing registration plates",
            Contact = new OpenApiContact
            {
                Name = "Regtransfers"
            }
        });

        options.EnableAnnotations();
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy",
            policy => policy
            .WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] 
                { 
                    "http://localhost:5100", 
                    "http://localhost:5102",
                    "http://localhost:4200"
                })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
    });

    builder.Services.AddControllers();
    builder.Services.AddControllersWithViews();
    builder.Services.AddRazorPages();

    builder.Services.AddHealthChecks()
        .AddSqlServer(
            connectionString: builder.Configuration["ConnectionString"] ?? string.Empty,
            name: "CatalogDB-check",
            tags: new[] { "catalogdb" });

    builder.Services.AddMassTransit(x =>
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            var eventBusConnection = builder.Configuration["EventBusConnection"] ?? "localhost";
            var eventBusUserName = builder.Configuration["EventBusUserName"];
            var eventBusPassword = builder.Configuration["EventBusPassword"];
            
            cfg.Host(eventBusConnection, "/", h =>
            {
                if (!string.IsNullOrEmpty(eventBusUserName))
                {
                    h.Username(eventBusUserName);
                }

                if (!string.IsNullOrEmpty(eventBusPassword))
                {
                    h.Password(eventBusPassword);
                }
            });

            cfg.ConfigureEndpoints(context);
            cfg.ExchangeType = ExchangeType.Fanout;
        });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseExceptionHandler();
    app.UseStatusCodePages();

    var pathBase = builder.Configuration["PATH_BASE"];
    if (!string.IsNullOrEmpty(pathBase))
    {
        app.UsePathBase(pathBase);
    }

    app.UseStaticFiles();
    app.UseForwardedHeaders();

    app.UseSwagger()
        .UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint($"{(!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty)}/swagger/v1/swagger.json", "Catalog.API V1");
        });

    app.UseRouting();
    app.UseCors("CorsPolicy");

    app.MapDefaultControllerRoute();
    app.MapControllers();
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/liveness", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false
    });

    Log.Information("Applying migrations ({ApplicationContext})...", AppName);
    app.MigrateDbContext<ApplicationDbContext>((context, services) =>
    {
        var env = services.GetRequiredService<IWebHostEnvironment>();
        var logger = services.GetRequiredService<ILogger<ApplicationDbContextSeed>>();
        var settings = services.GetRequiredService<IOptions<AppSettings>>();

        new ApplicationDbContextSeed()
            .SeedAsync(context, env, logger, settings)
            .Wait();
    });

    Log.Information("Starting web host ({ApplicationContext})...", AppName);
    app.Run();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Program terminated unexpectedly ({ApplicationContext})!", AppName);
    return 1;
}
finally
{
    Log.CloseAndFlush();
}