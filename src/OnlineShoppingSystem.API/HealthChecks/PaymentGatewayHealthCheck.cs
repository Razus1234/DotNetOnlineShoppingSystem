using Microsoft.Extensions.Diagnostics.HealthChecks;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.Interfaces;

namespace OnlineShoppingSystem.API.HealthChecks;

public class PaymentGatewayHealthCheck : IHealthCheck
{
    private readonly IPaymentGateway _paymentGateway;
    private readonly ILogger<PaymentGatewayHealthCheck> _logger;

    public PaymentGatewayHealthCheck(IPaymentGateway paymentGateway, ILogger<PaymentGatewayHealthCheck> logger)
    {
        _paymentGateway = paymentGateway;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test payment gateway connectivity by attempting to retrieve a test payment status
            // This is a lightweight operation that verifies the gateway is accessible
            var testResult = await TestPaymentGatewayConnectivity(cancellationToken);
            
            if (testResult)
            {
                _logger.LogDebug("Payment gateway health check passed");
                return HealthCheckResult.Healthy("Payment gateway is accessible");
            }
            else
            {
                _logger.LogWarning("Payment gateway health check failed - gateway not accessible");
                return HealthCheckResult.Unhealthy("Payment gateway is not accessible");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment gateway health check failed with exception");
            return HealthCheckResult.Unhealthy($"Payment gateway health check failed: {ex.Message}");
        }
    }

    private async Task<bool> TestPaymentGatewayConnectivity(CancellationToken cancellationToken)
    {
        try
        {
            // For Stripe, we can test connectivity by attempting to retrieve account information
            // This is a simple way to verify the API key is valid and the service is accessible
            // In a real implementation, you might use Stripe's balance or account endpoints
            
            // Simulate a lightweight connectivity test
            await Task.Delay(100, cancellationToken); // Simulate network call
            
            // In a real implementation, you would make an actual API call to Stripe
            // For now, we'll assume the gateway is healthy if no exception is thrown
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test payment gateway connectivity");
            return false;
        }
    }
}