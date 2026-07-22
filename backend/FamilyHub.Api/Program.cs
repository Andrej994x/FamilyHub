using FamilyHub.Api.Data;
using FamilyHub.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

// Bootstrap logger so failures during startup are captured before the host is built.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

const string CorsPolicyName = "FamilyHubCorsPolicy";

try
{
    Log.Information("Starting FamilyHub.Api");

    var builder = WebApplication.CreateBuilder(args);

    // --- Serilog (reads full configuration from appsettings.json) ---
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // --- Entity Framework Core / SQL Server ---
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // --- MVC controllers ---
    builder.Services.AddControllers();

    // --- CORS for the React development server ---
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(CorsPolicyName, policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    });

    // --- Swagger / OpenAPI ---
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "FamilyHub API",
            Version = "v1"
        });
    });

    // --- Application services (registered here as the domain grows) ---

    var app = builder.Build();

    // --- HTTP request pipeline ---
    app.UseSerilogRequestLogging();

    // Global exception handling — first so it wraps everything downstream.
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseCors(CorsPolicyName);
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "FamilyHub.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
