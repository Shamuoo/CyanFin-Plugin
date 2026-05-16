using System;
using System.Collections.Generic;
using Jellyfin.Plugin.CyanFin.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CyanFin;

/// <summary>
/// CyanFin Jellyfin Plugin.
/// Provides trickplay thumbnails, watch party sync, push notifications,
/// custom metadata, and deep integration with the CyanFin frontend.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly ILogger<Plugin> _logger;

    public Plugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer,
        ILogger<Plugin> logger)
        : base(applicationPaths, xmlSerializer)
    {
        _logger = logger;
        Instance = this;
        _logger.LogInformation("CyanFin Plugin v{Version} loaded", Version);
    }

    /// <inheritdoc />
    public override string Name => "CyanFin";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    /// <inheritdoc />
    public override string Description =>
        "Deep integration between Jellyfin and the CyanFin frontend. " +
        "Provides trickplay thumbnails, watch party sync, push notifications, and custom metadata.";

    /// <summary>Global plugin instance.</summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.config.html",
            },
        };
    }
}
