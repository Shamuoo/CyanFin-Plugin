using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Jellyfin.Plugin.CyanFin.Models;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CyanFin.Services;

/// <summary>
/// Stores custom per-user metadata for library items:
/// personal ratings (0-10), notes, custom tags, extended watch tracking.
/// Persisted as JSON to the Jellyfin data directory.
/// </summary>
public class CustomMetadataService
{
    private readonly ILogger<CustomMetadataService> _logger;
    private readonly string _dataPath;
    private readonly ConcurrentDictionary<string, CustomItemMetadata> _data = new();
    private static readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = true };

    public CustomMetadataService(
        ILogger<CustomMetadataService> logger,
        IApplicationPaths applicationPaths)
    {
        _logger = logger;
        _dataPath = Path.Combine(applicationPaths.DataPath, "cyanfin-metadata.json");
        Load();
    }

    private string MakeKey(string itemId, string userId) => $"{itemId}:{userId}";

    /// <summary>Get custom metadata for a user+item pair.</summary>
    public CustomItemMetadata Get(string itemId, string userId)
    {
        var key = MakeKey(itemId, userId);
        if (_data.TryGetValue(key, out var meta)) return meta;
        return new CustomItemMetadata { ItemId = itemId, UserId = userId };
    }

    /// <summary>Set custom metadata. Persists to disk.</summary>
    public CustomItemMetadata Set(CustomItemMetadata meta)
    {
        meta.LastModified = DateTime.UtcNow;
        var key = MakeKey(meta.ItemId, meta.UserId);
        _data[key] = meta;
        Save();
        return meta;
    }

    /// <summary>Set just the personal rating.</summary>
    public CustomItemMetadata SetRating(string itemId, string userId, float? rating)
    {
        var meta = Get(itemId, userId);
        meta.PersonalRating = rating;
        return Set(meta);
    }

    /// <summary>Set a personal note.</summary>
    public CustomItemMetadata SetNote(string itemId, string userId, string? note)
    {
        var meta = Get(itemId, userId);
        meta.PersonalNote = note;
        return Set(meta);
    }

    /// <summary>Add/remove personal tags.</summary>
    public CustomItemMetadata SetTags(string itemId, string userId, List<string> tags)
    {
        var meta = Get(itemId, userId);
        meta.PersonalTags = tags.Distinct().ToList();
        return Set(meta);
    }

    /// <summary>Get all items with metadata for a user (for the personal ratings page).</summary>
    public IEnumerable<CustomItemMetadata> GetAllForUser(string userId) =>
        _data.Values.Where(m => m.UserId == userId);

    /// <summary>Get top-rated items for a user.</summary>
    public IEnumerable<CustomItemMetadata> GetTopRated(string userId, int limit = 20) =>
        _data.Values
            .Where(m => m.UserId == userId && m.PersonalRating.HasValue)
            .OrderByDescending(m => m.PersonalRating)
            .Take(limit);

    private void Load()
    {
        try
        {
            if (!File.Exists(_dataPath)) return;
            var json = File.ReadAllText(_dataPath);
            var list = JsonSerializer.Deserialize<List<CustomItemMetadata>>(json);
            if (list == null) return;
            foreach (var m in list)
                _data[MakeKey(m.ItemId, m.UserId)] = m;
            _logger.LogInformation("CustomMetadataService: loaded {Count} entries", list.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CustomMetadataService: failed to load from {Path}", _dataPath);
        }
    }

    private void Save()
    {
        try
        {
            var list = _data.Values.ToList();
            var json = JsonSerializer.Serialize(list, _jsonOpts);
            File.WriteAllText(_dataPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CustomMetadataService: failed to save");
        }
    }
}
