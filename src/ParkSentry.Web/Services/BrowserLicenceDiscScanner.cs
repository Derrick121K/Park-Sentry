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
/// Browser camera scanner — demo mode only. Does not fabricate OCR results.
/// </summary>
public class BrowserLicenceDiscScanner(ICameraScannerInterop camera) : ILicenceDiscScanner
{
    public string ProviderName => "DEMO / BROWSER CAMERA";
    public bool IsDemo => true;

    public async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var capture = await camera.CaptureDemoAsync(cancellationToken);
        return MapCapture(capture);
    }

    public ScanResult ParseManualInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new ScanResult(false, null, null, null, "Registration number is required.", ProviderName);

        var normalized = RegistrationNormalizer.Normalize(input);
        return new ScanResult(true, normalized, null, 1.0, null, ProviderName, input.Trim());
    }

    private ScanResult MapCapture(JsCaptureResult capture) =>
        new(capture.Success, capture.RegistrationNumber, null, capture.Confidence,
            capture.ErrorMessage, capture.Provider ?? ProviderName, capture.RawDetectedText);
}
