namespace ParkSentry.Application.Interfaces.Scanning;

public enum ScanProvider
{
    Demo = 0,
    Browser = 1,
    Manual = 2,
    ExternalHardware = 3,
    CommercialOcr = 4
}

public enum ScanStatus
{
    Pending = 0,
    Captured = 1,
    Extracted = 2,
    Confirmed = 3,
    Failed = 4,
    ManualFallback = 5
}

public enum ScanError
{
    None = 0,
    CameraDenied = 1,
    CaptureFailed = 2,
    LowImageQuality = 3,
    NoRegistrationDetected = 4,
    ProviderUnavailable = 5,
    OcrNotConfigured = 6,
    Cancelled = 7,
    Unknown = 99
}

public record ScanRequest(
    Guid? OrganizationId = null,
    Guid? SiteId = null,
    Guid? DeviceId = null,
    byte[]? ImageBytes = null,
    string? ContentType = null,
    bool AllowManualFallback = true,
    bool RetainImage = false);

public record ExtractedRegistration(
    string? RawText,
    string? NormalizedRegistration,
    double? Confidence);

public record ScanResult(
    bool Success,
    string? RegistrationNumber,
    string? LicenceDiscNumber,
    double? Confidence,
    string? ErrorMessage,
    string Provider = "Demo",
    string? RawDetectedText = null,
    ScanStatus Status = ScanStatus.Failed,
    ScanError Error = ScanError.None,
    Guid? ScanId = null,
    DateTime? TimestampUtc = null,
    IReadOnlyList<string>? Warnings = null,
    string? ImageReference = null,
    ScanProvider ScanProvider = ScanProvider.Demo,
    bool IsOcrResult = false);

public interface ILicenceDiscScanner
{
    string ProviderName { get; }
    bool IsDemo { get; }
    bool SupportsOcr { get; }
    Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default);
    Task<ScanResult> ScanAsync(ScanRequest request, CancellationToken cancellationToken = default);
    ScanResult ParseManualInput(string input);
}
