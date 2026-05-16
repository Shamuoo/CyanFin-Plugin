using System.Collections.Generic;
using System.Security.Claims;
using Jellyfin.Plugin.CyanFin.Models;
using Jellyfin.Plugin.CyanFin.Services;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CyanFin.Api;

/// <summary>
/// Watch Party API — create, join, sync and manage watch party sessions.
/// 
/// Flow:
///   POST /CyanFin/WatchParty/Create   → create session, get sessionId
///   POST /CyanFin/WatchParty/Join     → join existing session
///   POST /CyanFin/WatchParty/Sync     → send position update, get authoritative state
///   GET  /CyanFin/WatchParty/{id}     → get session state (poll)
///   DELETE /CyanFin/WatchParty/{id}   → leave / end session
/// </summary>
[ApiController]
[Route("CyanFin/WatchParty")]
[Authorize]
public class WatchPartyController : ControllerBase
{
    private readonly WatchPartyService _watchParty;
    private readonly ILibraryManager _library;
    private readonly ILogger<WatchPartyController> _logger;

    public WatchPartyController(
        WatchPartyService watchParty,
        ILibraryManager library,
        ILogger<WatchPartyController> logger)
    {
        _watchParty = watchParty;
        _library = library;
        _logger = logger;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string Username => User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

    /// <summary>Create a new watch party session.</summary>
    [HttpPost("Create")]
    public ActionResult<WatchPartySession> Create([FromBody] WatchPartyJoinRequest request)
    {
        if (Plugin.Instance?.Configuration.EnableWatchParty != true)
            return NotFound("Watch Party is disabled");

        var item = _library.GetItemById(Guid.Parse(request.ItemId));
        var title = item?.Name ?? request.ItemId;

        var session = _watchParty.Create(request.ItemId, title, UserId, Username);
        return Ok(session);
    }

    /// <summary>Join an existing session.</summary>
    [HttpPost("Join")]
    public ActionResult<WatchPartySession> Join([FromBody] WatchPartyJoinRequest request)
    {
        if (Plugin.Instance?.Configuration.EnableWatchParty != true)
            return NotFound("Watch Party is disabled");

        if (string.IsNullOrEmpty(request.SessionId))
            return BadRequest("SessionId required to join");

        var session = _watchParty.Join(request.SessionId, UserId, Username);
        if (session == null)
            return NotFound($"Session {request.SessionId} not found");

        return Ok(session);
    }

    /// <summary>
    /// Send a position/state update and get the authoritative state back.
    /// Call this every 2-5 seconds while playing.
    /// </summary>
    [HttpPost("Sync")]
    public ActionResult<WatchPartySession> Sync([FromBody] WatchPartySyncRequest request)
    {
        var session = _watchParty.Sync(request.SessionId, UserId, request.PositionTicks, request.IsPaused);
        if (session == null)
            return NotFound($"Session {request.SessionId} not found");

        return Ok(session);
    }

    /// <summary>Host only: force all members to a specific position.</summary>
    [HttpPost("{sessionId}/ForceSync")]
    public ActionResult<WatchPartySession> ForceSync(string sessionId, [FromBody] WatchPartySyncRequest request)
    {
        var session = _watchParty.ForceSync(sessionId, UserId, request.PositionTicks, request.IsPaused);
        if (session == null)
            return Forbid();

        return Ok(session);
    }

    /// <summary>Get current session state.</summary>
    [HttpGet("{sessionId}")]
    public ActionResult<WatchPartySession> GetSession(string sessionId)
    {
        var session = _watchParty.Get(sessionId);
        if (session == null) return NotFound();
        return Ok(session);
    }

    /// <summary>List all active sessions (admin view).</summary>
    [HttpGet]
    public ActionResult<IEnumerable<WatchPartySession>> ListSessions()
    {
        return Ok(_watchParty.GetAll());
    }

    /// <summary>List sessions for the current user.</summary>
    [HttpGet("Mine")]
    public ActionResult<IEnumerable<WatchPartySession>> MySessions()
    {
        return Ok(_watchParty.GetForUser(UserId));
    }

    /// <summary>Leave a session.</summary>
    [HttpDelete("{sessionId}")]
    public IActionResult Leave(string sessionId)
    {
        _watchParty.Leave(sessionId, UserId);
        return Ok(new { message = "Left session" });
    }
}
