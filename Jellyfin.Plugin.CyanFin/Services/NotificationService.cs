using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.CyanFin.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CyanFin.Services;

/// <summary>
/// Sends webhook notifications to the CyanFin Node.js server
/// when Jellyfin events occur (new items, playback, library scans, etc.)
/// </summary>
public class NotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly HttpClient _http;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    public async Task NotifyAsync(CyanFinNotification notification)
    {
        var config = Plugin.Instance?.Configuration;
        if (config?.EnableNotifications != true) return;
        if (string.IsNullOrEmpty(config.CyanFinServerUrl)) return;

        try
        {
            var url = $"{config.CyanFinServerUrl.TrimEnd('/')}/api/plugin/notify";
            var json = JsonSerializer.Serialize(notification);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            if (!string.IsNullOrEmpty(config.WebhookSecret))
                _http.DefaultRequestHeaders.Remove("X-CyanFin-Secret");
                _http.DefaultRequestHeaders.Add("X-CyanFin-Secret", config.WebhookSecret);

            var response = await _http.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("NotificationService: CyanFin returned {Code} for {Type}",
                    response.StatusCode, notification.Type);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "NotificationService: could not reach CyanFin at {Url}",
                config?.CyanFinServerUrl);
        }
    }

    // Convenience methods
    public Task NotifyItemAdded(string itemId, string title, string type) =>
        NotifyAsync(new CyanFinNotification
        {
            Type = "item.added",
            ItemId = itemId,
            ItemTitle = title,
            ItemType = type,
        });

    public Task NotifyPlaybackStart(string itemId, string title, string userId, string username) =>
        NotifyAsync(new CyanFinNotification
        {
            Type = "playback.start",
            ItemId = itemId,
            ItemTitle = title,
            UserId = userId,
            Username = username,
        });

    public Task NotifyPlaybackStop(string itemId, string title, string userId, long positionTicks, long runtimeTicks) =>
        NotifyAsync(new CyanFinNotification
        {
            Type = "playback.stop",
            ItemId = itemId,
            ItemTitle = title,
            UserId = userId,
            Data = new() { ["positionTicks"] = positionTicks, ["runtimeTicks"] = runtimeTicks },
        });

    public Task NotifyLibraryScan(bool complete) =>
        NotifyAsync(new CyanFinNotification
        {
            Type = complete ? "library.scan.complete" : "library.scan.start",
        });
}
