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
app.MapControllers();

app.Run();
