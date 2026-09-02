using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ParkSentry.Application.Interfaces.Scanning;
using ParkSentry.Domain.Helpers;

namespace ParkSentry.Web.Services;

public interface ICameraScannerInterop
{
    Task<bool> IsSecureContextAsync(CancellationToken cancellationToken = default);
    Task<bool> HasMediaDevicesAsync(CancellationToken cancellationToken = default);
    Task StartCameraAsync(ElementReference videoElement, string facingMode = "environment", CancellationToken cancellationToken = default);
    Task StopCameraAsync(CancellationToken cancellationToken = default);
    Task SwitchCameraAsync(CancellationToken cancellationToken = default);
    Task<JsCaptureResult> CaptureDemoAsync(CancellationToken cancellationToken = default);
    Task CleanupAsync(CancellationToken cancellationToken = default);
}

public record JsCaptureResult(
    bool Success,
    string? RegistrationNumber,
    string? RawDetectedText,
    double? Confidence,
    string? ErrorMessage,
    string Provider);

public class CameraScannerInterop(IJSRuntime js) : ICameraScannerInterop
{
    public Task<bool> IsSecureContextAsync(CancellationToken cancellationToken = default) =>
        js.InvokeAsync<bool>("eval", "ParkSentryCamera.isSecureContext()", cancellationToken).AsTask();

    public Task<bool> HasMediaDevicesAsync(CancellationToken cancellationToken = default) =>
        js.InvokeAsync<bool>("eval", "ParkSentryCamera.hasMediaDevices()", cancellationToken).AsTask();

    public Task StartCameraAsync(ElementReference videoElement, string facingMode = "environment", CancellationToken cancellationToken = default) =>
        js.InvokeVoidAsync("ParkSentryCamera.start", videoElement, facingMode, cancellationToken).AsTask();

    public Task StopCameraAsync(CancellationToken cancellationToken = default) =>
        js.InvokeVoidAsync("ParkSentryCamera.stop", cancellationToken).AsTask();

    public Task SwitchCameraAsync(CancellationToken cancellationToken = default) =>
        js.InvokeVoidAsync("ParkSentryCamera.switchCamera", cancellationToken).AsTask();

    public Task<JsCaptureResult> CaptureDemoAsync(CancellationToken cancellationToken = default) =>
        js.InvokeAsync<JsCaptureResult>("ParkSentryCamera.captureDemo", cancellationToken).AsTask();

    public Task CleanupAsync(CancellationToken cancellationToken = default) =>
        js.InvokeVoidAsync("ParkSentryCamera.cleanup", cancellationToken).AsTask();
}

/// <summary>
/// Browser camera capture — not OCR. Capture confirms image acquisition only; registration must be entered manually unless a future OCR provider is configured.
/// </summary>
public class BrowserLicenceDiscScanner(ICameraScannerInterop camera) : ILicenceDiscScanner
{
    public string ProviderName => "BROWSER CAMERA CAPTURE";
    public bool IsDemo => true;
    public bool SupportsOcr => false;

    public Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default) =>
        ScanAsync(new ScanRequest(), cancellationToken);

    public async Task<ScanResult> ScanAsync(ScanRequest request, CancellationToken cancellationToken = default)
    {
        var capture = await camera.CaptureDemoAsync(cancellationToken);
        return MapCapture(capture);
    }

    public ScanResult ParseManualInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new ScanResult(false, null, null, null, "Registration number is required.", ProviderName,
                Status: ScanStatus.Failed, Error: ScanError.NoRegistrationDetected,
                ScanProvider: ScanProvider.Manual, IsOcrResult: false);

        var normalized = RegistrationNormalizer.Normalize(input);
        return new ScanResult(true, normalized, null, 1.0, null, ProviderName, input.Trim(),
            Status: ScanStatus.ManualFallback, Error: ScanError.None,
            ScanId: Guid.NewGuid(), TimestampUtc: DateTime.UtcNow,
            ScanProvider: ScanProvider.Manual, IsOcrResult: false);
    }

    private ScanResult MapCapture(JsCaptureResult capture) =>
        new(capture.Success, capture.RegistrationNumber, null, capture.Confidence,
            capture.ErrorMessage, capture.Provider ?? ProviderName, capture.RawDetectedText,
            Status: capture.Success ? ScanStatus.Captured : ScanStatus.Failed,
            Error: capture.Success ? ScanError.None : ScanError.CaptureFailed,
            ScanId: Guid.NewGuid(),
            TimestampUtc: DateTime.UtcNow,
            Warnings: ["Browser capture is not OCR. Enter or confirm the registration manually."],
            ScanProvider: ScanProvider.Browser,
            IsOcrResult: false);
}
