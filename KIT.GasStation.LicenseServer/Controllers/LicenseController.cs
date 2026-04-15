using KIT.GasStation.Licensing.Client;
using KIT.GasStation.LicenseServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace KIT.GasStation.LicenseServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LicenseController : ControllerBase
{
    private readonly LicenseService _licenseService;
    private readonly ILogger<LicenseController> _logger;

    public LicenseController(LicenseService licenseService, ILogger<LicenseController> logger)
    {
        _licenseService = licenseService;
        _logger = logger;
    }

    /// <summary>Активация лицензии на устройстве.</summary>
    [HttpPost("activate")]
    public async Task<IActionResult> Activate([FromBody] ActivationRequest request)
    {
        if (string.IsNullOrEmpty(request.LicenseKey) || string.IsNullOrEmpty(request.HardwareId))
            return BadRequest("LicenseKey и HardwareId обязательны");

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _licenseService.ActivateAsync(request, ip);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>Heartbeat / обновление lease.</summary>
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest request)
    {
        if (string.IsNullOrEmpty(request.LicenseId) || string.IsNullOrEmpty(request.InstanceId))
            return BadRequest("LicenseId и InstanceId обязательны");

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _licenseService.HeartbeatAsync(request, ip);

        if (!result.Success)
        {
            if (result.Revoked || result.CloneDetected)
                return StatusCode(403, result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>Проверка статуса лицензии.</summary>
    [HttpGet("status/{licenseId}")]
    public async Task<IActionResult> GetStatus(string licenseId)
    {
        var result = await _licenseService.GetStatusAsync(licenseId);
        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
