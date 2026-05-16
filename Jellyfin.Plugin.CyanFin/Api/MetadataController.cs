using System.Collections.Generic;
using System.Security.Claims;
using Jellyfin.Plugin.CyanFin.Models;
using Jellyfin.Plugin.CyanFin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.CyanFin.Api;

/// <summary>
/// Custom Metadata API — personal ratings, notes and tags per user.
/// 
///   GET    /CyanFin/Metadata/{itemId}          → get metadata for current user
///   PUT    /CyanFin/Metadata/{itemId}          → set full metadata
///   PATCH  /CyanFin/Metadata/{itemId}/rating   → set just rating
///   PATCH  /CyanFin/Metadata/{itemId}/note     → set just note
///   GET    /CyanFin/Metadata/TopRated          → user's top-rated items
/// </summary>
[ApiController]
[Route("CyanFin/Metadata")]
[Authorize]
public class MetadataController : ControllerBase
{
    private readonly CustomMetadataService _metadata;

    public MetadataController(CustomMetadataService metadata)
    {
        _metadata = metadata;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [HttpGet("{itemId}")]
    public ActionResult<CustomItemMetadata> Get(string itemId)
    {
        if (Plugin.Instance?.Configuration.EnableCustomMetadata != true)
            return NotFound("Custom metadata is disabled");
        return Ok(_metadata.Get(itemId, UserId));
    }

    [HttpPut("{itemId}")]
    public ActionResult<CustomItemMetadata> Set(string itemId, [FromBody] CustomItemMetadata meta)
    {
        if (Plugin.Instance?.Configuration.EnableCustomMetadata != true)
            return NotFound();
        meta.ItemId = itemId;
        meta.UserId = UserId;
        return Ok(_metadata.Set(meta));
    }

    [HttpPatch("{itemId}/rating")]
    public ActionResult<CustomItemMetadata> SetRating(string itemId, [FromBody] RatingRequest req)
    {
        if (Plugin.Instance?.Configuration.EnableCustomMetadata != true)
            return NotFound();
        return Ok(_metadata.SetRating(itemId, UserId, req.Rating));
    }

    [HttpPatch("{itemId}/note")]
    public ActionResult<CustomItemMetadata> SetNote(string itemId, [FromBody] NoteRequest req)
    {
        if (Plugin.Instance?.Configuration.EnableCustomMetadata != true)
            return NotFound();
        return Ok(_metadata.SetNote(itemId, UserId, req.Note));
    }

    [HttpPatch("{itemId}/tags")]
    public ActionResult<CustomItemMetadata> SetTags(string itemId, [FromBody] TagsRequest req)
    {
        if (Plugin.Instance?.Configuration.EnableCustomMetadata != true)
            return NotFound();
        return Ok(_metadata.SetTags(itemId, UserId, req.Tags));
    }

    [HttpGet("TopRated")]
    public ActionResult<IEnumerable<CustomItemMetadata>> GetTopRated([FromQuery] int limit = 20)
    {
        return Ok(_metadata.GetTopRated(UserId, limit));
    }

    [HttpGet("All")]
    public ActionResult<IEnumerable<CustomItemMetadata>> GetAll()
    {
        return Ok(_metadata.GetAllForUser(UserId));
    }
}

// DTO records for patch requests
public record RatingRequest(float? Rating);
public record NoteRequest(string? Note);
public record TagsRequest(List<string> Tags);


/// <summary>
/// Notification webhook receiver — CyanFin Node server can push events back to the plugin.
/// (Bidirectional communication for future features.)
/// </summary>
[ApiController]
[Route("CyanFin/Notify")]
public class NotifyController : ControllerBase
{
    private readonly NotificationService _notifications;

    public NotifyController(NotificationService notifications)
    {
        _notifications = notifications;
    }

    /// <summary>
    /// POST /CyanFin/Notify/Received
    /// CyanFin Node server acknowledging receipt of a notification.
    /// </summary>
    [HttpPost("Received")]
    [AllowAnonymous]
    public IActionResult Received([FromBody] object payload)
    {
        // Validate webhook secret
        var config = Plugin.Instance?.Configuration;
        if (!string.IsNullOrEmpty(config?.WebhookSecret))
        {
            var secret = Request.Headers["X-CyanFin-Secret"].ToString();
            if (secret != config.WebhookSecret)
                return Unauthorized();
        }

        return Ok(new { received = true });
    }
}
