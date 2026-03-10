using Microsoft.Extensions.Configuration;
using OneSignalApi.Api;
using OneSignalApi.Client;
using OneSignalApi.Model;

namespace Business.Services
{
    public interface IOneSignalService
    {
        Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, string? preheader = null);
        Task<bool> SendPushNotificationAsync(IEnumerable<string> externalUserIds, string title, string message, Dictionary<string, object>? data = null);
    }

    public class OneSignalService : IOneSignalService
    {
        private readonly IConfiguration _config;
        private readonly DefaultApi _apiInstance;
        private readonly string _appId;

        public OneSignalService(IConfiguration config)
        {
            _config = config;
            
            var apiKey = _config["OneSignal:ApiKey"];
            _appId = _config["OneSignal:AppId"] ?? "";

            var osConfig = new Configuration();
            osConfig.BasePath = "https://api.onesignal.com";
            osConfig.AccessToken = apiKey;

            _apiInstance = new DefaultApi(osConfig);
        }

        /// <summary>
        /// Send an email via OneSignal SDK
        /// </summary>
        public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, string? preheader = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_appId))
                {
                    Console.WriteLine("[OneSignal] Not configured - AppId missing");
                    return false;
                }

                var notification = new Notification(appId: _appId)
                {
                    IncludeEmailTokens = new List<string> { toEmail },
                    EmailSubject = subject,
                    EmailPreheader = preheader ?? $"Message for {toName}",
                    EmailBody = htmlBody
                };

                var result = await Task.Run(() => _apiInstance.CreateNotification(notification));
                
                Console.WriteLine($"[OneSignal] Email sent successfully to {toEmail}. ID: {result.Id}");
                return true;
            }
            catch (ApiException ex)
            {
                Console.WriteLine($"[OneSignal] API Error sending email: {ex.ErrorCode} - {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OneSignal] Error sending email: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send push notification via OneSignal SDK
        /// </summary>
        public async Task<bool> SendPushNotificationAsync(IEnumerable<string> externalUserIds, string title, string message, Dictionary<string, object>? data = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_appId))
                {
                    Console.WriteLine("[OneSignal] Not configured - AppId missing");
                    return false;
                }

                var ids = externalUserIds?.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList() ?? new List<string>();
                if (ids.Count == 0)
                {
                    Console.WriteLine("[OneSignal] No recipients for push notification");
                    return false;
                }

                var notification = new Notification(appId: _appId)
                {
                    IncludeExternalUserIds = ids,
                    ChannelForExternalUserIds = "push",
                    Headings = new StringMap(en: title),
                    Contents = new StringMap(en: message),
                    Data = data
                };

                var result = await Task.Run(() => _apiInstance.CreateNotification(notification));
                
                Console.WriteLine($"[OneSignal] Push notification sent to {ids.Count} users. ID: {result.Id}");
                return true;
            }
            catch (ApiException ex)
            {
                Console.WriteLine($"[OneSignal] API Error sending push: {ex.ErrorCode} - {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OneSignal] Error sending push: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generate HTML email for team member invite
        /// </summary>
        public static string GenerateInviteEmailHtml(string name, string email, string password, string companyName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #004732; color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 8px 8px; }}
        .credentials {{ background: white; border: 1px solid #e0e0e0; border-radius: 8px; padding: 20px; margin: 20px 0; }}
        .credential-row {{ margin: 10px 0; }}
        .credential-label {{ font-weight: bold; color: #666; }}
        .credential-value {{ font-family: monospace; font-size: 16px; color: #004732; background: #f0f0f0; padding: 8px 12px; border-radius: 4px; display: inline-block; margin-top: 5px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Welcome to Silk Road!</h1>
    </div>
    <div class='content'>
        <p>Hi <strong>{name}</strong>,</p>
        <p>You've been invited to join <strong>{companyName}</strong> on Silk Road.</p>
        <p>Download the app and log in with your credentials below:</p>
        
        <div class='credentials'>
            <div class='credential-row'>
                <div class='credential-label'>Email:</div>
                <div class='credential-value'>{email}</div>
            </div>
            <div class='credential-row'>
                <div class='credential-label'>Password:</div>
                <div class='credential-value'>{password}</div>
            </div>
        </div>
        
        <p><strong>Important:</strong> For security, you'll be asked to change your password on first login.</p>
        
        <p>If you have any questions, please contact your company administrator.</p>
        
        <div class='footer'>
            <p>© {DateTime.UtcNow.Year} Silk Road. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
