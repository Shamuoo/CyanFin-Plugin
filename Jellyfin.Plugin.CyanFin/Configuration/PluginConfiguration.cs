using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CyanFin.Configuration;

/// <summary>
/// Plugin configuration stored in Jellyfin's plugin config directory.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>URL of the CyanFin server to push notifications to.</summary>
    public string CyanFinServerUrl { get; set; } = string.Empty;

    /// <summary>Shared secret for CyanFin webhook authentication.</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>Enable trickplay thumbnail generation.</summary>
    public bool EnableTrickplay { get; set; } = true;

    /// <summary>Trickplay image width in pixels.</summary>
    public int TrickplayWidth { get; set; } = 320;

    /// <summary>Trickplay interval in seconds between thumbnails.</summary>
    public int TrickplayInterval { get; set; } = 10;

    /// <summary>Enable watch party session sync.</summary>
    public bool EnableWatchParty { get; set; } = true;

    /// <summary>Enable push notifications to CyanFin.</summary>
    public bool EnableNotifications { get; set; } = true;

    /// <summary>Enable custom user metadata (ratings, notes).</summary>
    public bool EnableCustomMetadata { get; set; } = true;

    /// <summary>Maximum concurrent watch party sessions.</summary>
    public int MaxWatchPartySessions { get; set; } = 10;
}
