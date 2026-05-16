using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.CyanFin.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CyanFin.Services;

/// <summary>
/// Manages watch party sessions — keeps multiple CyanFin clients
/// watching the same content in sync via server-side state.
/// </summary>
public class WatchPartyService
{
    private readonly ILogger<WatchPartyService> _logger;
    private readonly ConcurrentDictionary<string, WatchPartySession> _sessions = new();
    private const int SessionTimeoutMinutes = 60;
    // Sync tolerance — don't force resync if within 3 seconds
    private const long SyncToleranceTicks = 3 * 10_000_000L;

    public WatchPartyService(ILogger<WatchPartyService> logger)
    {
        _logger = logger;
    }

    /// <summary>Create a new watch party session.</summary>
    public WatchPartySession Create(string itemId, string itemTitle, string hostUserId, string hostUsername)
    {
        CleanupExpired();
        var session = new WatchPartySession
        {
            ItemId = itemId,
            ItemTitle = itemTitle,
            HostUserId = hostUserId,
            HostUsername = hostUsername,
        };
        session.Members.Add(new WatchPartyMember
        {
            UserId = hostUserId,
            Username = hostUsername,
        });
        _sessions[session.SessionId] = session;
        _logger.LogInformation("WatchParty: {Host} created session {Id} for '{Title}'",
            hostUsername, session.SessionId, itemTitle);
        return session;
    }

    /// <summary>Join an existing session.</summary>
    public WatchPartySession? Join(string sessionId, string userId, string username)
    {
        if (!_sessions.TryGetValue(sessionId, out var session)) return null;
        if (session.Members.Any(m => m.UserId == userId)) return session;

        session.Members.Add(new WatchPartyMember { UserId = userId, Username = username });
        session.LastUpdated = DateTime.UtcNow;
        _logger.LogInformation("WatchParty: {User} joined session {Id}", username, sessionId);
        return session;
    }

    /// <summary>Leave a session. Deletes it if the host leaves.</summary>
    public void Leave(string sessionId, string userId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session)) return;
        session.Members.RemoveAll(m => m.UserId == userId);

        if (session.HostUserId == userId || session.Members.Count == 0)
        {
            _sessions.TryRemove(sessionId, out _);
            _logger.LogInformation("WatchParty: session {Id} ended", sessionId);
        }
    }

    /// <summary>
    /// Sync update from a client. Returns the authoritative state all clients should be at.
    /// Host's position is authoritative; others follow within tolerance.
    /// </summary>
    public WatchPartySession? Sync(string sessionId, string userId, long positionTicks, bool isPaused)
    {
        if (!_sessions.TryGetValue(sessionId, out var session)) return null;

        var member = session.Members.FirstOrDefault(m => m.UserId == userId);
        if (member != null)
        {
            member.PositionTicks = positionTicks;
            member.LastSeen = DateTime.UtcNow;
            member.IsSynced = Math.Abs(positionTicks - session.PositionTicks) <= SyncToleranceTicks;
        }

        // Host controls playback state
        if (userId == session.HostUserId)
        {
            session.PositionTicks = positionTicks;
            session.IsPaused = isPaused;
            session.LastUpdated = DateTime.UtcNow;
        }

        return session;
    }

    /// <summary>Get a session by ID.</summary>
    public WatchPartySession? Get(string sessionId) =>
        _sessions.TryGetValue(sessionId, out var s) ? s : null;

    /// <summary>List all active sessions.</summary>
    public IEnumerable<WatchPartySession> GetAll() => _sessions.Values;

    /// <summary>Get sessions a user is part of.</summary>
    public IEnumerable<WatchPartySession> GetForUser(string userId) =>
        _sessions.Values.Where(s => s.Members.Any(m => m.UserId == userId));

    /// <summary>Force all members to a specific position (host override).</summary>
    public WatchPartySession? ForceSync(string sessionId, string hostUserId, long positionTicks, bool isPaused)
    {
        if (!_sessions.TryGetValue(sessionId, out var session)) return null;
        if (session.HostUserId != hostUserId) return null;

        session.PositionTicks = positionTicks;
        session.IsPaused = isPaused;
        session.LastUpdated = DateTime.UtcNow;

        // Mark all members as needing resync
        foreach (var m in session.Members)
            m.IsSynced = false;

        return session;
    }

    private void CleanupExpired()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-SessionTimeoutMinutes);
        var expired = _sessions.Where(kv => kv.Value.LastUpdated < cutoff).Select(kv => kv.Key).ToList();
        foreach (var key in expired)
        {
            _sessions.TryRemove(key, out _);
            _logger.LogInformation("WatchParty: expired session {Id}", key);
        }
    }
}
