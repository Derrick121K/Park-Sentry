# Progressive Web App (PWA)

## Overview

ParkSentry can be installed as a PWA on mobile devices for guard operations.

## Files

| File | Purpose |
|------|---------|
| `wwwroot/manifest.json` | Web app manifest |
| `wwwroot/service-worker.js` | Application shell caching |
| `wwwroot/js/pwa.js` | Service worker registration, online/offline detection |
| `wwwroot/icons/` | SVG icons (192, 512, maskable) |

## Manifest

- `display: standalone`
- `start_url: /guard` — opens guard operations
- Theme/background: `#1B2A4A` (ParkSentry primary)

## Service Worker

Caches application shell assets (CSS, manifest, icons). Does **not** cache API or SignalR traffic.

## Installation

1. Deploy over HTTPS
2. Open ParkSentry in Chrome (Android) or Safari (iOS)
3. Use browser "Add to Home Screen" / "Install app"

## Icons

SVG placeholder icons are included using ParkSentry brand colours. **Replace with production PNG assets** (`parksentrylogo.png` at 192×192 and 512×512) before production deployment.

## Connection Status

`ConnectionStatus.razor` displays ONLINE / OFFLINE state. Server operations are blocked while offline.

## Offline Behaviour

- Shell may load from cache when offline
- Vehicle entry, exit, and payment require server connectivity
- No offline queue in Build 2B (architecture prepared for future)
