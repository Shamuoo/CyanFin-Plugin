namespace Jellyfin.Plugin.CyanFin.Models;

/// <summary>Trickplay thumbnail sprite sheet info.</summary>
public class TrickplayInfo
{
    public string ItemId { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public int Interval { get; set; }  // seconds between thumbnails
    public int TileWidth { get; set; }
    public int TileHeight { get; set; }
    public int TilesPerRow { get; set; }
    public int TileCount { get; set; }
    public bool IsReady { get; set; }
    public string? SpriteUrl { get; set; }
}

/// <summary>Watch party session.</summary>
public class WatchPartySession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
    public string ItemId { get; set; } = string.Empty;
    public string ItemTitle { get; set; } = string.Empty;
    public string HostUserId { get; set; } = string.Empty;
    public string HostUsername { get; set; } = string.Empty;
    public List<WatchPartyMember> Members { get; set; } = new();
    public long PositionTicks { get; set; }
    public bool IsPaused { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>Member of a watch party session.</summary>
public class WatchPartyMember
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public long PositionTicks { get; set; }
    public bool IsSynced { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
}

/// <summary>Custom user metadata for a library item.</summary>
public class CustomItemMetadata
{
    public string ItemId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public float? PersonalRating { get; set; }     // 0-10
    public string? PersonalNote { get; set; }
    public List<string> PersonalTags { get; set; } = new();
    public DateTime? WatchedOn { get; set; }
    public int WatchCount { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

/// <summary>Notification sent to CyanFin server.</summary>
public class CyanFinNotification
{
    public string Type { get; set; } = string.Empty;  // "item.added" | "playback.start" | "playback.stop" | "library.scan"
    public string? ItemId { get; set; }
    public string? ItemTitle { get; set; }
    public string? ItemType { get; set; }
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>Plugin status response for CyanFin detection.</summary>
public class PluginStatus
{
    public string Name { get; set; } = "CyanFin Plugin";
    public string Version { get; set; } = "1.0.0";
    public bool TrickplayEnabled { get; set; }
    public bool WatchPartyEnabled { get; set; }
    public bool NotificationsEnabled { get; set; }
    public bool CustomMetadataEnabled { get; set; }
    public string CyanFinServerUrl { get; set; } = string.Empty;
}

/// <summary>Request to create/join a watch party.</summary>
public class WatchPartyJoinRequest
{
    public string ItemId { get; set; } = string.Empty;
    public string? SessionId { get; set; }  // null = create new
}

/// <summary>Watch party sync update from a client.</summary>
public class WatchPartySyncRequest
{
    public string SessionId { get; set; } = string.Empty;
    public long PositionTicks { get; set; }
    public bool IsPaused { get; set; }
}
