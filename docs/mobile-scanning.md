# Mobile Scanning

## Overview

ParkSentry guard operations use a browser-based camera layer for licence disc scanning. Production OCR is **not** implemented in Build 2B — the demo scanner provides camera preview and manual fallback only.

## Architecture

```
Browser (camera-scanner.js)
    ↓ getUserMedia
Camera preview (HTML5 video)
    ↓ Capture (demo)
BrowserLicenceDiscScanner (ILicenceDiscScanner)
    ↓ ScanResult
ScanVehicle.razor
    ↓ normalized registration
VehicleService / ParkingSessionService
```

## Components

| Component | Purpose |
|-----------|---------|
| `wwwroot/js/camera-scanner.js` | Camera lifecycle, permissions, cleanup |
| `BrowserLicenceDiscScanner` | `ILicenceDiscScanner` implementation using JS interop |
| `CameraScanner.razor` | Reusable camera UI component |
| `ScanVehicle.razor` | Mobile-first guard entry workflow |

## Browser Requirements

- **HTTPS required** for camera access (secure context)
- `navigator.mediaDevices.getUserMedia` support
- Tested targets: Chrome/Edge desktop, Android Chrome, iOS Safari (with limitations)

## Manual Fallback

Guards can always enter registration manually. The demo scanner **never fabricates** OCR results.

## Scanner Abstraction

`ILicenceDiscScanner` is unchanged. Implementations:

| Implementation | Context |
|----------------|---------|
| `DemoLicenceDiscScanner` | API / server-only paths |
| `BrowserLicenceDiscScanner` | Web guard UI |

Build 2C will add a production OCR provider behind the same interface.

## Rendering Model

Administrative pages use **Interactive Server**. Scanner pages use **Interactive Server with `prerender: false`** and JavaScript interop for camera access. This avoids converting the entire app to WASM while keeping camera operations client-side in the browser.

## Known Limitations

- iOS Safari may restrict camera in non-standalone tabs
- Rear camera selection depends on browser `facingMode` support
- No image storage — camera frames are not persisted
