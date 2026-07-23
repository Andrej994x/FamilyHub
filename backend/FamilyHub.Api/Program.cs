using System.Text;
using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Middleware;
using FamilyHub.Api.Models;
using FamilyHub.Api.Services;
using FamilyHub.Api.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

    // --- ASP.NET Core Identity (JWT-only API, no cookies) ---
    builder.Services
        .AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

    // --- JWT authentication ---
    var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
        ?? throw new InvalidOperationException("The 'Jwt' configuration section is missing.");
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();

    // --- MVC controllers ---
    builder.Services.AddControllers();

    // --- FluentValidation ---
    builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

    // --- Application services ---
    builder.Services.Configure<InvitationSettings>(builder.Configuration.GetSection("Invitations"));

    // Email: use real SMTP when a host is configured, otherwise log the message (dev).
    builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
    var emailSettings = builder.Configuration.GetSection("Email").Get<EmailSettings>();
    if (!string.IsNullOrWhiteSpace(emailSettings?.Smtp.Host))
    {
        builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
    }
    else
    {
        builder.Services.AddScoped<IEmailSender, LoggingEmailSender>();
    }

    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IFamilyService, FamilyService>();
    builder.Services.AddScoped<IInvitationService, InvitationService>();
    builder.Services.AddScoped<IChildProfileService, ChildProfileService>();
    builder.Services.AddScoped<IFamilyTaskService, FamilyTaskService>();
    builder.Services.AddScoped<IFamilyEventService, FamilyEventService>();
    builder.Services.AddScoped<IShoppingListService, ShoppingListService>();
    builder.Services.AddScoped<IPickupScheduleService, PickupScheduleService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IFamilyDocumentService, FamilyDocumentService>();
    builder.Services.AddScoped<IVehicleService, VehicleService>();
    builder.Services.AddScoped<IPetService, PetService>();
    builder.Services.AddScoped<IHomeRecordService, HomeRecordService>();
    builder.Services.AddScoped<IWarrantyService, WarrantyService>();
    builder.Services.AddScoped<IOtherRecordService, OtherRecordService>();
    builder.Services.AddScoped<IVaultAttachmentService, VaultAttachmentService>();
    builder.Services.AddSingleton<IFamilyVaultStorage, FamilyVaultStorage>();

    // --- CORS for the React development server ---
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(CorsPolicyName, policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // Development: allow any origin so the app can be opened from a phone on the
                // local network (e.g. http://192.168.x.x:5173). The API is stateless and uses
                // bearer tokens (no cookies), so reflecting the origin is safe here.
                policy.SetIsOriginAllowed(_ => true)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
            else
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
        });
    });

    // --- Swagger / OpenAPI (with JWT bearer support) ---
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "FamilyHub API",
            Version = "v1"
        });

        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "Enter the JWT token (without the 'Bearer' prefix).",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = JwtBearerDefaults.AuthenticationScheme
            }
        };

        options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { securityScheme, Array.Empty<string>() }
        });
    });

    var app = builder.Build();

    // --- HTTP request pipeline ---
    app.UseSerilogRequestLogging();

    // Global exception handling — first so it wraps everything downstream.
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        // Development-only: apply migrations and populate sample data (no-op if already seeded).
        await DbSeeder.SeedAsync(app.Services);
    }

    app.UseHttpsRedirection();
    app.UseCors(CorsPolicyName);
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
// HostAbortedException is the expected signal raised by the EF Core design-time tools
// (e.g. `dotnet ef migrations add`); it is not an actual startup failure.
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "FamilyHub.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
