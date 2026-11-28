using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using HuyetMach175.Api.Data;
using HuyetMach175.SharedKernel.Authorization;
using HuyetMach175.SharedKernel.Services;

using HuyetMach175.Modules.Auth;
using HuyetMach175.Modules.Clinical;
using HuyetMach175.Modules.Donation;
using HuyetMach175.Modules.Inventory;
using HuyetMach175.Modules.Notifications;

using Scalar.AspNetCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddClinicalModule();
builder.Services.AddDonationModule();
builder.Services.AddInventoryModule();
builder.Services.AddNotificationsModule(builder.Configuration);
builder.Services.AddPermissionAuthorization();

var configuredOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Select(o => o.Trim().TrimEnd('/'))
    .Where(o => !string.IsNullOrWhiteSpace(o))
    .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

var allowLocalhostOrigins = builder.Environment.IsDevelopment() ||
    builder.Configuration.GetValue<bool>("Cors:AllowLocalhostOrigins");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrWhiteSpace(origin))
            {
                return false;
            }

            var normalizedOrigin = origin.Trim().TrimEnd('/');
            if (configuredOrigins.Contains(normalizedOrigin))
            {
                return true;
            }

            if (allowLocalhostOrigins && Uri.TryCreate(normalizedOrigin, UriKind.Absolute, out var uri))
            {
                var isHttpOrHttps = uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
                var isLocalhost = string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase);
                return isHttpOrHttps && isLocalhost;
            }

            return false;
        })
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(o =>
    {
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var errors = ctx.ModelState
                .Where(e => e.Key.Length > 0 && e.Value?.Errors.Count > 0)
                .GroupBy(e => char.ToLowerInvariant(e.Key[0]) + e.Key[1..].Split('[')[0])
                .ToDictionary(g => g.Key, g => g.First().Value!.Errors[0].ErrorMessage);

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
            {
                statusCode = 400,
                message = "Dữ liệu không hợp lệ",
                errors,
                timestamp = DateTime.UtcNow
            });
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Nhập chuỗi JWT Access Token vào đây để xác thực API."
        };
        if (document.Components != null && document.Components.SecuritySchemes != null)
        {
            document.Components.SecuritySchemes["Bearer"] = scheme;
        }
        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var hasAuthorize = metadata.OfType<IAuthorizeData>().Any();
        var allowAnonymous = metadata.OfType<IAllowAnonymous>().Any();

        if (hasAuthorize && !allowAnonymous)
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();
            var req = new OpenApiSecurityRequirement();
            var schemeRef = new OpenApiSecuritySchemeReference("Bearer");
            req.Add(schemeRef, new List<string>());
            operation.Security.Add(req);
        }
        return Task.CompletedTask;
    });
});

builder.Services.AddExceptionHandler<HuyetMach175.SharedKernel.Middlewares.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("Huyết Mạch 175 - API Documentation")
           .WithTheme(ScalarTheme.Purple)
           .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Axios);
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Lỗi xảy ra trong quá trình Migrate Database.");
    }

    try
    {
        await DbInitializer.SeedAsync(dbContext, passwordHasher);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Lỗi xảy ra trong quá trình Seed Database.");
    }
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
