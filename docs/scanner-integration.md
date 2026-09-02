# Scanner Integration

## Interface

```csharp
public interface ILicenceDiscScanner
{
    string ProviderName { get; }
    bool IsDemo { get; }
    Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default);
    ScanResult ParseManualInput(string input);
}
```

## Current Implementations

| Implementation | Context |
|----------------|---------|
| `DemoLicenceDiscScanner` | API / server-only paths |
| `BrowserLicenceDiscScanner` | Web guard UI (camera + manual) |

### BrowserLicenceDiscScanner (Build 2B)

- Uses `camera-scanner.js` via `ICameraScannerInterop` / `IJSRuntime`
- Returns `ScanResult` with success/failure, normalized registration, raw text, confidence, provider, and error reason
- Demo capture does **not** fabricate OCR — returns failure with guidance to enter manually
- `ParseManualInput` normalizes registration for manual fallback

### DemoLicenceDiscScanner

- No camera OCR
- Manual registration input only
- Used where browser interop is unavailable (API)

## Scan Result

```csharp
public record ScanResult(
    bool Success,
    string? RegistrationNumber,
    string? LicenceDiscNumber,
    double? Confidence,
    string? ErrorMessage,
    string Provider,
    string? RawDetectedText = null);
```

Never report success if the scan did not actually succeed.

See [mobile-scanning.md](mobile-scanning.md) for camera architecture and browser requirements.
## Capture vs OCR

Browser capture confirms an image was acquired. It does **not** perform OCR.
ScanResult.IsOcrResult must remain false unless a real OCR provider is configured.
Image retention defaults to off (Scanning:RetainImages=false).
