using APIServer.AuthAttributes;
using APIServer.Services;
using Business.Model;
using Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace APIServer.Controllers
{
    /// <summary>
    /// Controller for Square Payment integration
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SquarePaymentController : ControllerBase
    {
        public SquarePaymentController()
        {
        }

        /// <summary>
        /// Get the current user's Square connection status
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpGet("status")]
        public async Task<ActionResult<SquareStatusRM>> GetStatus(
            [FromServices] ISquarePaymentService squareService)
        {
            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
                !Guid.TryParse(userIdHeader, out var userId))
            {
                return BadRequest(new { Message = "X-User-Id header is required" });
            }

            var (isLinked, merchantId, locationId) = await squareService.GetConnectionStatusAsync(userId);

            return Ok(new SquareStatusRM
            {
                IsLinked = isLinked,
                MerchantId = merchantId,
                LocationId = locationId
            });
        }

        /// <summary>
        /// Get the OAuth authorization URL to link Square
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpGet("auth-url")]
        public ActionResult<SquareAuthUrlRM> GetAuthUrl(
            [FromServices] ISquarePaymentService squareService)
        {
            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
                !Guid.TryParse(userIdHeader, out var userId))
            {
                return BadRequest(new { Message = "X-User-Id header is required" });
            }

            var authUrl = squareService.GetAuthorizationUrl(userId);

            return Ok(new SquareAuthUrlRM
            {
                AuthorizationUrl = authUrl
            });
        }

        /// <summary>
        /// OAuth callback - exchange authorization code for tokens
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpPost("callback")]
        public async Task<ActionResult<SquareCallbackResultRM>> ExchangeCode(
            [FromBody] SquareCallbackVM request,
            [FromServices] ISquarePaymentService squareService)
        {
            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
                !Guid.TryParse(userIdHeader, out var userId))
            {
                return BadRequest(new { Message = "X-User-Id header is required" });
            }

            if (string.IsNullOrEmpty(request.Code))
            {
                return BadRequest(new { Message = "Authorization code is required" });
            }

            var (success, merchantId, locationId) = await squareService.ExchangeCodeForTokensAsync(userId, request.Code);

            if (!success)
            {
                return BadRequest(new { Message = "Failed to link Square. Please try again." });
            }

            return Ok(new SquareCallbackResultRM
            {
                Success = true,
                MerchantId = merchantId,
                LocationId = locationId
            });
        }

        /// <summary>
        /// [DEV/SANDBOX ONLY] Manually link Square with a pre-generated access token
        /// This bypasses OAuth for testing in Sandbox where OAuth flow doesn't work properly
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpPost("link-sandbox")]
        public async Task<ActionResult<SquareCallbackResultRM>> LinkSandbox(
            [FromBody] SandboxLinkVM request,
            [FromServices] ISquarePaymentService squareService)
        {
            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
                !Guid.TryParse(userIdHeader, out var userId))
            {
                return BadRequest(new { Message = "X-User-Id header is required" });
            }

            if (string.IsNullOrEmpty(request.AccessToken))
            {
                return BadRequest(new { Message = "Access token is required" });
            }

            var (success, merchantId, locationId) = await squareService.LinkWithSandboxTokenAsync(
                userId,
                request.AccessToken);

            if (!success)
            {
                return BadRequest(new { Message = "Failed to link Square with sandbox token" });
            }

            return Ok(new SquareCallbackResultRM
            {
                Success = true,
                MerchantId = merchantId,
                LocationId = locationId
            });
        }

        /// <summary>
        /// Disconnect Square from the user's account
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpPost("disconnect")]
        public async Task<ActionResult> Disconnect(
            [FromServices] ISquarePaymentService squareService)
        {
            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
                !Guid.TryParse(userIdHeader, out var userId))
            {
                return BadRequest(new { Message = "X-User-Id header is required" });
            }

            var success = await squareService.DisconnectAsync(userId);

            if (!success)
            {
                return BadRequest(new { Message = "Failed to disconnect Square" });
            }

            return Ok(new { Message = "Square disconnected successfully" });
        }

        /// <summary>
        /// Initiate a payment - creates a Payment record and returns Square POS deep link URL
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpPost("initiate")]
        public async Task<ActionResult<PaymentInitiationResultRM>> InitiatePayment(
            [FromBody] PaymentInitiationVM request,
            [FromServices] ISquarePaymentService squareService)
        {
            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
                !Guid.TryParse(userIdHeader, out var userId))
            {
                return BadRequest(new { Message = "X-User-Id header is required" });
            }

            if (request.JobId == Guid.Empty)
            {
                return BadRequest(new { Message = "Job ID is required" });
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new { Message = "Amount must be greater than zero" });
            }

            var (payment, posUrl) = await squareService.InitiatePaymentAsync(userId, request.JobId, request.Amount, request.Notes);

            if (payment == null || string.IsNullOrEmpty(posUrl))
            {
                return BadRequest(new { Message = "Failed to initiate payment. Please ensure Square is connected." });
            }

            return Ok(new PaymentInitiationResultRM
            {
                PaymentId = payment.id,
                SquarePosUrl = posUrl,
                Amount = payment.amount,
                Currency = payment.currency
            });
        }

        /// <summary>
        /// Create a Square Payment Link - returns a shareable URL for client payment
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpPost("create-payment-link")]
        public async Task<ActionResult<PaymentLinkResultRM>> CreatePaymentLink(
            [FromBody] PaymentInitiationVM request,
            [FromServices] ISquarePaymentService squareService)
        {
            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
                !Guid.TryParse(userIdHeader, out var userId))
            {
                return BadRequest(new { Message = "X-User-Id header is required" });
            }

            if (request.JobId == Guid.Empty)
            {
                return BadRequest(new { Message = "Job ID is required" });
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new { Message = "Amount must be greater than zero" });
            }

            var (payment, paymentLinkUrl) = await squareService.CreatePaymentLinkAsync(
                userId, request.JobId, request.Amount, request.Notes);

            if (payment == null || string.IsNullOrEmpty(paymentLinkUrl))
            {
                return BadRequest(new { Message = "Failed to create payment link. Please ensure Square is connected." });
            }

            return Ok(new PaymentLinkResultRM
            {
                PaymentId = payment.id,
                PaymentLinkUrl = paymentLinkUrl,
                Amount = payment.amount,
                Currency = payment.currency
            });
        }

        /// <summary>
        /// Check the status of a payment link
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpGet("payment-link-status/{paymentId}")]
        public async Task<ActionResult<PaymentRM>> CheckPaymentLinkStatus(
            Guid paymentId,
            [FromServices] ISquarePaymentService squareService)
        {
            if (paymentId == Guid.Empty)
            {
                return BadRequest(new { Message = "Payment ID is required" });
            }

            var payment = await squareService.CheckPaymentLinkStatusAsync(paymentId);

            if (payment == null)
            {
                return NotFound(new { Message = "Payment not found" });
            }

            return Ok(MapToPaymentRM(payment));
        }

        /// <summary>
        /// Record the result of a payment from Square POS callback
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpPost("result")]
        public async Task<ActionResult<PaymentRM>> RecordPaymentResult(
            [FromBody] PaymentResultVM request,
            [FromServices] ISquarePaymentService squareService)
        {
            if (request.PaymentId == Guid.Empty)
            {
                return BadRequest(new { Message = "Payment ID is required" });
            }

            if (string.IsNullOrEmpty(request.Status))
            {
                return BadRequest(new { Message = "Status is required" });
            }

            var payment = await squareService.RecordPaymentResultAsync(
                request.PaymentId,
                request.Status,
                request.SquarePaymentId,
                request.SquareTransactionId,
                request.CardBrand,
                request.LastFourDigits,
                request.ErrorMessage);

            if (payment == null)
            {
                return NotFound(new { Message = "Payment not found" });
            }

            return Ok(MapToPaymentRM(payment));
        }

        /// <summary>
        /// Get all payments for a specific job
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpGet("job/{jobId}/payments")]
        public async Task<ActionResult<List<PaymentRM>>> GetJobPayments(
            Guid jobId,
            [FromServices] ISquarePaymentService squareService)
        {
            if (jobId == Guid.Empty)
            {
                return BadRequest(new { Message = "Job ID is required" });
            }

            var payments = await squareService.GetPaymentsForJobAsync(jobId);
            return Ok(payments.Select(MapToPaymentRM).ToList());
        }

        /// <summary>
        /// Export payments to CSV for QuickBooks integration (Owner/Admin only)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpGet("export")]
        public async Task<ActionResult> ExportPaymentsCsv(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? status = "completed",
            [FromServices] ISquarePaymentService squareService = null!)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            if (startDate > endDate)
            {
                return BadRequest(new { Message = "startDate must be before or equal to endDate" });
            }

            // Make endDate inclusive of the full day
            var endDateInclusive = endDate.Date.AddDays(1).AddTicks(-1);

            var csvBytes = await squareService.ExportPaymentsToCsvAsync(companyId, startDate.Date, endDateInclusive, status);

            // Check if CSV only contains BOM + header (no data rows)
            var csvString = System.Text.Encoding.UTF8.GetString(csvBytes);
            var lines = csvString.TrimEnd('\r', '\n').Split('\n');
            if (lines.Length <= 1)
            {
                return NotFound(new { Message = "No payments found for the specified date range" });
            }

            var fileName = $"silkroad-payments-{startDate:yyyy-MM-dd}-{endDate:yyyy-MM-dd}.csv";
            return File(csvBytes, "text/csv", fileName);
        }

        /// <summary>
        /// Get paginated payment history for the current user
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpGet("payments")]
        public async Task<ActionResult<PaymentHistoryRM>> GetPaymentHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromServices] ISquarePaymentService squareService = null!)
        {
            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
                !Guid.TryParse(userIdHeader, out var userId))
            {
                return BadRequest(new { Message = "X-User-Id header is required" });
            }

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var (payments, totalCount) = await squareService.GetPaymentsForUserAsync(userId, page, pageSize);

            return Ok(new PaymentHistoryRM
            {
                Payments = payments.Select(MapToPaymentRM).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        private static PaymentRM MapToPaymentRM(Payment payment)
        {
            return new PaymentRM
            {
                Id = payment.id,
                JobId = payment.jobId,
                Amount = payment.amount,
                Currency = payment.currency,
                Status = payment.status,
                PaymentMethod = payment.paymentMethod,
                CardBrand = payment.cardBrand,
                LastFourDigits = payment.lastFourDigits,
                Notes = payment.notes,
                FailureReason = payment.failureReason,
                CreateDatetime = payment.createDatetime,
                CompletedDatetime = payment.completedDatetime,
                JobTitle = payment.job?.title,
                JobNumber = payment.job?.jobNumber,
                SquarePaymentLinkId = payment.squarePaymentLinkId,
                SquarePaymentLinkUrl = payment.squarePaymentLinkUrl,
                SquareOrderId = payment.squareOrderId
            };
        }
    }

    #region ViewModels

    public class SquareStatusRM
    {
        public bool IsLinked { get; set; }
        public string? MerchantId { get; set; }
        public string? LocationId { get; set; }
    }

    public class SquareAuthUrlRM
    {
        public string AuthorizationUrl { get; set; } = string.Empty;
    }

    public class SquareCallbackVM
    {
        public string Code { get; set; } = string.Empty;
        public string? State { get; set; }
    }

    public class SquareCallbackResultRM
    {
        public bool Success { get; set; }
        public string? MerchantId { get; set; }
        public string? LocationId { get; set; }
    }

    public class SandboxLinkVM
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    public class PaymentInitiationVM
    {
        public Guid JobId { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
    }

    public class PaymentInitiationResultRM
    {
        public Guid PaymentId { get; set; }
        public string SquarePosUrl { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
    }

    public class PaymentLinkResultRM
    {
        public Guid PaymentId { get; set; }
        public string PaymentLinkUrl { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
    }

    public class PaymentResultVM
    {
        public Guid PaymentId { get; set; }
        public string Status { get; set; } = string.Empty; // completed, failed, cancelled
        public string? SquarePaymentId { get; set; }
        public string? SquareTransactionId { get; set; }
        public string? CardBrand { get; set; }
        public string? LastFourDigits { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class PaymentRM
    {
        public Guid Id { get; set; }
        public Guid JobId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Status { get; set; } = "pending";
        public string? PaymentMethod { get; set; }
        public string? CardBrand { get; set; }
        public string? LastFourDigits { get; set; }
        public string? Notes { get; set; }
        public string? FailureReason { get; set; }
        public DateTime CreateDatetime { get; set; }
        public DateTime? CompletedDatetime { get; set; }
        public string? JobTitle { get; set; }
        public string? JobNumber { get; set; }
        public string? SquarePaymentLinkId { get; set; }
        public string? SquarePaymentLinkUrl { get; set; }
        public string? SquareOrderId { get; set; }
    }

    public class PaymentHistoryRM
    {
        public List<PaymentRM> Payments { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    #endregion
}
