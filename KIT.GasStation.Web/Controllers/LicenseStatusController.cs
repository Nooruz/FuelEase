using KIT.GasStation.Licensing.Core;
using Microsoft.AspNetCore.Mvc;

namespace KIT.GasStation.Web.Controllers;

/// <summary>
/// Эндпоинт для проверки состояния лицензии.
/// Используется для мониторинга и диагностики.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LicenseStatusController : ControllerBase
{
    private readonly LicenseGuardService _licenseGuard;

    public LicenseStatusController(LicenseGuardService licenseGuard)
    {
        _licenseGuard = licenseGuard;
    }

    /// <summary>
    /// Возвращает текущее состояние лицензии.
    /// GET /api/licensestatus
    /// </summary>
    [HttpGet]
    public IActionResult GetStatus()
    {
        var grace = _licenseGuard.GraceRemaining;

        return Ok(new
        {
            IsValid  = _licenseGuard.IsLicenseValid,
            Status   = _licenseGuard.CurrentStatus.ToString(),
            GraceRemaining = grace.HasValue
                ? $"{grace.Value.Days}д {grace.Value.Hours}ч {grace.Value.Minutes}мин"
                : null,
            CheckedAt = DateTime.UtcNow
        });
    }
}
