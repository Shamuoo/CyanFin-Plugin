using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.CyanFin.Services;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CyanFin.Services;

/// <summary>
/// Background service that hooks into Jellyfin's event system
/// and forwards relevant events to the NotificationService.
/// </summary>
public class EventListenerService : IHostedService, IDisposable
{
    private readonly ILogger<EventListenerService> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly ISessionManager _sessionManager;
    private readonly NotificationService _notifications;

    public EventListenerService(
        ILogger<EventListenerService> logger,
        ILibraryManager libraryManager,
        ISessionManager sessionManager,
        NotificationService notifications)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _sessionManager = sessionManager;
        _notifications = notifications;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CyanFin EventListener: started");

        // Library events
        _libraryManager.ItemAdded += OnItemAdded;
        _libraryManager.ItemUpdated += OnItemUpdated;
        _libraryManager.ItemRemoved += OnItemRemoved;

        // Playback events
        _sessionManager.PlaybackStart += OnPlaybackStart;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CyanFin EventListener: stopped");

        _libraryManager.ItemAdded -= OnItemAdded;
        _libraryManager.ItemUpdated -= OnItemUpdated;
        _libraryManager.ItemRemoved -= OnItemRemoved;
        _sessionManager.PlaybackStart -= OnPlaybackStart;
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;

        return Task.CompletedTask;
    }

    private void OnItemAdded(object? sender, ItemChangeEventArgs e)
    {
        var item = e.Item;
        // Only notify for leaf items (movies, episodes) not folders/seasons
        if (item.IsFolder) return;

        _ = _notifications.NotifyItemAdded(
            item.Id.ToString(),
            item.Name,
            item.GetType().Name);
    }

    private void OnItemUpdated(object? sender, ItemChangeEventArgs e)
    {
        _ = _notifications.NotifyAsync(new Models.CyanFinNotification
        {
            Type = "item.updated",
            ItemId = e.Item.Id.ToString(),
            ItemTitle = e.Item.Name,
            ItemType = e.Item.GetType().Name,
        });
    }

    private void OnItemRemoved(object? sender, ItemChangeEventArgs e)
    {
        _ = _notifications.NotifyAsync(new Models.CyanFinNotification
        {
            Type = "item.removed",
            ItemId = e.Item.Id.ToString(),
            ItemTitle = e.Item.Name,
        });
    }

    private void OnPlaybackStart(object? sender, PlaybackProgressEventArgs e)
    {
        if (e.Item == null || e.Session == null) return;
        _ = _notifications.NotifyPlaybackStart(
            e.Item.Id.ToString(),
            e.Item.Name,
            e.Session.UserId.ToString(),
            e.Session.UserName ?? "Unknown");
    }

    private void OnPlaybackStopped(object? sender, PlaybackStopEventArgs e)
    {
        if (e.Item == null || e.Session == null) return;
        _ = _notifications.NotifyPlaybackStop(
            e.Item.Id.ToString(),
            e.Item.Name,
            e.Session.UserId.ToString(),
            e.PlaybackPositionTicks ?? 0,
            e.Item.RunTimeTicks ?? 0);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
