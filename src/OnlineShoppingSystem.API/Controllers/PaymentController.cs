using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShoppingSystem.Application.Commands.Payment;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Interfaces;
using System.Security.Claims;

namespace OnlineShoppingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Process payment for an order
    /// </summary>
    /// <param name="command">Payment processing details</param>
    /// <returns>Payment result</returns>
    [HttpPost("process")]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaymentResultDto>> ProcessPayment([FromBody] ProcessPaymentCommand command)
    {
        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Processing payment for user {UserId}, order: {OrderId}", userId, command.OrderId);

        var result = await _paymentService.ProcessPaymentAsync(command);

        if (result.IsSuccessful)
        {
            _logger.LogInformation("Payment processed successfully for order {OrderId}: {TransactionId}", 
                command.OrderId, result.TransactionId);
        }
        else
        {
            _logger.LogWarning("Payment failed for order {OrderId}: {ErrorMessage}", 
                command.OrderId, result.ErrorMessage);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get payment details by order ID
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <returns>Payment details</returns>
    [HttpGet("order/{orderId:guid}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDto>> GetPaymentByOrderId(Guid orderId)
    {
        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Getting payment for order {OrderId} by user: {UserId}", orderId, userId);

        var payment = await _paymentService.GetPaymentByOrderIdAsync(orderId);

        return Ok(payment);
    }

    /// <summary>
    /// Get payment details by payment ID
    /// </summary>
    /// <param name="id">Payment ID</param>
    /// <returns>Payment details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDto>> GetPayment(Guid id)
    {
        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Getting payment {PaymentId} by user: {UserId}", id, userId);

        var payment = await _paymentService.GetPaymentByIdAsync(id);

        return Ok(payment);
    }

    /// <summary>
    /// Refund a payment (Admin only)
    /// </summary>
    /// <param name="id">Payment ID</param>
    /// <param name="request">Refund request details</param>
    /// <returns>Refund result</returns>
    [HttpPost("{id:guid}/refund")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResultDto>> RefundPayment(Guid id, [FromBody] RefundRequest request)
    {
        _logger.LogInformation("Processing refund for payment {PaymentId}, amount: {Amount}", id, request.Amount);

        var result = await _paymentService.RefundPaymentAsync(id, request.Amount);

        if (result.IsSuccessful)
        {
            _logger.LogInformation("Refund processed successfully for payment {PaymentId}: {TransactionId}", 
                id, result.TransactionId);
        }
        else
        {
            _logger.LogWarning("Refund failed for payment {PaymentId}: {ErrorMessage}", 
                id, result.ErrorMessage);
        }

        return Ok(result);
    }

    /// <summary>
    /// Check for duplicate payment
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <param name="paymentToken">Payment token</param>
    /// <returns>Duplicate check result</returns>
    [HttpGet("duplicate-check")]
    [ProducesResponseType(typeof(DuplicateCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DuplicateCheckResponse>> CheckDuplicatePayment(
        [FromQuery] Guid orderId, 
        [FromQuery] string paymentToken)
    {
        if (orderId == Guid.Empty)
        {
            return BadRequest("Order ID is required");
        }

        if (string.IsNullOrWhiteSpace(paymentToken))
        {
            return BadRequest("Payment token is required");
        }

        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Checking duplicate payment for user {UserId}, order: {OrderId}", userId, orderId);

        var isDuplicate = await _paymentService.IsPaymentDuplicateAsync(orderId, paymentToken);

        return Ok(new DuplicateCheckResponse
        {
            OrderId = orderId,
            PaymentToken = paymentToken,
            IsDuplicate = isDuplicate
        });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }
        return userId;
    }

    public class RefundRequest
    {
        public decimal? Amount { get; set; }
        public string? Reason { get; set; }
    }

    public class DuplicateCheckResponse
    {
        public Guid OrderId { get; set; }
        public string PaymentToken { get; set; } = string.Empty;
        public bool IsDuplicate { get; set; }
    }
}