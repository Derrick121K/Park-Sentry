# Payment Integration

## Interface

```csharp
public interface IPaymentProvider
{
    string ProviderName { get; }
    bool IsMock { get; }
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct = default);
}
```

## Current Implementation

`MockPaymentProvider` — **MOCK PAYMENT PROVIDER**

- Always succeeds in development
- Generates `MOCK-` prefixed transaction IDs
- Clearly labeled in the guard exit UI

## Adding a Provider

1. Implement `IPaymentProvider` in `ParkSentry.Infrastructure/Payments/`
2. Register in `DependencyInjection.cs`
3. Configure API keys via environment variables

## Future Providers

- PayFast, Yoco, Ozow (South Africa)
- Peach Payments, Stripe
- EFT, card terminals, cash recording

## Payment States

`Pending` → `Successful` | `Failed` | `Cancelled` | `Refunded`

Never store raw card details. All card processing must occur at the provider.

## Safety Fees

Configured per organization:
- `SafetyFeeEnabled`, `SafetyFeeType` (Fixed/Percentage), `SafetyFeeAmount`
- Calculated by `PricingService.CalculateSafetyFee()`

## Demo vs Production

- **MockPaymentProvider**: Demo/Testing only (IsMock=true).
- **ManualPaymentProvider**: Production-safe desk/cash/card-at-desk recording (IsMock=false). Not a card gateway.
- Webhooks: IPaymentWebhookHandler is reserved for future gateways; authenticity validation is mandatory.
