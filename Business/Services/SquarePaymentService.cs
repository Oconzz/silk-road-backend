using Business.Data;
using Business.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace Business.Services
{
    public interface ISquarePaymentService
    {
        /// <summary>
        /// Generate the Square OAuth authorization URL for the user to grant payment access
        /// </summary>
        string GetAuthorizationUrl(Guid userId);

        /// <summary>
        /// Exchange the authorization code for access and refresh tokens.
        /// Returns success and merchant ID.
        /// </summary>
        Task<(bool success, string? merchantId, string? locationId)> ExchangeCodeForTokensAsync(Guid userId, string authorizationCode);

        /// <summary>
        /// Get the connection status for a user
        /// </summary>
        Task<(bool isLinked, string? merchantId, string? locationId)> GetConnectionStatusAsync(Guid userId);

        /// <summary>
        /// Disconnect Square from the user's account (revoke tokens)
        /// </summary>
        Task<bool> DisconnectAsync(Guid userId);

        /// <summary>
        /// Initiate a payment - creates a Payment record and returns POS deep link URL
        /// </summary>
        Task<(Payment? payment, string? posUrl)> InitiatePaymentAsync(Guid userId, Guid jobId, decimal amount, string? notes);

        /// <summary>
        /// Record the result of a payment from Square POS callback
        /// </summary>
        Task<Payment?> RecordPaymentResultAsync(Guid paymentId, string status, string? squarePaymentId,
            string? squareTransactionId, string? cardBrand, string? lastFourDigits, string? errorMessage);

        /// <summary>
        /// Get all payments for a specific job
        /// </summary>
        Task<List<Payment>> GetPaymentsForJobAsync(Guid jobId);

        /// <summary>
        /// Get paginated payment history for a user
        /// </summary>
        Task<(List<Payment> payments, int totalCount)> GetPaymentsForUserAsync(Guid userId, int pageNumber, int pageSize);

        /// <summary>
        /// [SANDBOX ONLY] Link Square with a pre-generated access token from Square Developer Console
        /// </summary>
        Task<(bool success, string? merchantId, string? locationId)> LinkWithSandboxTokenAsync(Guid userId, string accessToken);

        /// <summary>
        /// Create a Square Payment Link and return the shareable URL
        /// </summary>
        Task<(Payment? payment, string? paymentLinkUrl)> CreatePaymentLinkAsync(Guid userId, Guid jobId, decimal amount, string? notes);

        /// <summary>
        /// Check the status of a payment link by retrieving its associated order from Square
        /// </summary>
        Task<Payment?> CheckPaymentLinkStatusAsync(Guid paymentId);

        /// <summary>
        /// Export payments to CSV format compatible with QuickBooks
        /// </summary>
        Task<byte[]> ExportPaymentsToCsvAsync(Guid companyId, DateTime startDate, DateTime endDate, string? status = "completed");
    }

    public class SquarePaymentService : ISquarePaymentService
    {
        private readonly DBContext _ctx;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        // Square OAuth endpoints
        private const string SANDBOX_AUTH_URL = "https://connect.squareupsandbox.com/oauth2/authorize";
        private const string PRODUCTION_AUTH_URL = "https://connect.squareup.com/oauth2/authorize";
        private const string SANDBOX_TOKEN_URL = "https://connect.squareupsandbox.com/oauth2/token";
        private const string PRODUCTION_TOKEN_URL = "https://connect.squareup.com/oauth2/token";
        private const string SANDBOX_REVOKE_URL = "https://connect.squareupsandbox.com/oauth2/revoke";
        private const string PRODUCTION_REVOKE_URL = "https://connect.squareup.com/oauth2/revoke";
        private const string SANDBOX_API_URL = "https://connect.squareupsandbox.com/v2";
        private const string PRODUCTION_API_URL = "https://connect.squareup.com/v2";

        public SquarePaymentService(DBContext ctx, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _ctx = ctx;
            _config = config;
            _httpClient = httpClientFactory.CreateClient();
        }

        private bool IsSandbox => _config["Square:Environment"]?.ToLower() == "sandbox";
        private string AuthUrl => IsSandbox ? SANDBOX_AUTH_URL : PRODUCTION_AUTH_URL;
        private string TokenUrl => IsSandbox ? SANDBOX_TOKEN_URL : PRODUCTION_TOKEN_URL;
        private string RevokeUrl => IsSandbox ? SANDBOX_REVOKE_URL : PRODUCTION_REVOKE_URL;
        private string ApiUrl => IsSandbox ? SANDBOX_API_URL : PRODUCTION_API_URL;

        public string GetAuthorizationUrl(Guid userId)
        {
            var applicationId = _config["Square:ApplicationId"];
            var redirectUri = _config["Square:RedirectUri"];
            var scopes = _config["Square:Scopes"] ?? "PAYMENTS_WRITE PAYMENTS_READ MERCHANT_PROFILE_READ";

            // Build URL manually - redirect_uri should be URL encoded but Square expects
            // the URL structure to remain readable (not double-encoded)
            var queryBuilder = new StringBuilder();
            queryBuilder.Append($"client_id={Uri.EscapeDataString(applicationId ?? "")}");
            queryBuilder.Append($"&redirect_uri={Uri.EscapeDataString(redirectUri ?? "")}");
            queryBuilder.Append("&response_type=code");
            queryBuilder.Append($"&scope={Uri.EscapeDataString(scopes)}");
            queryBuilder.Append($"&state={Uri.EscapeDataString(userId.ToString())}");

            return $"{AuthUrl}?{queryBuilder}";
        }

        public async Task<(bool success, string? merchantId, string? locationId)> ExchangeCodeForTokensAsync(Guid userId, string authorizationCode)
        {
            try
            {
                var applicationId = _config["Square:ApplicationId"];
                var applicationSecret = _config["Square:ApplicationSecret"];
                var redirectUri = _config["Square:RedirectUri"];

                // Exchange code for tokens
                var tokenRequest = new
                {
                    client_id = applicationId,
                    client_secret = applicationSecret,
                    code = authorizationCode,
                    grant_type = "authorization_code",
                    redirect_uri = redirectUri
                };

                var jsonContent = JsonSerializer.Serialize(tokenRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(TokenUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[SquarePayment] Token exchange failed: {errorContent}");
                    return (false, null, null);
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<SquareTokenResponse>(responseContent);

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    Console.WriteLine("[SquarePayment] Invalid token response");
                    return (false, null, null);
                }

                // Get merchant info including location
                var (merchantId, locationId) = await GetMerchantInfoAsync(tokenResponse.AccessToken);

                // Save tokens to user
                var user = await _ctx.User.FindAsync(userId);
                if (user == null)
                {
                    Console.WriteLine($"[SquarePayment] User not found: {userId}");
                    return (false, null, null);
                }

                user.squareAccessToken = tokenResponse.AccessToken;
                user.squareRefreshToken = tokenResponse.RefreshToken ?? user.squareRefreshToken;
                user.squareTokenExpiry = tokenResponse.ExpiresAt != null
                    ? DateTime.Parse(tokenResponse.ExpiresAt)
                    : DateTime.UtcNow.AddDays(30);
                user.squareLinked = true;
                user.squareMerchantId = merchantId;
                user.squareLocationId = locationId;

                await _ctx.SaveChangesAsync();

                Console.WriteLine($"[SquarePayment] Square linked successfully for user {userId}");
                return (true, merchantId, locationId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SquarePayment] Error exchanging code: {ex.Message}");
                return (false, null, null);
            }
        }

        private async Task<(string? merchantId, string? locationId)> GetMerchantInfoAsync(string accessToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // Get merchant info
                var merchantResponse = await _httpClient.GetAsync($"{ApiUrl}/merchants/me");
                string? merchantId = null;

                if (merchantResponse.IsSuccessStatusCode)
                {
                    var merchantContent = await merchantResponse.Content.ReadAsStringAsync();
                    var merchantData = JsonSerializer.Deserialize<SquareMerchantResponse>(merchantContent);
                    merchantId = merchantData?.Merchant?.Id;
                }

                // Get first location (main location)
                var locationsResponse = await _httpClient.GetAsync($"{ApiUrl}/locations");
                string? locationId = null;

                if (locationsResponse.IsSuccessStatusCode)
                {
                    var locationsContent = await locationsResponse.Content.ReadAsStringAsync();
                    var locationsData = JsonSerializer.Deserialize<SquareLocationsResponse>(locationsContent);
                    locationId = locationsData?.Locations?.FirstOrDefault()?.Id;
                }

                return (merchantId, locationId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SquarePayment] Error getting merchant info: {ex.Message}");
                return (null, null);
            }
        }

        public async Task<(bool isLinked, string? merchantId, string? locationId)> GetConnectionStatusAsync(Guid userId)
        {
            var user = await _ctx.User.FindAsync(userId);
            if (user == null)
            {
                return (false, null, null);
            }

            return (user.squareLinked, user.squareMerchantId, user.squareLocationId);
        }

        public async Task<bool> DisconnectAsync(Guid userId)
        {
            try
            {
                var user = await _ctx.User.FindAsync(userId);
                if (user == null)
                {
                    return false;
                }

                // Revoke token at Square
                if (!string.IsNullOrEmpty(user.squareAccessToken))
                {
                    try
                    {
                        var applicationId = _config["Square:ApplicationId"];
                        var applicationSecret = _config["Square:ApplicationSecret"];

                        var revokeRequest = new
                        {
                            client_id = applicationId,
                            access_token = user.squareAccessToken
                        };

                        var jsonContent = JsonSerializer.Serialize(revokeRequest);
                        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client", applicationSecret);
                        await _httpClient.PostAsync(RevokeUrl, content);
                    }
                    catch
                    {
                        // Ignore revoke errors - we still want to clear local state
                    }
                }

                // Clear user's Square data
                user.squareAccessToken = null;
                user.squareRefreshToken = null;
                user.squareTokenExpiry = null;
                user.squareLinked = false;
                user.squareMerchantId = null;
                user.squareLocationId = null;

                await _ctx.SaveChangesAsync();

                Console.WriteLine($"[SquarePayment] Square disconnected for user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SquarePayment] Error disconnecting: {ex.Message}");
                return false;
            }
        }

        public async Task<(Payment? payment, string? posUrl)> InitiatePaymentAsync(Guid userId, Guid jobId, decimal amount, string? notes)
        {
            try
            {
                var user = await _ctx.User.FindAsync(userId);
                if (user == null || !user.squareLinked)
                {
                    Console.WriteLine($"[SquarePayment] User not found or Square not linked: {userId}");
                    return (null, null);
                }

                // Get job with related data
                var job = await _ctx.Job
                    .Include(j => j.client)
                    .FirstOrDefaultAsync(j => j.id == jobId);

                if (job == null)
                {
                    Console.WriteLine($"[SquarePayment] Job not found: {jobId}");
                    return (null, null);
                }

                // Create payment record
                var payment = new Payment
                {
                    id = Guid.NewGuid(),
                    companyId = job.companyId,
                    jobId = jobId,
                    userId = userId,
                    amount = amount,
                    currency = "USD",
                    status = "pending",
                    notes = notes ?? $"Payment for: {job.title}",
                    createDatetime = DateTime.UtcNow,
                    updateDatetime = DateTime.UtcNow
                };

                _ctx.Payments.Add(payment);
                await _ctx.SaveChangesAsync();

                // Build Square POS deep link URL
                var posUrl = BuildSquarePOSUrl(payment, job, user);

                Console.WriteLine($"[SquarePayment] Payment initiated: {payment.id}");
                return (payment, posUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SquarePayment] Error initiating payment: {ex.Message}");
                return (null, null);
            }
        }

        private string BuildSquarePOSUrl(Payment payment, Job job, User user)
        {
            var applicationId = _config["Square:ApplicationId"];
            var callbackUrl = "silkroad://square-payment-callback";

            // Square POS API expects amount in cents
            var amountInCents = (int)(payment.amount * 100);

            var posData = new
            {
                amount_money = new
                {
                    amount = amountInCents,
                    currency_code = payment.currency
                },
                callback_url = callbackUrl,
                client_id = applicationId,
                location_id = user.squareLocationId,
                notes = $"Job: {job.title} - {job.jobNumber}",
                state = payment.id.ToString() // To identify payment on callback
            };

            var jsonData = JsonSerializer.Serialize(posData);
            var base64Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonData));

            return $"square-commerce-v1://payment/create?data={base64Data}";
        }

        public async Task<Payment?> RecordPaymentResultAsync(Guid paymentId, string status, string? squarePaymentId,
            string? squareTransactionId, string? cardBrand, string? lastFourDigits, string? errorMessage)
        {
            try
            {
                var payment = await _ctx.Payments.FindAsync(paymentId);
                if (payment == null)
                {
                    Console.WriteLine($"[SquarePayment] Payment not found: {paymentId}");
                    return null;
                }

                payment.status = status;
                payment.squarePaymentId = squarePaymentId;
                payment.squareTransactionId = squareTransactionId;
                payment.cardBrand = cardBrand;
                payment.lastFourDigits = lastFourDigits;
                payment.failureReason = errorMessage;
                payment.updateDatetime = DateTime.UtcNow;

                if (status == "completed")
                {
                    payment.completedDatetime = DateTime.UtcNow;
                    payment.paymentMethod = "card";
                }

                await _ctx.SaveChangesAsync();

                Console.WriteLine($"[SquarePayment] Payment result recorded: {paymentId} - {status}");
                return payment;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SquarePayment] Error recording payment result: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Payment>> GetPaymentsForJobAsync(Guid jobId)
        {
            return await _ctx.Payments
                .Where(p => p.jobId == jobId)
                .OrderByDescending(p => p.createDatetime)
                .ToListAsync();
        }

        public async Task<(List<Payment> payments, int totalCount)> GetPaymentsForUserAsync(Guid userId, int pageNumber, int pageSize)
        {
            // Get companies the user belongs to
            var userCompanyIds = await _ctx.UserCompany
                .Where(uc => uc.userId == userId)
                .Select(uc => uc.companyId)
                .ToListAsync();

            var query = _ctx.Payments
                .Where(p => userCompanyIds.Contains(p.companyId))
                .OrderByDescending(p => p.createDatetime);

            var totalCount = await query.CountAsync();
            var payments = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.job)
                .ToListAsync();

            return (payments, totalCount);
        }

        #region Private Helpers

        private async Task<string?> GetValidAccessTokenAsync(Guid userId)
        {
            var user = await _ctx.User.FindAsync(userId);
            if (user == null || !user.squareLinked)
            {
                return null;
            }

            // Check if token is expired or about to expire (5 min buffer)
            if (user.squareTokenExpiry.HasValue && user.squareTokenExpiry.Value > DateTime.UtcNow.AddMinutes(5))
            {
                return user.squareAccessToken;
            }

            // Sandbox tokens: no expiry and no refresh token — use token directly
            if (!user.squareTokenExpiry.HasValue && !string.IsNullOrEmpty(user.squareAccessToken))
            {
                return user.squareAccessToken;
            }

            // Token expired, refresh it
            if (string.IsNullOrEmpty(user.squareRefreshToken))
            {
                Console.WriteLine($"[SquarePayment] No refresh token for user {userId}");
                return null;
            }

            var refreshed = await RefreshAccessTokenAsync(user);
            return refreshed ? user.squareAccessToken : null;
        }

        private async Task<bool> RefreshAccessTokenAsync(User user)
        {
            try
            {
                var applicationId = _config["Square:ApplicationId"];
                var applicationSecret = _config["Square:ApplicationSecret"];

                var refreshRequest = new
                {
                    client_id = applicationId,
                    client_secret = applicationSecret,
                    refresh_token = user.squareRefreshToken,
                    grant_type = "refresh_token"
                };

                var jsonContent = JsonSerializer.Serialize(refreshRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(TokenUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[SquarePayment] Token refresh failed: {errorContent}");
                    return false;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<SquareTokenResponse>(responseContent);

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    return false;
                }

                user.squareAccessToken = tokenResponse.AccessToken;
                user.squareRefreshToken = tokenResponse.RefreshToken ?? user.squareRefreshToken;
                user.squareTokenExpiry = tokenResponse.ExpiresAt != null
                    ? DateTime.Parse(tokenResponse.ExpiresAt)
                    : DateTime.UtcNow.AddDays(30);

                await _ctx.SaveChangesAsync();

                Console.WriteLine($"[SquarePayment] Token refreshed for user {user.id}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SquarePayment] Error refreshing token: {ex.Message}");
                return false;
            }
        }

        #endregion

        /// <summary>
        /// [SANDBOX ONLY] Link Square with a pre-generated access token from Square Developer Console
        /// </summary>
        public async Task<(bool success, string? merchantId, string? locationId)> LinkWithSandboxTokenAsync(Guid userId, string accessToken)
        {
            try
            {
                var user = await _ctx.User.FindAsync(userId);
                if (user == null)
                {
                    Console.WriteLine($"[SquarePayment] User not found: {userId}");
                    return (false, null, null);
                }

                // Use the sandbox token to get merchant info
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // Get merchant info
                var merchantResponse = await _httpClient.GetAsync($"{ApiUrl}/merchants/me");
                string? merchantId = null;

                if (merchantResponse.IsSuccessStatusCode)
                {
                    var merchantContent = await merchantResponse.Content.ReadAsStringAsync();
                    var merchantData = JsonSerializer.Deserialize<SquareMerchantResponse>(merchantContent);
                    merchantId = merchantData?.Merchant?.Id;
                }
                else
                {
                    Console.WriteLine($"[SquarePayment] Failed to get merchant info: {await merchantResponse.Content.ReadAsStringAsync()}");
                }

                // Get first location
                string? locationId = null;
                var locationsResponse = await _httpClient.GetAsync($"{ApiUrl}/locations");

                if (locationsResponse.IsSuccessStatusCode)
                {
                    var locationsContent = await locationsResponse.Content.ReadAsStringAsync();
                    var locationsData = JsonSerializer.Deserialize<SquareLocationsResponse>(locationsContent);
                    locationId = locationsData?.Locations?.FirstOrDefault(l => l.Status == "ACTIVE")?.Id
                                ?? locationsData?.Locations?.FirstOrDefault()?.Id;
                }
                else
                {
                    Console.WriteLine($"[SquarePayment] Failed to get locations: {await locationsResponse.Content.ReadAsStringAsync()}");
                }

                // Update user with sandbox token
                user.squareAccessToken = accessToken;
                user.squareRefreshToken = null; // Sandbox tokens don't have refresh tokens
                user.squareTokenExpiry = null; // Sandbox tokens don't expire
                user.squareLinked = true;
                user.squareMerchantId = merchantId;
                user.squareLocationId = locationId;

                await _ctx.SaveChangesAsync();

                Console.WriteLine($"[SquarePayment] Sandbox token linked for user {userId}. Merchant: {merchantId}, Location: {locationId}");
                return (true, merchantId, locationId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SquarePayment] Error linking sandbox token: {ex.Message}");
                return (false, null, null);
            }
        }

        /// <summary>
        /// Create a Square Payment Link and return the shareable URL
        /// </summary>
        public async Task<(Payment? payment, string? paymentLinkUrl)> CreatePaymentLinkAsync(
            Guid userId, Guid jobId, decimal amount, string? notes)
        {
            try
            {
                var user = await _ctx.User.FindAsync(userId);
                if (user == null || !user.squareLinked)
                {
                    Console.WriteLine($"[SquarePayment] User not found or Square not linked: {userId}");
                    return (null, null);
                }

                var accessToken = await GetValidAccessTokenAsync(userId);
                if (string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine($"[SquarePayment] No valid access token for user: {userId}");
                    return (null, null);
                }

                var job = await _ctx.Job
                    .Include(j => j.client)
                    .FirstOrDefaultAsync(j => j.id == jobId);

                if (job == null)
                {
                    Console.WriteLine($"[SquarePayment] Job not found: {jobId}");
                    return (null, null);
                }

                // Create payment record
                var payment = new Payment
                {
                    id = Guid.NewGuid(),
                    companyId = job.companyId,
                    jobId = jobId,
                    userId = userId,
                    amount = amount,
                    currency = "USD",
                    status = "pending",
                    paymentMethod = "payment_link",
                    notes = notes ?? $"Payment for: {job.title}",
                    createDatetime = DateTime.UtcNow,
                    updateDatetime = DateTime.UtcNow
                };

                // Amount in cents for Square
                var amountInCents = (long)(amount * 100);
                var idempotencyKey = payment.id.ToString();

                var paymentLinkRequest = new
                {
                    idempotency_key = idempotencyKey,
                    quick_pay = new
                    {
                        name = job.title,
                        price_money = new
                        {
                            amount = amountInCents,
                            currency = "USD"
                        },
                        location_id = user.squareLocationId
                    }
                };

                var jsonContent = JsonSerializer.Serialize(paymentLinkRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.PostAsync(
                    $"{ApiUrl}/online-checkout/payment-links", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[SquarePayment] Payment link creation failed: {errorContent}");
                    return (null, null);
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var linkResponse = JsonSerializer.Deserialize<SquarePaymentLinkResponse>(responseContent);

                if (linkResponse?.PaymentLink == null)
                {
                    Console.WriteLine("[SquarePayment] Invalid payment link response");
                    return (null, null);
                }

                // Update payment with Square Payment Link details
                payment.squarePaymentLinkId = linkResponse.PaymentLink.Id;
                payment.squarePaymentLinkUrl = linkResponse.PaymentLink.Url
                    ?? linkResponse.PaymentLink.LongUrl;
                payment.squareOrderId = linkResponse.PaymentLink.OrderId;

                _ctx.Payments.Add(payment);
                await _ctx.SaveChangesAsync();

                Console.WriteLine($"[SquarePayment] Payment link created: {payment.id}, URL: {payment.squarePaymentLinkUrl}");
                return (payment, payment.squarePaymentLinkUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SquarePayment] Error creating payment link: {ex.Message}");
                return (null, null);
            }
        }

        /// <summary>
        /// Check the status of a payment link by retrieving its associated order from Square
        /// </summary>
        public async Task<Payment?> CheckPaymentLinkStatusAsync(Guid paymentId)
        {
            try
            {
                var payment = await _ctx.Payments.FindAsync(paymentId);
                if (payment == null || string.IsNullOrEmpty(payment.squareOrderId))
                {
                    return null;
                }

                // Only check if still pending
                if (payment.status != "pending")
                {
                    return payment;
                }

                var accessToken = await GetValidAccessTokenAsync(payment.userId);
                if (string.IsNullOrEmpty(accessToken))
                {
                    return payment;
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.GetAsync(
                    $"{ApiUrl}/orders/{payment.squareOrderId}");

                if (!response.IsSuccessStatusCode)
                {
                    return payment;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[SquarePayment] Order response for {payment.squareOrderId}: {responseContent}");

                var orderResponse = JsonSerializer.Deserialize<SquareRetrieveOrderResponse>(responseContent);

                if (orderResponse?.Order == null)
                {
                    return payment;
                }

                var orderState = orderResponse.Order.State;
                var hasTenders = orderResponse.Order.Tenders != null && orderResponse.Order.Tenders.Count > 0;
                Console.WriteLine($"[SquarePayment] Order state: {orderState}, Has tenders: {hasTenders}, Tender count: {orderResponse.Order.Tenders?.Count ?? 0}");

                // Payment is complete if:
                // - Order state is COMPLETED, OR
                // - Order state is OPEN but has tenders (payment received, order not yet fulfilled)
                if (orderState == "COMPLETED" || (orderState == "OPEN" && hasTenders))
                {
                    payment.status = "completed";
                    payment.completedDatetime = DateTime.UtcNow;
                    payment.updateDatetime = DateTime.UtcNow;

                    // Extract card details from tenders if available
                    var tender = orderResponse.Order.Tenders?.FirstOrDefault();
                    if (tender?.CardDetails?.Card != null)
                    {
                        payment.cardBrand = tender.CardDetails.Card.CardBrand;
                        payment.lastFourDigits = tender.CardDetails.Card.Last4;
                    }
                    if (tender?.PaymentId != null)
                    {
                        payment.squarePaymentId = tender.PaymentId;
                    }

                    await _ctx.SaveChangesAsync();
                    Console.WriteLine($"[SquarePayment] Payment {paymentId} marked as completed");
                }
                else if (orderState == "CANCELED")
                {
                    payment.status = "cancelled";
                    payment.updateDatetime = DateTime.UtcNow;
                    await _ctx.SaveChangesAsync();
                }

                return payment;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SquarePayment] Error checking payment link status: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Export payments to CSV format compatible with QuickBooks
        /// </summary>
        public async Task<byte[]> ExportPaymentsToCsvAsync(Guid companyId, DateTime startDate, DateTime endDate, string? status = "completed")
        {
            var query = _ctx.Payments
                .Include(p => p.job)
                    .ThenInclude(j => j!.client)
                .Where(p => p.companyId == companyId
                    && p.createDatetime >= startDate
                    && p.createDatetime <= endDate);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.status == status);
            }

            var payments = await query
                .OrderByDescending(p => p.createDatetime)
                .ToListAsync();

            var sb = new StringBuilder();

            // UTF-8 BOM for Excel/QuickBooks compatibility
            sb.Append('\uFEFF');

            // Header row
            sb.AppendLine("Date,PaymentNumber,CustomerName,CustomerCompany,JobNumber,JobTitle,Description,Amount,Currency,PaymentMethod,CardBrand,Last4Digits,Status,Notes");

            foreach (var p in payments)
            {
                var date = (p.completedDatetime ?? p.createDatetime).ToString("MM/dd/yyyy");
                var paymentNumber = p.id.ToString()[^8..]; // last 8 chars
                var customerName = SanitizeCsvField(p.job?.client?.contactName);
                var customerCompany = SanitizeCsvField(p.job?.client?.companyName);
                var jobNumber = SanitizeCsvField(p.job?.jobNumber);
                var jobTitle = SanitizeCsvField(p.job?.title);
                var description = SanitizeCsvField($"{p.job?.title} - Payment via {p.paymentMethod ?? "Square"}");
                var amount = p.amount.ToString("F2");
                var currency = p.currency;
                var paymentMethod = SanitizeCsvField(p.paymentMethod);
                var cardBrand = SanitizeCsvField(p.cardBrand);
                var last4 = SanitizeCsvField(p.lastFourDigits);
                var paymentStatus = p.status;
                var notes = SanitizeCsvField(p.notes);

                sb.AppendLine($"{date},{paymentNumber},{customerName},{customerCompany},{jobNumber},{jobTitle},{description},{amount},{currency},{paymentMethod},{cardBrand},{last4},{paymentStatus},{notes}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string SanitizeCsvField(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            // If field contains comma, quote, or newline, wrap in quotes and escape inner quotes
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
    }

    #region Square API DTOs

    public class SquareTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_at")]
        public string? ExpiresAt { get; set; }

        [JsonPropertyName("merchant_id")]
        public string? MerchantId { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }

    public class SquareMerchantResponse
    {
        [JsonPropertyName("merchant")]
        public SquareMerchant? Merchant { get; set; }
    }

    public class SquareMerchant
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("business_name")]
        public string? BusinessName { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }
    }

    public class SquareLocationsResponse
    {
        [JsonPropertyName("locations")]
        public List<SquareLocation>? Locations { get; set; }
    }

    public class SquareLocation
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    // Square Payment Links API DTOs

    public class SquarePaymentLinkResponse
    {
        [JsonPropertyName("payment_link")]
        public SquarePaymentLink? PaymentLink { get; set; }

        [JsonPropertyName("related_resources")]
        public SquareRelatedResources? RelatedResources { get; set; }
    }

    public class SquarePaymentLink
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("long_url")]
        public string? LongUrl { get; set; }

        [JsonPropertyName("order_id")]
        public string? OrderId { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }
    }

    public class SquareRelatedResources
    {
        [JsonPropertyName("orders")]
        public List<SquareOrder>? Orders { get; set; }
    }

    public class SquareOrder
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }
    }

    public class SquareRetrieveOrderResponse
    {
        [JsonPropertyName("order")]
        public SquareOrderDetail? Order { get; set; }
    }

    public class SquareOrderDetail
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("tenders")]
        public List<SquareTender>? Tenders { get; set; }
    }

    public class SquareTender
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("payment_id")]
        public string? PaymentId { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("card_details")]
        public SquareTenderCardDetails? CardDetails { get; set; }
    }

    public class SquareTenderCardDetails
    {
        [JsonPropertyName("card")]
        public SquareCardInfo? Card { get; set; }
    }

    public class SquareCardInfo
    {
        [JsonPropertyName("card_brand")]
        public string? CardBrand { get; set; }

        [JsonPropertyName("last_4")]
        public string? Last4 { get; set; }
    }

    #endregion
}
