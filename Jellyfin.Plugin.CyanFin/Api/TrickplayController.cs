using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.CyanFin.Models;
using Jellyfin.Plugin.CyanFin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.CyanFin.Api;

/// <summary>
/// Trickplay API — generates and serves thumbnail sprite sheets for scrubber preview.
/// 
/// Usage in CyanFin player:
///   GET /CyanFin/Trickplay/{itemId}       → TrickplayInfo (width, height, interval, tileCount etc.)
///   GET /CyanFin/Trickplay/{itemId}/sprite → JPEG sprite sheet image
/// </summary>
[ApiController]
[Route("CyanFin/Trickplay")]
[Authorize]
public class TrickplayController : ControllerBase
{
    private readonly TrickplayService _trickplay;

    public TrickplayController(TrickplayService trickplay)
    {
        _trickplay = trickplay;
    }

    /// <summary>
    /// GET /CyanFin/Trickplay/{itemId}
    /// Returns trickplay metadata. If not yet generated, triggers generation
    /// and returns isReady=false. Client should poll until isReady=true.
    /// </summary>
    [HttpGet("{itemId}")]
    public async Task<ActionResult<TrickplayInfo>> GetInfo(string itemId, CancellationToken ct)
    {
        if (Plugin.Instance?.Configuration.EnableTrickplay != true)
            return NotFound("Trickplay is disabled in plugin settings");

        var info = await _trickplay.GetOrGenerateAsync(itemId, ct);
        return Ok(info);
    }

    /// <summary>
    /// GET /CyanFin/Trickplay/{itemId}/sprite
    /// Returns the JPEG sprite sheet for this item.
    /// Returns 404 if not yet generated, 202 Accepted if still generating.
    /// </summary>
    [HttpGet("{itemId}/sprite")]
    public async Task<IActionResult> GetSprite(string itemId, CancellationToken ct)
    {
        if (Plugin.Instance?.Configuration.EnableTrickplay != true)
            return NotFound("Trickplay is disabled");

        var info = await _trickplay.GetOrGenerateAsync(itemId, ct);
        if (!info.IsReady)
            return StatusCode(202, new { message = "Generating trickplay, please retry", isReady = false });

        var bytes = _trickplay.GetSpriteBytes(itemId);
        if (bytes == null)
            return NotFound("Sprite sheet not found");

        return File(bytes, "image/jpeg");
    }

    /// <summary>
    /// DELETE /CyanFin/Trickplay/{itemId}
    /// Invalidate cache and regenerate on next request.
    /// </summary>
    [HttpDelete("{itemId}")]
    public IActionResult Invalidate(string itemId)
    {
        _trickplay.Invalidate(itemId);
        return Ok(new { message = "Cache invalidated" });
    }
}
