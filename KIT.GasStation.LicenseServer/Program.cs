using KIT.GasStation.LicenseServer.Data;
using KIT.GasStation.LicenseServer.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/license-server-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 90)
    .CreateLogger();

builder.Host.UseSerilog();

// Database
builder.Services.AddDbContext<LicenseDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("LicenseDb")
                      ?? "Data Source=licenses.db"));

// Services
builder.Services.AddScoped<LicenseService>();

// API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "KIT-AZS License Server", Version = "v1" });
    c.AddSecurityDefinition("ApiKey", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-Admin-Key",
        Description = "Admin API key (required for /api/admin/* endpoints)"
    });
});

var app = builder.Build();

// Auto-migrate DB
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LicenseDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseSerilogRequestLogging();

// Middleware: защита Admin-эндпоинтов API-ключом
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/admin"))
    {
        var adminKey = builder.Configuration["Licensing:AdminApiKey"];

        // Если ключ не настроен — заблокировать всё
        if (string.IsNullOrEmpty(adminKey))
        {
            context.Response.StatusCode = 503;
            await context.Response.WriteAsync("Admin API key not configured on server.");
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Admin-Key", out var incomingKey) ||
            !string.Equals(incomingKey, adminKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized: invalid or missing X-Admin-Key header.");
            return;
        }
    }

    await next();
});

app.MapControllers();

app.Run();
