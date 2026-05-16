# CyanFin Plugin for Jellyfin

> ⚠️ AI-Generated — built with Claude by Anthropic.

A Jellyfin server plugin providing deep integration with the [CyanFin](https://github.com/Shamuoo/CyanFin-React) frontend. Adds features that aren't possible through the standard Jellyfin API alone.

---

## Features

### 🎬 Trickplay Thumbnails
Generates scrubber preview thumbnails (sprite sheets) for the CyanFin player. When you hover or scrub the progress bar, you see a small preview of that moment in the video.

- Uses ffmpeg to extract frames at configurable intervals (default: every 10s)
- Tiles into a single sprite sheet JPEG for efficient delivery
- Generated on first request, cached until invalidated
- `GET /CyanFin/Trickplay/{itemId}` → metadata
- `GET /CyanFin/Trickplay/{itemId}/sprite` → JPEG sprite sheet

### 🎉 Watch Party
Server-side session sync for watching together with friends and family.

- Create a session tied to any library item
- Host controls playback (play/pause/seek), others follow
- 3-second sync tolerance to avoid constant seeking
- `POST /CyanFin/WatchParty/Create` → create session
- `POST /CyanFin/WatchParty/Join` → join by sessionId
- `POST /CyanFin/WatchParty/Sync` → send position, get authoritative state

### 🔔 Push Notifications
Jellyfin pushes events to CyanFin in real time:
- New library items added
- Playback started / stopped
- Library scan started / complete
- Items updated or removed

CyanFin uses these to refresh the UI immediately without polling.

### ⭐ Custom Metadata
Per-user metadata stored separately from Jellyfin's library metadata:
- Personal ratings (0–10, independent of community ratings)
- Personal notes / mini reviews
- Custom tags
- Extended watch tracking

Persisted as JSON in Jellyfin's data directory.

---

## Installation

### Manual (Unraid / direct)
1. Download the latest `CyanFin-Plugin-x.x.x.zip` from [Releases](https://github.com/Shamuoo/CyanFin-Plugin/releases)
2. Extract `Jellyfin.Plugin.CyanFin.dll`
3. Copy to your Jellyfin plugins directory:
   ```
   /mnt/user/appdata/jellyfin/plugins/CyanFin/Jellyfin.Plugin.CyanFin.dll
   ```
4. Restart Jellyfin
5. Go to Dashboard → Plugins → CyanFin → configure

### Via Jellyfin Plugin Catalogue (coming soon)
A repository manifest will be published for one-click install from the Jellyfin dashboard.

---

## Configuration

In Jellyfin Dashboard → Plugins → CyanFin:

| Setting | Default | Description |
|---|---|---|
| CyanFin Server URL | — | Your CyanFin container URL (e.g. `http://192.168.1.x:3002`) |
| Webhook Secret | — | Optional shared secret for webhook auth |
| Enable Trickplay | ✓ | Generate scrubber thumbnails |
| Thumbnail Width | 320px | Width of each preview tile |
| Thumbnail Interval | 10s | Seconds between thumbnails |
| Enable Watch Party | ✓ | Server-side watch sync |
| Max Sessions | 10 | Max concurrent watch party sessions |
| Enable Notifications | ✓ | Push events to CyanFin |
| Enable Custom Metadata | ✓ | Personal ratings, notes, tags |

---

## Building from Source

Requires .NET 8 SDK.

```bash
git clone https://github.com/Shamuoo/CyanFin-Plugin.git
cd CyanFin-Plugin
dotnet build --configuration Release
# DLL is in: Jellyfin.Plugin.CyanFin/bin/Release/net8.0/Jellyfin.Plugin.CyanFin.dll
```

GitHub Actions automatically builds and publishes releases on version tags.

---

## API Reference

All endpoints under `/CyanFin/` — standard Jellyfin auth (Bearer token or API key).

```
GET  /CyanFin/Status                    Plugin capabilities (no auth)
GET  /CyanFin/Health                    Ping (no auth)

GET  /CyanFin/Trickplay/{id}            Trickplay metadata
GET  /CyanFin/Trickplay/{id}/sprite     Sprite sheet JPEG
DELETE /CyanFin/Trickplay/{id}          Invalidate cache

POST /CyanFin/WatchParty/Create         Create session
POST /CyanFin/WatchParty/Join           Join session
POST /CyanFin/WatchParty/Sync           Position sync
GET  /CyanFin/WatchParty/{id}           Get session state
GET  /CyanFin/WatchParty/Mine           My sessions
DELETE /CyanFin/WatchParty/{id}         Leave session

GET  /CyanFin/Metadata/{id}             Get user metadata
PUT  /CyanFin/Metadata/{id}             Set user metadata
PATCH /CyanFin/Metadata/{id}/rating     Set personal rating
PATCH /CyanFin/Metadata/{id}/note       Set note
PATCH /CyanFin/Metadata/{id}/tags       Set tags
GET  /CyanFin/Metadata/TopRated         User's top-rated items
```

---

## License

GPL-3.0 — same as Jellyfin (required for plugin binary compatibility).
