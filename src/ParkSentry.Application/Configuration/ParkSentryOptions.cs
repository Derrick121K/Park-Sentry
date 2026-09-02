namespace ParkSentry.Application.Configuration;

public enum IntegrationMode
{
    Demo = 0,
    Production = 1
}

public sealed class IntegrationsOptions
{
    public const string SectionName = "Integrations";

    /// <summary>Demo allows mock/demo providers. Production refuses silent mock fallback.</summary>
    public IntegrationMode Mode { get; set; } = IntegrationMode.Demo;

    /// <summary>
    /// Dangerous escape hatch: allow demo/mock providers even when Mode=Production.
    /// Never enable in a real customer deployment.
    /// </summary>
    public bool AllowDemoProviders { get; set; }
}

public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";

    /// <summary>Mock | Manual | (future gateway names)</summary>
    public string Provider { get; set; } = "Mock";

    /// <summary>Optional webhook HMAC secret for future gateway callbacks.</summary>
    public string? WebhookSecret { get; set; }
}

public sealed class ScanningOptions
{
    public const string SectionName = "Scanning";

    /// <summary>Demo | Browser | Manual | (future OCR provider names)</summary>
    public string Provider { get; set; } = "Demo";

    /// <summary>Permanently store licence-disc images. Default false.</summary>
    public bool RetainImages { get; set; }

    public string? OcrApiKey { get; set; }
    public string? OcrEndpoint { get; set; }
}

public sealed class ApplicationCorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];
}

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>Only honored when host environment is Development.</summary>
    public bool EnableDevelopmentSeed { get; set; } = true;
}
