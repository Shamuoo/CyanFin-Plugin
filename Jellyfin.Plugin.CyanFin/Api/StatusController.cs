using Jellyfin.Plugin.CyanFin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.CyanFin.Api;

/// <summary>
/// Status endpoint — CyanFin calls this on startup to detect if the plugin is installed
/// and which features are enabled.
/// </summary>
[ApiController]
[Route("CyanFin")]
public class StatusController : ControllerBase
{
    /// <summary>
    /// GET /CyanFin/Status
    /// Returns plugin capabilities. No auth required so CyanFin can check before login.
    /// </summary>
    [HttpGet("Status")]
    [AllowAnonymous]
    public ActionResult<PluginStatus> GetStatus()
    {
        var config = Plugin.Instance?.Configuration;
        return Ok(new PluginStatus
        {
            Version = Plugin.Instance?.Version.ToString() ?? "1.0.0",
            TrickplayEnabled = config?.EnableTrickplay ?? false,
            WatchPartyEnabled = config?.EnableWatchParty ?? false,
            NotificationsEnabled = config?.EnableNotifications ?? false,
            CustomMetadataEnabled = config?.EnableCustomMetadata ?? false,
            CyanFinServerUrl = config?.CyanFinServerUrl ?? string.Empty,
        });
    }

    /// <summary>GET /CyanFin/Health — simple ping.</summary>
    [HttpGet("Health")]
    [AllowAnonymous]
    public ActionResult GetHealth() => Ok(new { status = "ok", plugin = "CyanFin" });
}
