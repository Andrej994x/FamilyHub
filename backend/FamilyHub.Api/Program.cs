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
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
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

    // --- Reverse-proxy / TLS-termination support ---
    // In production the API typically runs behind a proxy or load balancer that terminates
    // HTTPS. Honour the forwarded scheme and client IP so HTTPS redirection, secure-context
    // detection (required by an installed PWA) and request logging see the real values.
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // The proxy is part of the trusted deployment; clear the default allow-lists so
        // forwarded headers are honoured regardless of the proxy's internal address.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // --- CORS (environment-based) ---
    // The API is stateless and authenticated with bearer tokens (no cookies), so credentials
    // are never needed. When the PWA is served from the same origin as the API no CORS is
    // involved at all; this policy covers cross-origin callers (the dev server, or a PWA hosted
    // on a separate origin listed in Cors:AllowedOrigins).
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(CorsPolicyName, policy =>
        {
            policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                // Lets cross-origin download requests read the attachment file name.
                .WithExposedHeaders("Content-Disposition")
                // Cache preflight responses so mobile clients avoid an OPTIONS per request.
                .SetPreflightMaxAge(TimeSpan.FromHours(1));

            if (builder.Environment.IsDevelopment())
            {
                // Development: reflect any origin so the app can be opened from a phone on the
                // local network (e.g. http://192.168.x.x:5173).
                policy.SetIsOriginAllowed(_ => true);
            }
            else
            {
                // Production: only the explicitly configured PWA origin(s) are allowed.
                policy.WithOrigins(allowedOrigins);
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

    // Apply forwarded headers first so every downstream component sees the real scheme/IP.
    app.UseForwardedHeaders();

    app.UseSerilogRequestLogging();

    // Global exception handling — early so it wraps everything downstream.
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        // Development-only: apply migrations and populate sample data (no-op if already seeded).
        await DbSeeder.SeedAsync(app.Services);
    }
    else
    {
        // HSTS tells browsers to only ever use HTTPS for this origin — a prerequisite for a
        // trustworthy, installable PWA. Sent only in non-development environments.
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    // --- Serve the built PWA from the same origin as the API (when present) ---
    // Single-origin hosting means the installed PWA calls the API without CORS, and client-side
    // deep links / refreshes fall back to index.html below. When no build has been copied into
    // wwwroot (e.g. the API-only dev workflow) this is skipped and the app stays a pure API.
    var webRootPath = app.Environment.WebRootPath
        ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    var spaIndexPath = Path.Combine(webRootPath, "index.html");
    var serveSpa = File.Exists(spaIndexPath);

    if (serveSpa)
    {
        var fileProvider = new PhysicalFileProvider(webRootPath);

        // Static files need the correct MIME type for the web app manifest.
        var contentTypeProvider = new FileExtensionContentTypeProvider();
        contentTypeProvider.Mappings[".webmanifest"] = "application/manifest+json";

        var staticFileOptions = new StaticFileOptions
        {
            FileProvider = fileProvider,
            ContentTypeProvider = contentTypeProvider,
            OnPrepareResponse = ctx =>
            {
                var name = ctx.File.Name;
                var isShell =
                    name.Equals("index.html", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("sw.js", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith(".webmanifest", StringComparison.OrdinalIgnoreCase);

                // Never cache the app shell or service worker, so a new deployment is picked up
                // immediately and stale routes can't break deep links. Hashed build assets under
                // /assets are content-addressed and safe to cache for a year.
                ctx.Context.Response.Headers.CacheControl = isShell
                    ? "no-cache, no-store, must-revalidate"
                    : "public, max-age=31536000, immutable";
            },
        };

        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
        app.UseStaticFiles(staticFileOptions);
    }

    app.UseCors(CorsPolicyName);
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // SPA fallback: client-side routes (deep links / refresh) return index.html, while unknown
    // API and Swagger paths keep returning a real 404 for API clients.
    if (serveSpa)
    {
        app.MapFallback(async context =>
        {
            var path = context.Request.Path;
            if (path.StartsWithSegments("/api") || path.StartsWithSegments("/swagger"))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "text/html";
            context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            await context.Response.SendFileAsync(spaIndexPath);
        });
    }

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
