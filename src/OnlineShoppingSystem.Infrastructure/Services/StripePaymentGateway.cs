using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnlineShoppingSystem.Application.Interfaces;
using Stripe;

namespace OnlineShoppingSystem.Infrastructure.Services;

public class StripePaymentGateway : IPaymentGateway
{
    private readonly ILogger<StripePaymentGateway> _logger;
    private readonly PaymentIntentService _paymentIntentService;
    private readonly RefundService _refundService;

    public StripePaymentGateway(IConfiguration configuration, ILogger<StripePaymentGateway> logger)
    {
        _logger = logger;
        
        var secretKey = configuration["Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe SecretKey not configured");
        StripeConfiguration.ApiKey = secretKey;
        
        _paymentIntentService = new PaymentIntentService();
        _refundService = new RefundService();
    }

    public async Task<PaymentGatewayResult> ProcessPaymentAsync(PaymentGatewayRequest request)
    {
        try
        {
            _logger.LogInformation("Processing payment for amount {Amount} {Currency}", request.Amount, request.Currency);

            var options = new PaymentIntentCreateOptions
            {
                Amount = ConvertToStripeAmount(request.Amount, request.Currency),
                Currency = request.Currency.ToLowerInvariant(),
                PaymentMethod = request.PaymentToken,
                ConfirmationMethod = "manual",
                Confirm = true,
                ReturnUrl = "https://your-website.com/return", // This would be configurable
                Metadata = request.Metadata
            };

            var paymentIntent = await _paymentIntentService.CreateAsync(options);

            var result = new PaymentGatewayResult
            {
                TransactionId = paymentIntent.Id,
                Amount = ConvertFromStripeAmount(paymentIntent.Amount, paymentIntent.Currency),
                Currency = paymentIntent.Currency.ToUpperInvariant(),
                Status = MapStripeStatus(paymentIntent.Status),
                IsSuccessful = paymentIntent.Status == "succeeded",
                Metadata = paymentIntent.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>()
            };

            if (!result.IsSuccessful && paymentIntent.LastPaymentError != null)
            {
                result.ErrorMessage = paymentIntent.LastPaymentError.Message;
                result.ErrorCode = paymentIntent.LastPaymentError.Code;
            }

            _logger.LogInformation("Payment processing completed. TransactionId: {TransactionId}, Status: {Status}", 
                result.TransactionId, result.Status);

            return result;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe payment processing failed: {Message}", ex.Message);
            
            return new PaymentGatewayResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                ErrorCode = ex.StripeError?.Code,
                Amount = request.Amount,
                Currency = request.Currency
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during payment processing");
            
            return new PaymentGatewayResult
            {
                IsSuccessful = false,
                ErrorMessage = "An unexpected error occurred during payment processing",
                Amount = request.Amount,
                Currency = request.Currency
            };
        }
    }

    public async Task<PaymentGatewayResult> RefundPaymentAsync(string transactionId, decimal amount)
    {
        try
        {
            _logger.LogInformation("Processing refund for transaction {TransactionId}, amount {Amount}", transactionId, amount);

            var options = new RefundCreateOptions
            {
                PaymentIntent = transactionId,
                Amount = ConvertToStripeAmount(amount, "USD") // Currency should be determined from original payment
            };

            var refund = await _refundService.CreateAsync(options);

            var result = new PaymentGatewayResult
            {
                TransactionId = refund.Id,
                Amount = ConvertFromStripeAmount(refund.Amount, refund.Currency),
                Currency = refund.Currency.ToUpperInvariant(),
                Status = MapRefundStatus(refund.Status),
                IsSuccessful = refund.Status == "succeeded"
            };

            if (!result.IsSuccessful && refund.FailureReason != null)
            {
                result.ErrorMessage = refund.FailureReason;
            }

            _logger.LogInformation("Refund processing completed. RefundId: {RefundId}, Status: {Status}", 
                result.TransactionId, result.Status);

            return result;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe refund processing failed: {Message}", ex.Message);
            
            return new PaymentGatewayResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                ErrorCode = ex.StripeError?.Code,
                Amount = amount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during refund processing");
            
            return new PaymentGatewayResult
            {
                IsSuccessful = false,
                ErrorMessage = "An unexpected error occurred during refund processing",
                Amount = amount
            };
        }
    }

    public async Task<PaymentGatewayStatus> GetPaymentStatusAsync(string transactionId)
    {
        try
        {
            _logger.LogInformation("Retrieving payment status for transaction {TransactionId}", transactionId);

            var paymentIntent = await _paymentIntentService.GetAsync(transactionId);

            var status = new PaymentGatewayStatus
            {
                TransactionId = paymentIntent.Id,
                Status = MapStripeStatus(paymentIntent.Status),
                Amount = ConvertFromStripeAmount(paymentIntent.Amount, paymentIntent.Currency),
                Currency = paymentIntent.Currency.ToUpperInvariant(),
                ProcessedAt = paymentIntent.Created,
                Metadata = paymentIntent.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>()
            };

            return status;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to retrieve payment status: {Message}", ex.Message);
            throw new InvalidOperationException($"Failed to retrieve payment status: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving payment status");
            throw new InvalidOperationException("An unexpected error occurred while retrieving payment status", ex);
        }
    }

    private static long ConvertToStripeAmount(decimal amount, string currency)
    {
        // Stripe expects amounts in the smallest currency unit (e.g., cents for USD)
        var multiplier = GetCurrencyMultiplier(currency);
        return (long)(amount * multiplier);
    }

    private static decimal ConvertFromStripeAmount(long amount, string currency)
    {
        var multiplier = GetCurrencyMultiplier(currency);
        return amount / (decimal)multiplier;
    }

    private static int GetCurrencyMultiplier(string currency)
    {
        // Most currencies use 2 decimal places (cents)
        // Some currencies like JPY use 0 decimal places
        // This is a simplified implementation
        return currency.ToUpperInvariant() switch
        {
            "JPY" => 1,
            "KRW" => 1,
            _ => 100
        };
    }

    private static string MapStripeStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "requires_payment_method" => "Pending",
            "requires_confirmation" => "Pending",
            "requires_action" => "Processing",
            "processing" => "Processing",
            "succeeded" => "Completed",
            "canceled" => "Failed",
            _ => "Unknown"
        };
    }

    private static string MapRefundStatus(string refundStatus)
    {
        return refundStatus switch
        {
            "pending" => "Processing",
            "succeeded" => "Completed",
            "failed" => "Failed",
            "canceled" => "Failed",
            _ => "Unknown"
        };
    }
}