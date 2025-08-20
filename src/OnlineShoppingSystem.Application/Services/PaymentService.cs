using AutoMapper;
using Microsoft.Extensions.Logging;
using OnlineShoppingSystem.Application.Commands.Payment;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Interfaces;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.Enums;
using OnlineShoppingSystem.Domain.Exceptions;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentService> _logger;
    private readonly Dictionary<string, DateTime> _paymentTokenCache;

    public PaymentService(
        IUnitOfWork unitOfWork,
        IPaymentGateway paymentGateway,
        IMapper mapper,
        ILogger<PaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _paymentGateway = paymentGateway;
        _mapper = mapper;
        _logger = logger;
        _paymentTokenCache = new Dictionary<string, DateTime>();
    }

    public async Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentCommand command)
    {
        try
        {
            _logger.LogInformation("Processing payment for order {OrderId}", command.OrderId);

            // Check for duplicate payment
            if (!string.IsNullOrEmpty(command.PaymentToken) && 
                await IsPaymentDuplicateAsync(command.OrderId, command.PaymentToken))
            {
                _logger.LogWarning("Duplicate payment attempt detected for order {OrderId} with token {PaymentToken}", 
                    command.OrderId, command.PaymentToken);
                
                return new PaymentResultDto
                {
                    IsSuccessful = false,
                    Status = PaymentStatus.Failed,
                    ErrorMessage = "Duplicate payment attempt detected"
                };
            }

            // Get the order
            var order = await _unitOfWork.Orders.GetByIdAsync(command.OrderId);
            if (order == null)
            {
                throw new OrderNotFoundException(command.OrderId);
            }

            // Check if order already has a successful payment
            var existingPayment = await _unitOfWork.Payments.GetByOrderIdAsync(command.OrderId);
            if (existingPayment != null && existingPayment.IsSuccessful())
            {
                _logger.LogWarning("Order {OrderId} already has a successful payment", command.OrderId);
                
                return new PaymentResultDto
                {
                    IsSuccessful = false,
                    Status = PaymentStatus.Failed,
                    ErrorMessage = "Order already has a successful payment",
                    Payment = _mapper.Map<PaymentDto>(existingPayment)
                };
            }

            // Create payment record
            var payment = Payment.Create(command.OrderId, order.Total.Amount, string.Empty, command.PaymentMethod);
            await _unitOfWork.Payments.AddAsync(payment);

            // Process payment through gateway
            var gatewayRequest = new PaymentGatewayRequest
            {
                Amount = order.Total.Amount,
                Currency = "USD", // This could be configurable
                PaymentToken = command.PaymentToken ?? string.Empty,
                PaymentMethod = command.PaymentMethod,
                Metadata = new Dictionary<string, string>
                {
                    ["OrderId"] = command.OrderId.ToString(),
                    ["PaymentId"] = payment.Id.ToString()
                }
            };

            // Add payment details to metadata
            foreach (var detail in command.PaymentDetails)
            {
                gatewayRequest.Metadata[detail.Key] = detail.Value;
            }

            var gatewayResult = await _paymentGateway.ProcessPaymentAsync(gatewayRequest);

            // Update payment based on gateway result
            if (gatewayResult.IsSuccessful)
            {
                payment.MarkAsProcessing(gatewayResult.TransactionId);
                payment.MarkAsCompleted();
                
                // Update order status to paid
                order.MarkAsPaid();
                
                // Cache payment token to prevent duplicates
                if (!string.IsNullOrEmpty(command.PaymentToken))
                {
                    CachePaymentToken(command.OrderId, command.PaymentToken);
                }
                
                _logger.LogInformation("Payment processed successfully for order {OrderId}. TransactionId: {TransactionId}", 
                    command.OrderId, gatewayResult.TransactionId);
            }
            else
            {
                payment.MarkAsFailed(gatewayResult.ErrorMessage ?? "Payment processing failed");
                
                _logger.LogWarning("Payment failed for order {OrderId}. Error: {ErrorMessage}", 
                    command.OrderId, gatewayResult.ErrorMessage);
            }

            await _unitOfWork.SaveChangesAsync();

            var result = new PaymentResultDto
            {
                IsSuccessful = gatewayResult.IsSuccessful,
                TransactionId = gatewayResult.TransactionId,
                Status = MapGatewayStatusToPaymentStatus(gatewayResult.Status),
                Amount = gatewayResult.Amount,
                Currency = gatewayResult.Currency,
                ErrorMessage = gatewayResult.ErrorMessage,
                Payment = _mapper.Map<PaymentDto>(payment)
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for order {OrderId}", command.OrderId);
            throw;
        }
    }

    public async Task<PaymentDto> GetPaymentByOrderIdAsync(Guid orderId)
    {
        _logger.LogInformation("Retrieving payment for order {OrderId}", orderId);

        var payment = await _unitOfWork.Payments.GetByOrderIdAsync(orderId);
        if (payment == null)
        {
            throw new PaymentNotFoundException($"No payment found for order {orderId}");
        }

        return _mapper.Map<PaymentDto>(payment);
    }

    public async Task<PaymentDto> GetPaymentByIdAsync(Guid paymentId)
    {
        _logger.LogInformation("Retrieving payment {PaymentId}", paymentId);

        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
        if (payment == null)
        {
            throw new PaymentNotFoundException(paymentId);
        }

        return _mapper.Map<PaymentDto>(payment);
    }

    public async Task<PaymentResultDto> RefundPaymentAsync(Guid paymentId, decimal? refundAmount = null)
    {
        try
        {
            _logger.LogInformation("Processing refund for payment {PaymentId}", paymentId);

            var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
            if (payment == null)
            {
                throw new PaymentNotFoundException(paymentId);
            }

            if (!payment.CanBeRefunded())
            {
                throw new InvalidOperationException($"Payment {paymentId} cannot be refunded. Current status: {payment.Status}");
            }

            var amountToRefund = refundAmount ?? payment.GetRemainingRefundableAmount().Amount;
            
            if (amountToRefund <= 0)
            {
                throw new InvalidOperationException("Refund amount must be greater than zero");
            }

            if (amountToRefund > payment.GetRemainingRefundableAmount().Amount)
            {
                throw new InvalidOperationException("Refund amount exceeds remaining refundable amount");
            }

            // Process refund through gateway
            var gatewayResult = await _paymentGateway.RefundPaymentAsync(payment.TransactionId, amountToRefund);

            if (gatewayResult.IsSuccessful)
            {
                payment.ProcessRefund(new Money(amountToRefund));
                
                _logger.LogInformation("Refund processed successfully for payment {PaymentId}. Amount: {Amount}", 
                    paymentId, amountToRefund);
            }
            else
            {
                _logger.LogWarning("Refund failed for payment {PaymentId}. Error: {ErrorMessage}", 
                    paymentId, gatewayResult.ErrorMessage);
            }

            await _unitOfWork.SaveChangesAsync();

            return new PaymentResultDto
            {
                IsSuccessful = gatewayResult.IsSuccessful,
                TransactionId = gatewayResult.TransactionId,
                Status = MapGatewayStatusToPaymentStatus(gatewayResult.Status),
                Amount = gatewayResult.Amount,
                Currency = gatewayResult.Currency,
                ErrorMessage = gatewayResult.ErrorMessage,
                Payment = _mapper.Map<PaymentDto>(payment)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", paymentId);
            throw;
        }
    }

    public async Task<bool> IsPaymentDuplicateAsync(Guid orderId, string paymentToken)
    {
        if (string.IsNullOrEmpty(paymentToken))
        {
            return false;
        }

        // Check cache first (for recent attempts)
        var cacheKey = $"{orderId}:{paymentToken}";
        if (_paymentTokenCache.ContainsKey(cacheKey))
        {
            var cachedTime = _paymentTokenCache[cacheKey];
            if (DateTime.UtcNow - cachedTime < TimeSpan.FromMinutes(5)) // 5-minute window
            {
                return true;
            }
            
            // Remove expired cache entry
            _paymentTokenCache.Remove(cacheKey);
        }

        // Check database for existing successful payments with same token
        var existingPayment = await _unitOfWork.Payments.GetByOrderIdAsync(orderId);
        if (existingPayment != null && existingPayment.IsSuccessful())
        {
            return true;
        }

        return false;
    }

    private void CachePaymentToken(Guid orderId, string paymentToken)
    {
        var cacheKey = $"{orderId}:{paymentToken}";
        _paymentTokenCache[cacheKey] = DateTime.UtcNow;
        
        // Clean up old cache entries (simple cleanup)
        var expiredKeys = _paymentTokenCache
            .Where(kvp => DateTime.UtcNow - kvp.Value > TimeSpan.FromMinutes(10))
            .Select(kvp => kvp.Key)
            .ToList();
            
        foreach (var key in expiredKeys)
        {
            _paymentTokenCache.Remove(key);
        }
    }

    private static PaymentStatus MapGatewayStatusToPaymentStatus(string gatewayStatus)
    {
        return gatewayStatus switch
        {
            "Pending" => PaymentStatus.Pending,
            "Processing" => PaymentStatus.Processing,
            "Completed" => PaymentStatus.Completed,
            "Failed" => PaymentStatus.Failed,
            _ => PaymentStatus.Failed
        };
    }
}