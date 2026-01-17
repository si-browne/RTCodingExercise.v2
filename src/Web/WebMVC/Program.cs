using MassTransit;
using RabbitMQ.Client;

const string AppName = "WebMVC";

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
    builder.Services.AddControllers();
    builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
    builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

    builder.Services.AddHealthChecks();

    // Add HttpClient for Catalog API
    builder.Services.AddHttpClient("CatalogApi", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["CatalogApiUrl"] ?? "http://localhost:5101");
        // NEW: demo "admin" user id for auditing using defined header
        client.DefaultRequestHeaders.Add("X-User-Id", "11111111-1111-1111-1111-111111111111");
    });

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
    }

    var pathBase = builder.Configuration["PATH_BASE"];
    if (!string.IsNullOrEmpty(pathBase))
    {
        app.UsePathBase(pathBase);
    }

    app.UseStaticFiles();
    app.UseForwardedHeaders();
    app.UseRouting();

    app.MapDefaultControllerRoute();
    app.MapControllers();
    app.MapHealthChecks("/health");

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
