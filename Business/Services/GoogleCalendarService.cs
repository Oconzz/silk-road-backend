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
    public interface IGoogleCalendarService
    {
        /// <summary>
        /// Generate the Google OAuth authorization URL for the user to grant calendar access
        /// </summary>
        string GetAuthorizationUrl(Guid userId);

        /// <summary>
        /// Exchange the authorization code for access and refresh tokens.
        /// Returns success, email, and count of synced jobs.
        /// </summary>
        Task<(bool success, string? email, int syncedJobsCount)> ExchangeCodeForTokensAsync(Guid userId, string authorizationCode);

        /// <summary>
        /// Get the connection status for a user
        /// </summary>
        Task<(bool isLinked, string? email)> GetConnectionStatusAsync(Guid userId);

        /// <summary>
        /// Create a calendar event for a job
        /// </summary>
        Task<string?> CreateCalendarEventAsync(Guid userId, Job job);

        /// <summary>
        /// Update an existing calendar event
        /// </summary>
        Task<bool> UpdateCalendarEventAsync(Guid userId, string eventId, Job job);

        /// <summary>
        /// Delete a calendar event
        /// </summary>
        Task<bool> DeleteCalendarEventAsync(Guid userId, string eventId);

        /// <summary>
        /// Disconnect the user's calendar (revoke tokens)
        /// </summary>
        Task<bool> DisconnectCalendarAsync(Guid userId);
    }

    public class GoogleCalendarService : IGoogleCalendarService
    {
        private readonly DBContext _ctx;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        // OAuth endpoints
        private const string AUTH_URL = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string TOKEN_URL = "https://oauth2.googleapis.com/token";
        private const string REVOKE_URL = "https://oauth2.googleapis.com/revoke";
        private const string CALENDAR_API = "https://www.googleapis.com/calendar/v3";
        private const string USERINFO_URL = "https://www.googleapis.com/oauth2/v2/userinfo";

        public GoogleCalendarService(DBContext ctx, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _ctx = ctx;
            _config = config;
            _httpClient = httpClientFactory.CreateClient();
        }

        public string GetAuthorizationUrl(Guid userId)
        {
            var clientId = _config["GoogleCalendar:ClientId"];
            var redirectUri = _config["GoogleCalendar:RedirectUri"];
            var scopes = _config["GoogleCalendar:Scopes"];

            var queryParams = new Dictionary<string, string>
            {
                { "client_id", clientId ?? "" },
                { "redirect_uri", redirectUri ?? "" },
                { "response_type", "code" },
                { "scope", scopes ?? "" },
                { "access_type", "offline" },
                { "prompt", "consent" },
                { "state", userId.ToString() }
            };

            var queryString = string.Join("&", queryParams.Select(p => $"{p.Key}={HttpUtility.UrlEncode(p.Value)}"));
            return $"{AUTH_URL}?{queryString}";
        }

        public async Task<(bool success, string? email, int syncedJobsCount)> ExchangeCodeForTokensAsync(Guid userId, string authorizationCode)
        {
            try
            {
                var clientId = _config["GoogleCalendar:ClientId"];
                var clientSecret = _config["GoogleCalendar:ClientSecret"];
                var redirectUri = _config["GoogleCalendar:RedirectUri"];

                // Exchange code for tokens
                var tokenRequest = new Dictionary<string, string>
                {
                    { "code", authorizationCode },
                    { "client_id", clientId ?? "" },
                    { "client_secret", clientSecret ?? "" },
                    { "redirect_uri", redirectUri ?? "" },
                    { "grant_type", "authorization_code" }
                };

                var content = new FormUrlEncodedContent(tokenRequest);
                var response = await _httpClient.PostAsync(TOKEN_URL, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GoogleCalendar] Token exchange failed: {errorContent}");
                    return (false, null, 0);
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent);

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    Console.WriteLine("[GoogleCalendar] Invalid token response");
                    return (false, null, 0);
                }

                // Get user email from Google
                var email = await GetGoogleUserEmailAsync(tokenResponse.AccessToken);

                // Save tokens to user
                var user = await _ctx.User.FindAsync(userId);
                if (user == null)
                {
                    Console.WriteLine($"[GoogleCalendar] User not found: {userId}");
                    return (false, null, 0);
                }

                user.googleAccessToken = tokenResponse.AccessToken;
                user.googleRefreshToken = tokenResponse.RefreshToken ?? user.googleRefreshToken; // Keep existing if not returned
                user.googleTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                user.googleCalendarLinked = true;
                user.googleEmail = email;

                await _ctx.SaveChangesAsync();

                Console.WriteLine($"[GoogleCalendar] Calendar linked successfully for user {userId}");

                // NMJ-40: Auto-sync existing jobs to the new calendar
                int syncedJobsCount = await SyncExistingJobsToCalendarAsync(userId);

                return (true, email, syncedJobsCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleCalendar] Error exchanging code: {ex.Message}");
                return (false, null, 0);
            }
        }

        /// <summary>
        /// Sync all existing scheduled jobs to Google Calendar after linking
        /// </summary>
        private async Task<int> SyncExistingJobsToCalendarAsync(Guid userId)
        {
            try
            {
                // Get all companies for this user
                var userCompanies = await _ctx.UserCompany
                    .Where(uc => uc.userId == userId)
                    .Select(uc => uc.companyId)
                    .ToListAsync();

                if (!userCompanies.Any())
                {
                    return 0;
                }

                // Get all jobs with scheduled dates that don't have a calendar event yet
                var jobsToSync = await _ctx.Job
                    .Include(j => j.client)
                    .Include(j => j.location)
                    .Where(j => userCompanies.Contains(j.companyId)
                        && j.scheduledStartDate != null
                        && j.googleCalendarEventId == null
                        && j.status != null
                        && j.status.ToLower() != "cancelled"
                        && j.status.ToLower() != "completed")
                    .ToListAsync();

                if (!jobsToSync.Any())
                {
                    Console.WriteLine($"[GoogleCalendar] No existing jobs to sync for user {userId}");
                    return 0;
                }

                Console.WriteLine($"[GoogleCalendar] Syncing {jobsToSync.Count} existing jobs for user {userId}");

                int syncedCount = 0;
                foreach (var job in jobsToSync)
                {
                    try
                    {
                        var eventId = await CreateCalendarEventAsync(userId, job);
                        if (!string.IsNullOrEmpty(eventId))
                        {
                            job.googleCalendarEventId = eventId;
                            syncedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[GoogleCalendar] Failed to sync job {job.id}: {ex.Message}");
                        // Continue with other jobs
                    }
                }

                await _ctx.SaveChangesAsync();
                Console.WriteLine($"[GoogleCalendar] Synced {syncedCount}/{jobsToSync.Count} existing jobs for user {userId}");

                return syncedCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleCalendar] Error syncing existing jobs: {ex.Message}");
                return 0;
            }
        }

        public async Task<(bool isLinked, string? email)> GetConnectionStatusAsync(Guid userId)
        {
            var user = await _ctx.User.FindAsync(userId);
            if (user == null)
            {
                return (false, null);
            }

            return (user.googleCalendarLinked, user.googleEmail);
        }

        public async Task<string?> CreateCalendarEventAsync(Guid userId, Job job)
        {
            try
            {
                // Check if event already exists (NMJ-41 duplicate prevention)
                if (!string.IsNullOrEmpty(job.googleCalendarEventId))
                {
                    // Event exists, update instead of creating duplicate
                    var updated = await UpdateCalendarEventAsync(userId, job.googleCalendarEventId, job);
                    return updated ? job.googleCalendarEventId : null;
                }

                var accessToken = await GetValidAccessTokenAsync(userId);
                if (string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine($"[GoogleCalendar] No valid access token for user {userId}");
                    return null;
                }

                var calendarEvent = CreateGoogleCalendarEvent(job);
                var jsonContent = JsonSerializer.Serialize(calendarEvent);

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{CALENDAR_API}/calendars/primary/events", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GoogleCalendar] Create event failed: {errorContent}");
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var eventResponse = JsonSerializer.Deserialize<GoogleCalendarEventResponse>(responseContent);

                Console.WriteLine($"[GoogleCalendar] Event created: {eventResponse?.Id}");
                return eventResponse?.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleCalendar] Error creating event: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateCalendarEventAsync(Guid userId, string eventId, Job job)
        {
            try
            {
                var accessToken = await GetValidAccessTokenAsync(userId);
                if (string.IsNullOrEmpty(accessToken))
                {
                    return false;
                }

                var calendarEvent = CreateGoogleCalendarEvent(job);
                var jsonContent = JsonSerializer.Serialize(calendarEvent);

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{CALENDAR_API}/calendars/primary/events/{eventId}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GoogleCalendar] Update event failed: {errorContent}");

                    // If event not found (404), it may have been deleted - clear the event ID
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Console.WriteLine($"[GoogleCalendar] Event {eventId} not found, may have been deleted");
                    }
                    return false;
                }

                Console.WriteLine($"[GoogleCalendar] Event updated: {eventId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleCalendar] Error updating event: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteCalendarEventAsync(Guid userId, string eventId)
        {
            try
            {
                var accessToken = await GetValidAccessTokenAsync(userId);
                if (string.IsNullOrEmpty(accessToken))
                {
                    return false;
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.DeleteAsync($"{CALENDAR_API}/calendars/primary/events/{eventId}");

                // 404 is OK - event may already be deleted
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine($"[GoogleCalendar] Event deleted: {eventId}");
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[GoogleCalendar] Delete event failed: {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleCalendar] Error deleting event: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DisconnectCalendarAsync(Guid userId)
        {
            try
            {
                var user = await _ctx.User.FindAsync(userId);
                if (user == null)
                {
                    return false;
                }

                // NMJ-40: Delete all calendar events BEFORE revoking tokens
                // We need the access token to delete events from Google Calendar
                var userCompanies = await _ctx.UserCompany
                    .Where(uc => uc.userId == userId)
                    .Select(uc => uc.companyId)
                    .ToListAsync();

                if (userCompanies.Any() && user.googleCalendarLinked)
                {
                    var jobsWithEvents = await _ctx.Job
                        .Where(j => userCompanies.Contains(j.companyId) && j.googleCalendarEventId != null)
                        .ToListAsync();

                    int deletedCount = 0;
                    foreach (var job in jobsWithEvents)
                    {
                        try
                        {
                            // Delete the event from Google Calendar
                            var deleted = await DeleteCalendarEventAsync(userId, job.googleCalendarEventId!);
                            if (deleted)
                            {
                                deletedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[GoogleCalendar] Failed to delete event for job {job.id}: {ex.Message}");
                            // Continue with other events even if one fails
                        }

                        // Clear the event ID from the job regardless of delete success
                        job.googleCalendarEventId = null;
                    }

                    Console.WriteLine($"[GoogleCalendar] Deleted {deletedCount}/{jobsWithEvents.Count} calendar events");
                }

                // Revoke token at Google
                if (!string.IsNullOrEmpty(user.googleAccessToken))
                {
                    try
                    {
                        var revokeContent = new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            { "token", user.googleAccessToken }
                        });
                        await _httpClient.PostAsync(REVOKE_URL, revokeContent);
                    }
                    catch
                    {
                        // Ignore revoke errors - we still want to clear local state
                    }
                }

                // Clear user's Google calendar data
                user.googleAccessToken = null;
                user.googleRefreshToken = null;
                user.googleTokenExpiry = null;
                user.googleCalendarLinked = false;
                user.googleEmail = null;

                await _ctx.SaveChangesAsync();

                Console.WriteLine($"[GoogleCalendar] Calendar disconnected for user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleCalendar] Error disconnecting: {ex.Message}");
                return false;
            }
        }

        #region Private Helpers

        private async Task<string?> GetValidAccessTokenAsync(Guid userId)
        {
            var user = await _ctx.User.FindAsync(userId);
            if (user == null || !user.googleCalendarLinked)
            {
                return null;
            }

            // Check if token is expired or about to expire (5 min buffer)
            if (user.googleTokenExpiry.HasValue && user.googleTokenExpiry.Value > DateTime.UtcNow.AddMinutes(5))
            {
                return user.googleAccessToken;
            }

            // Token expired, refresh it
            if (string.IsNullOrEmpty(user.googleRefreshToken))
            {
                Console.WriteLine($"[GoogleCalendar] No refresh token for user {userId}");
                return null;
            }

            var refreshed = await RefreshAccessTokenAsync(user);
            return refreshed ? user.googleAccessToken : null;
        }

        private async Task<bool> RefreshAccessTokenAsync(User user)
        {
            try
            {
                var clientId = _config["GoogleCalendar:ClientId"];
                var clientSecret = _config["GoogleCalendar:ClientSecret"];

                var refreshRequest = new Dictionary<string, string>
                {
                    { "client_id", clientId ?? "" },
                    { "client_secret", clientSecret ?? "" },
                    { "refresh_token", user.googleRefreshToken ?? "" },
                    { "grant_type", "refresh_token" }
                };

                var content = new FormUrlEncodedContent(refreshRequest);
                var response = await _httpClient.PostAsync(TOKEN_URL, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GoogleCalendar] Token refresh failed: {errorContent}");
                    return false;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent);

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    return false;
                }

                user.googleAccessToken = tokenResponse.AccessToken;
                user.googleTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                // Note: Refresh token is NOT returned on refresh, keep existing

                await _ctx.SaveChangesAsync();

                Console.WriteLine($"[GoogleCalendar] Token refreshed for user {user.id}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleCalendar] Error refreshing token: {ex.Message}");
                return false;
            }
        }

        private async Task<string?> GetGoogleUserEmailAsync(string accessToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.GetAsync(USERINFO_URL);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(content);
                return userInfo?.Email;
            }
            catch
            {
                return null;
            }
        }

        private static GoogleCalendarEvent CreateGoogleCalendarEvent(Job job)
        {
            var startDate = job.scheduledStartDate ?? DateTime.UtcNow.Date;
            var endDate = job.scheduledEndDate ?? startDate.AddHours(8);

            // Build description with job details
            var descriptionParts = new List<string>();

            if (!string.IsNullOrEmpty(job.description))
            {
                descriptionParts.Add(job.description);
            }

            if (job.client != null)
            {
                descriptionParts.Add($"Client: {job.client.contactName}");
                if (!string.IsNullOrEmpty(job.client.email))
                {
                    descriptionParts.Add($"Email: {job.client.email}");
                }
                if (!string.IsNullOrEmpty(job.client.phone))
                {
                    descriptionParts.Add($"Phone: {job.client.phone}");
                }
            }

            if (job.estimatedTotal > 0)
            {
                descriptionParts.Add($"Estimated Total: ${job.estimatedTotal:N2}");
            }

            descriptionParts.Add($"\n---\nCreated by SilkRoad App");

            // Build location string
            string? locationString = null;
            if (job.location != null)
            {
                var locationParts = new List<string>();
                if (!string.IsNullOrEmpty(job.location.addressLine1)) locationParts.Add(job.location.addressLine1);
                if (!string.IsNullOrEmpty(job.location.addressLine2)) locationParts.Add(job.location.addressLine2);
                if (!string.IsNullOrEmpty(job.location.city)) locationParts.Add(job.location.city);
                if (!string.IsNullOrEmpty(job.location.state)) locationParts.Add(job.location.state);
                if (!string.IsNullOrEmpty(job.location.zipcode)) locationParts.Add(job.location.zipcode);
                if (locationParts.Count > 0)
                {
                    locationString = string.Join(", ", locationParts);
                }
            }

            return new GoogleCalendarEvent
            {
                Summary = $"[SilkRoad] {job.title}",
                Description = string.Join("\n", descriptionParts),
                Location = locationString,
                Start = new GoogleCalendarDateTime
                {
                    DateTime = startDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = "UTC"
                },
                End = new GoogleCalendarDateTime
                {
                    DateTime = endDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = "UTC"
                }
            };
        }

        #endregion
    }

    #region Google API DTOs

    public class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }

    public class GoogleUserInfo
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class GoogleCalendarEvent
    {
        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("start")]
        public GoogleCalendarDateTime? Start { get; set; }

        [JsonPropertyName("end")]
        public GoogleCalendarDateTime? End { get; set; }
    }

    public class GoogleCalendarDateTime
    {
        [JsonPropertyName("dateTime")]
        public string? DateTime { get; set; }

        [JsonPropertyName("timeZone")]
        public string? TimeZone { get; set; }
    }

    public class GoogleCalendarEventResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("htmlLink")]
        public string? HtmlLink { get; set; }
    }

    #endregion
}
