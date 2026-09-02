extern alias Web;
using FluentAssertions;
using Moq;
using ParkSentry.Application.Interfaces.Notifications;
using ParkSentry.Application.Interfaces.Scanning;
using Web::ParkSentry.Web.Services;

namespace ParkSentry.UnitTests;

public class BrowserLicenceDiscScannerTests
{
    private readonly Mock<ICameraScannerInterop> _camera = new();

    [Fact]
    public void ParseManualInput_NormalizesRegistration()
    {
        var scanner = new BrowserLicenceDiscScanner(_camera.Object);
        var result = scanner.ParseManualInput("ca 123-456");
        result.Success.Should().BeTrue();
        result.RegistrationNumber.Should().Be("CA123456");
        result.Provider.Should().Contain("BROWSER");
    }

    [Fact]
    public void ParseManualInput_Empty_ReturnsError()
    {
        var scanner = new BrowserLicenceDiscScanner(_camera.Object);
        var result = scanner.ParseManualInput("  ");
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ScanAsync_DemoCapture_DoesNotFabricateOcr()
    {
        _camera.Setup(c => c.CaptureDemoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JsCaptureResult(false, null, null, null,
                "DEMO SCANNER: Licence disc OCR is not available yet.",
                "DEMO / BROWSER CAMERA"));

        var scanner = new BrowserLicenceDiscScanner(_camera.Object);
        var result = await scanner.ScanAsync();
        result.Success.Should().BeFalse();
        result.RegistrationNumber.Should().BeNull();
        result.ErrorMessage.Should().Contain("OCR");
    }

    [Fact]
    public void Scanner_IsDemo()
    {
        var scanner = new BrowserLicenceDiscScanner(_camera.Object);
        scanner.IsDemo.Should().BeTrue();
    }
}

public class SignalRParkingNotificationServiceTests
{
    [Fact]
    public async Task IdempotentExit_DoesNotSendNotification()
    {
        var hub = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<Web::ParkSentry.Web.Hubs.ParkingHub>>();
        var clients = new Mock<Microsoft.AspNetCore.SignalR.IHubClients>();
        var group = new Mock<Microsoft.AspNetCore.SignalR.IClientProxy>();
        hub.Setup(h => h.Clients).Returns(clients.Object);
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(group.Object);

        var service = new SignalRParkingNotificationService(hub.Object);
        await service.NotifyVehicleExitAsync(new VehicleExitNotification(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "ABC123", null, DateTime.UtcNow, IsIdempotentReplay: true));

        group.Verify(g => g.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IdempotentPayment_DoesNotSendNotification()
    {
        var hub = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<Web::ParkSentry.Web.Hubs.ParkingHub>>();
        var clients = new Mock<Microsoft.AspNetCore.SignalR.IHubClients>();
        var group = new Mock<Microsoft.AspNetCore.SignalR.IClientProxy>();
        hub.Setup(h => h.Clients).Returns(clients.Object);
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(group.Object);

        var service = new SignalRParkingNotificationService(hub.Object);
        await service.NotifyPaymentAsync(new PaymentNotification(
            Guid.NewGuid(), Guid.NewGuid(), 10m, "R1", IsIdempotentReplay: true));

        group.Verify(g => g.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class PwaManifestTests
{
    private static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    public void Manifest_HasRequiredFields()
    {
        var path = Path.Combine(RepoRoot(), "src", "ParkSentry.Web", "wwwroot", "manifest.json");
        var content = File.ReadAllText(path);
        content.Should().Contain("\"display\": \"standalone\"");
        content.Should().Contain("\"theme_color\"");
        content.Should().Contain("icon-192");
        content.Should().Contain("icon-512");
        content.Should().Contain("maskable");
    }

    [Fact]
    public void ServiceWorker_Exists()
    {
        var path = Path.Combine(RepoRoot(), "src", "ParkSentry.Web", "wwwroot", "service-worker.js");
        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public void CameraScannerScript_Exists()
    {
        var path = Path.Combine(RepoRoot(), "src", "ParkSentry.Web", "wwwroot", "js", "camera-scanner.js");
        var content = File.ReadAllText(path);
        content.Should().Contain("getUserMedia");
        content.Should().Contain("captureDemo");
    }
}
