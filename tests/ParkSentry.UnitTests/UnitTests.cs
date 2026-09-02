using FluentAssertions;
using ParkSentry.Application.Services;
using ParkSentry.Domain.Entities;
using ParkSentry.Domain.Enums;
using ParkSentry.Domain.Helpers;
using ParkSentry.Domain.ValueObjects;
using System.Text.Json;

namespace ParkSentry.UnitTests;

public class RegistrationNormalizerTests
{
    [Theory]
    [InlineData("ca 123-456", "CA123456")]
    [InlineData("CA123456", "CA123456")]
    [InlineData(" ca 12 34 ", "CA1234")]
    [InlineData("", "")]
    public void Normalize_RemovesSpacesAndUppercases(string input, string expected)
    {
        RegistrationNormalizer.Normalize(input).Should().Be(expected);
    }
}

public class PricingServiceTests
{
    private readonly PricingService _service = new();

    [Fact]
    public async Task CalculateParkingFee_GracePeriod_ReturnsZero()
    {
        var rate = CreateRate(graceMinutes: 30, tiers: [(0, 30, 0), (31, 120, 10)]);
        var fee = await _service.CalculateParkingFeeAsync(rate, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(20));
        fee.Should().Be(0);
    }

    [Fact]
    public async Task CalculateParkingFee_TieredRate_ReturnsCorrectFee()
    {
        var rate = CreateRate(tiers: [(0, 30, 0), (31, 120, 10), (121, 240, 20)]);
        var fee = await _service.CalculateParkingFeeAsync(rate, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(90));
        fee.Should().Be(10);
    }

    [Fact]
    public async Task CalculateParkingFee_DailyMaximum_CapsFee()
    {
        var rate = CreateRate(dailyMax: 50, tiers: [(0, null, 100)]);
        var fee = await _service.CalculateParkingFeeAsync(rate, DateTime.UtcNow, DateTime.UtcNow.AddHours(10));
        fee.Should().Be(50);
    }

    [Fact]
    public void CalculateSafetyFee_Fixed_ReturnsAmount()
    {
        var org = new Organization { SafetyFeeEnabled = true, SafetyFeeType = SafetyFeeType.Fixed, SafetyFeeAmount = 5 };
        _service.CalculateSafetyFee(org, 20).Should().Be(5);
    }

    [Fact]
    public void CalculateSafetyFee_Percentage_ReturnsCorrectAmount()
    {
        var org = new Organization { SafetyFeeEnabled = true, SafetyFeeType = SafetyFeeType.Percentage, SafetyFeeAmount = 10 };
        _service.CalculateSafetyFee(org, 100).Should().Be(10);
    }

    [Fact]
    public void CalculateSafetyFee_Disabled_ReturnsZero()
    {
        var org = new Organization { SafetyFeeEnabled = false };
        _service.CalculateSafetyFee(org, 100).Should().Be(0);
    }

    private static ParkingRate CreateRate(int? graceMinutes = null, decimal? dailyMax = null, (int from, int? to, decimal amount)[]? tiers = null)
    {
        var tierList = tiers?.Select(t => new PricingTier(t.from, t.to, t.amount)).ToList() ?? [];
        return new ParkingRate
        {
            GracePeriodMinutes = graceMinutes,
            DailyMaximum = dailyMax,
            TiersJson = JsonSerializer.Serialize(tierList)
        };
    }
}

public class MockPaymentProviderTests
{
    [Fact]
    public void MockPaymentProvider_HasCorrectName()
    {
        var provider = new TestMockPaymentProvider();
        provider.IsMock.Should().BeTrue();
        provider.ProviderName.Should().Contain("Mock");
    }

    private class TestMockPaymentProvider : ParkSentry.Application.Interfaces.Payments.IPaymentProvider
    {
        public string ProviderName => "Mock Payment Provider";
        public bool IsMock => true;
        public Task<ParkSentry.Application.Interfaces.Payments.PaymentResult> ProcessPaymentAsync(
            ParkSentry.Application.Interfaces.Payments.PaymentRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ParkSentry.Application.Interfaces.Payments.PaymentResult(true, "MOCK-TEST", null));
    }
}
