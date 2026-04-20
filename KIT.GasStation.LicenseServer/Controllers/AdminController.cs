using KIT.GasStation.LicenseServer.Data;
using KIT.GasStation.LicenseServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KIT.GasStation.LicenseServer.Controllers;

/// <summary>
/// Административные эндпоинты для управления лицензиями.
/// Защищены API-ключом через заголовок X-Admin-Key (см. Program.cs middleware).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly LicenseService _licenseService;
    private readonly LicenseDbContext _db;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        LicenseService licenseService,
        LicenseDbContext db,
        ILogger<AdminController> logger)
    {
        _licenseService = licenseService;
        _db = db;
        _logger = logger;
    }

    /// <summary>Создать новую лицензию.</summary>
    [HttpPost("licenses")]
    public async Task<IActionResult> CreateLicense([FromBody] CreateLicenseRequest request)
    {
        if (string.IsNullOrEmpty(request.CustomerId) || string.IsNullOrEmpty(request.CustomerName))
            return BadRequest("CustomerId и CustomerName обязательны");

        var license = await _licenseService.CreateLicenseAsync(request);
        return Ok(new
        {
            license.LicenseId,
            license.LicenseKey,
            license.CustomerId,
            license.CustomerName,
            license.IssuedAtUtc,
            license.ExpiresAtUtc,
            license.MaxSeats
        });
    }

    /// <summary>Отозвать лицензию.</summary>
    [HttpPost("licenses/{licenseId}/revoke")]
    public async Task<IActionResult> RevokeLicense(string licenseId, [FromBody] RevokeRequest request)
    {
        var result = await _licenseService.RevokeLicenseAsync(licenseId, request.Reason);
        if (!result)
            return NotFound();

        return Ok(new { Message = "Лицензия отозвана" });
    }

    /// <summary>Список всех лицензий.</summary>
    [HttpGet("licenses")]
    public async Task<IActionResult> GetLicenses([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var licenses = await _db.Licenses
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.LicenseId,
                l.LicenseKey,
                l.CustomerId,
                l.CustomerName,
                l.Product,
                l.Tier,
                l.MaxSeats,
                l.IssuedAtUtc,
                l.ExpiresAtUtc,
                l.IsRevoked,
                ActiveDevices = _db.Activations.Count(a => a.LicenseId == l.LicenseId && a.IsActive)
            })
            .ToListAsync();

        var total = await _db.Licenses.CountAsync();

        return Ok(new { Data = licenses, Total = total, Page = page, PageSize = pageSize });
    }

    /// <summary>Детали лицензии с активациями.</summary>
    [HttpGet("licenses/{licenseId}")]
    public async Task<IActionResult> GetLicenseDetails(string licenseId)
    {
        var license = await _db.Licenses
            .FirstOrDefaultAsync(l => l.LicenseId == licenseId);

        if (license == null)
            return NotFound();

        var activations = await _db.Activations
            .Where(a => a.LicenseId == licenseId)
            .OrderByDescending(a => a.LastHeartbeatUtc)
            .ToListAsync();

        var recentEvents = await _db.SecurityEvents
            .Where(e => e.LicenseId == licenseId)
            .OrderByDescending(e => e.Timestamp)
            .Take(50)
            .ToListAsync();

        return Ok(new { License = license, Activations = activations, SecurityEvents = recentEvents });
    }

    /// <summary>Журнал событий безопасности.</summary>
    [HttpGet("security-events")]
    public async Task<IActionResult> GetSecurityEvents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? licenseId = null)
    {
        var query = _db.SecurityEvents.AsQueryable();

        if (!string.IsNullOrEmpty(licenseId))
            query = query.Where(e => e.LicenseId == licenseId);

        var events = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Деактивация конкретного устройства (например, при замене железа).</summary>
    [HttpPost("licenses/{licenseId}/deactivate")]
    public async Task<IActionResult> DeactivateDevice(string licenseId, [FromBody] DeactivateRequest request)
    {
        var activation = await _db.Activations
            .FirstOrDefaultAsync(a => a.LicenseId == licenseId &&
                (a.HardwareId == request.HardwareId || a.InstanceId == request.InstanceId));

        if (activation == null)
            return NotFound("Активация не найдена");

        activation.IsActive = false;
        await _db.SaveChangesAsync();

        _logger.LogWarning("Устройство деактивировано вручную: LicenseId={LicenseId}, Machine={Machine}",
            licenseId, activation.MachineName);

        return Ok(new { Message = $"Устройство {activation.MachineName} деактивировано" });
    }

    /// <summary>
    /// Генерация RSA ключей (утилитарный эндпоинт для первоначальной настройки).
    /// ВНИМАНИЕ: после генерации нужно обновить PrivateKey в appsettings сервера
    /// и PublicKey во всех клиентских appsettings, затем перевыпустить лицензии.
    /// </summary>
    [HttpPost("generate-keys")]
    public IActionResult GenerateKeys()
    {
        using var rsa = System.Security.Cryptography.RSA.Create(2048);

        var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        var publicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());

        return Ok(new
        {
            PrivateKey = privateKey,
            PublicKey = publicKey,
            Instructions = new[]
            {
                "1. Сохраните PrivateKey в appsettings сервера (Licensing:PrivateKey)",
                "2. PublicKey добавьте в appsettings клиентских приложений (Licensing:PublicKey)",
                "3. Удалите локальные файлы лицензий на всех устройствах (license.dat, state.dat)",
                "4. Повторно активируйте каждое устройство (через LicenseKey в appsettings клиента)"
            }
        });
    }
}

public sealed class DeactivateRequest
{
    public string HardwareId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
}

public sealed class RevokeRequest
{
    public string Reason { get; set; } = string.Empty;
}
