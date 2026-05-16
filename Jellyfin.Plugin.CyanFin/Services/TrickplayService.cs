using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.CyanFin.Models;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CyanFin.Services;

/// <summary>
/// Generates trickplay sprite sheets (contact sheets of thumbnails at regular intervals)
/// for scrubber preview in the CyanFin player.
/// </summary>
public class TrickplayService
{
    private readonly ILogger<TrickplayService> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly ConcurrentDictionary<string, TrickplayInfo> _cache = new();
    private readonly string _cacheDir;

    // Track in-progress generations
    private readonly ConcurrentDictionary<string, Task> _pending = new();

    public TrickplayService(
        ILogger<TrickplayService> logger,
        ILibraryManager libraryManager)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _cacheDir = Path.Combine(
            Path.GetTempPath(),
            "cyanfin-trickplay");
        Directory.CreateDirectory(_cacheDir);
    }

    /// <summary>
    /// Get trickplay info for an item. Triggers generation if not cached.
    /// </summary>
    public async Task<TrickplayInfo> GetOrGenerateAsync(string itemId, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(itemId, out var cached) && cached.IsReady)
            return cached;

        var config = Plugin.Instance?.Configuration;
        if (config?.EnableTrickplay != true)
            return new TrickplayInfo { ItemId = itemId, IsReady = false };

        // Return in-progress info if already generating
        if (_cache.TryGetValue(itemId, out var inProgress))
            return inProgress;

        var info = new TrickplayInfo { ItemId = itemId, IsReady = false };
        _cache[itemId] = info;

        // Fire and forget generation
        _ = GenerateAsync(itemId, info, ct);
        return info;
    }

    /// <summary>
    /// Get the sprite image bytes if ready.
    /// </summary>
    public byte[]? GetSpriteBytes(string itemId)
    {
        var path = SpriteSheetPath(itemId);
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    private string SpriteSheetPath(string itemId) =>
        Path.Combine(_cacheDir, $"{itemId}.jpg");

    private async Task GenerateAsync(string itemId, TrickplayInfo info, CancellationToken ct)
    {
        try
        {
            var item = _libraryManager.GetItemById(Guid.Parse(itemId));
            if (item == null)
            {
                _logger.LogWarning("TrickplayService: item {Id} not found", itemId);
                return;
            }

            var mediaPath = item.Path;
            if (string.IsNullOrEmpty(mediaPath) || !File.Exists(mediaPath))
            {
                _logger.LogWarning("TrickplayService: media file not found for {Id}", itemId);
                return;
            }

            var config = Plugin.Instance!.Configuration;
            var interval = config.TrickplayInterval;
            var thumbWidth = config.TrickplayWidth;
            var thumbHeight = (int)(thumbWidth * 9.0 / 16.0);
            var spriteOutput = SpriteSheetPath(itemId);
            var tempDir = Path.Combine(_cacheDir, $"tmp_{itemId}");
            Directory.CreateDirectory(tempDir);

            _logger.LogInformation("TrickplayService: generating for {Title} ({Id})", item.Name, itemId);

            // Step 1: Extract thumbnails with ffmpeg
            var ffmpegArgs = $"-i \"{mediaPath}\" " +
                $"-vf \"fps=1/{interval},scale={thumbWidth}:{thumbHeight}:force_original_aspect_ratio=decrease,pad={thumbWidth}:{thumbHeight}:(ow-iw)/2:(oh-ih)/2\" " +
                $"-q:v 5 " +
                $"\"{Path.Combine(tempDir, "thumb%04d.jpg")}\" " +
                $"-y -loglevel error";

            var exitCode = await RunProcessAsync("ffmpeg", ffmpegArgs, ct);
            if (exitCode != 0)
            {
                _logger.LogError("TrickplayService: ffmpeg failed for {Id}", itemId);
                return;
            }

            var thumbFiles = Directory.GetFiles(tempDir, "thumb*.jpg");
            var tileCount = thumbFiles.Length;
            if (tileCount == 0) return;

            // Step 2: Tile thumbnails into a sprite sheet with ffmpeg
            var tilesPerRow = (int)Math.Ceiling(Math.Sqrt(tileCount));
            var tileArgs = $"-i \"{Path.Combine(tempDir, "thumb%04d.jpg")}\" " +
                $"-vf \"tile={tilesPerRow}x{(int)Math.Ceiling((double)tileCount / tilesPerRow)}\" " +
                $"-q:v 5 \"{spriteOutput}\" -y -loglevel error";

            exitCode = await RunProcessAsync("ffmpeg", tileArgs, ct);

            // Cleanup temp
            Directory.Delete(tempDir, true);

            if (exitCode == 0 && File.Exists(spriteOutput))
            {
                info.Width = thumbWidth;
                info.Height = thumbHeight;
                info.Interval = interval;
                info.TileWidth = thumbWidth;
                info.TileHeight = thumbHeight;
                info.TilesPerRow = tilesPerRow;
                info.TileCount = tileCount;
                info.IsReady = true;
                info.SpriteUrl = $"/CyanFin/Trickplay/{itemId}/sprite";
                _logger.LogInformation("TrickplayService: complete for {Id} ({Count} tiles)", itemId, tileCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TrickplayService: error generating for {Id}", itemId);
        }
    }

    private static async Task<int> RunProcessAsync(string exe, string args, CancellationToken ct)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        process.Start();
        await process.WaitForExitAsync(ct);
        return process.ExitCode;
    }

    /// <summary>Invalidate cache for an item (e.g. after re-encode).</summary>
    public void Invalidate(string itemId)
    {
        _cache.TryRemove(itemId, out _);
        var path = SpriteSheetPath(itemId);
        if (File.Exists(path)) File.Delete(path);
    }
}
