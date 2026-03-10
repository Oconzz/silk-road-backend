using Business.ViewModel;
using Microsoft.Extensions.Configuration;
using Resend;

namespace Business.Services
{
    public class ResendEmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly string _defaultSender;
        private readonly string _resetLinkBaseUrl;
        private readonly string _imgBase;
        private readonly string _contactFormDestination;

        public ResendEmailService(IResend resend, IConfiguration config)
        {
            _resend = resend;
            _defaultSender = config["Resend:DefaultSender"] ?? "onboarding@resend.dev";
            _resetLinkBaseUrl = config["AppSettings:BaseUrl"] ?? "https://app.silkroadapp.com";
            _imgBase = $"{_resetLinkBaseUrl}/images/email";
            _contactFormDestination = config["ContactForm:DestinationEmail"] ?? "nelia@silkroadapp.com";
        }

        public async Task<bool> SendEmailAsync(EmailVO email)
        {
            try
            {
                var message = new EmailMessage();
                message.From = email.from ?? _defaultSender;
                message.To.Add(email.to ?? string.Empty);
                message.Subject = email.subject ?? string.Empty;
                message.HtmlBody = email.body ?? string.Empty;

                var response = await _resend.EmailSendAsync(message);
                Console.WriteLine($"[Resend] Email sent to {email.to}: Id={response.Content}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Resend] Error sending email to {email.to}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken, string userName)
        {
            var resetUrl = $"{_resetLinkBaseUrl}/reset-password.html?token={resetToken}";

            var email = new EmailVO
            {
                from = _defaultSender,
                to = toEmail,
                subject = "Reset Your Password - SilkRoad",
                body = GetPasswordResetEmailTemplate(userName, resetUrl)
            };

            return await SendEmailAsync(email);
        }

        public async Task<bool> SendTeamMemberInviteEmailAsync(string toEmail, string inviteToken, string teamMemberName, string companyName, string invitedByName)
        {
            var inviteUrl = $"{_resetLinkBaseUrl}/accept-invite?token={inviteToken}";

            var email = new EmailVO
            {
                from = _defaultSender,
                to = toEmail,
                subject = $"You've been invited to join {companyName} on SilkRoad",
                body = GetTeamMemberInviteEmailTemplate(teamMemberName, companyName, invitedByName, inviteUrl)
            };

            return await SendEmailAsync(email);
        }

        public async Task<bool> SendRegistrationConfirmationAsync(string toEmail, string userName, string companyName)
        {
            var email = new EmailVO
            {
                from = _defaultSender,
                to = toEmail,
                subject = "Welcome to SilkRoad!",
                body = GetRegistrationConfirmationTemplate(userName, companyName)
            };
            return await SendEmailAsync(email);
        }

        public async Task<bool> SendEstimateDeliveryAsync(string toEmail, string clientName, string estimateNumber, string webViewUrl, decimal totalCost)
        {
            var email = new EmailVO
            {
                from = _defaultSender,
                to = toEmail,
                subject = $"Estimate {estimateNumber} from SilkRoad",
                body = GetEstimateDeliveryTemplate(clientName, estimateNumber, webViewUrl, totalCost)
            };
            return await SendEmailAsync(email);
        }

        public async Task<bool> SendTimeCardExportAsync(string toEmail, string userName, string dateRange, string attachmentUrl)
        {
            var email = new EmailVO
            {
                from = _defaultSender,
                to = toEmail,
                subject = $"Time Card Export — {dateRange}",
                body = GetTimeCardExportTemplate(userName, dateRange, attachmentUrl)
            };
            return await SendEmailAsync(email);
        }

        public async Task<bool> SendPaymentSummaryAsync(string toEmail, string userName, string period, decimal totalAmount, int transactionCount)
        {
            var email = new EmailVO
            {
                from = _defaultSender,
                to = toEmail,
                subject = $"Payment Summary — {period}",
                body = GetPaymentSummaryTemplate(userName, period, totalAmount, transactionCount)
            };
            return await SendEmailAsync(email);
        }

        public async Task<bool> SendChurnMitigationAsync(string toEmail, string userName, string companyName, int daysSinceLastLogin)
        {
            var email = new EmailVO
            {
                from = _defaultSender,
                to = toEmail,
                subject = "We miss you at SilkRoad!",
                body = GetChurnMitigationTemplate(userName, companyName, daysSinceLastLogin)
            };
            return await SendEmailAsync(email);
        }

        public async Task<bool> SendContactFormEmailAsync(string name, string senderEmail, string subject, string message)
        {
            try
            {
                var htmlBody = GetEmailHeader("New Contact Form Message") + $@"
                <tr>
                    <td style='padding: 40px 30px;'>
                        <h1 style='font-size: 22px; font-weight: 700; color: #1a1a1a; margin: 0 0 24px 0; font-family: Arial, sans-serif;'>
                            New Contact Form Submission
                        </h1>
                        <table cellpadding='0' cellspacing='0' border='0' style='width: 100%; margin-bottom: 24px;'>
                            <tr>
                                <td style='padding: 12px 0; border-bottom: 1px solid #eee;'>
                                    <span style='font-size: 12px; font-weight: 600; color: #888; text-transform: uppercase; letter-spacing: 1px;'>Name</span><br>
                                    <span style='font-size: 15px; color: #1a1a1a; font-family: Arial, sans-serif;'>{System.Net.WebUtility.HtmlEncode(name)}</span>
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 12px 0; border-bottom: 1px solid #eee;'>
                                    <span style='font-size: 12px; font-weight: 600; color: #888; text-transform: uppercase; letter-spacing: 1px;'>Email</span><br>
                                    <a href='mailto:{System.Net.WebUtility.HtmlEncode(senderEmail)}' style='font-size: 15px; color: #1E7A50; font-family: Arial, sans-serif;'>{System.Net.WebUtility.HtmlEncode(senderEmail)}</a>
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 12px 0; border-bottom: 1px solid #eee;'>
                                    <span style='font-size: 12px; font-weight: 600; color: #888; text-transform: uppercase; letter-spacing: 1px;'>Subject</span><br>
                                    <span style='font-size: 15px; color: #1a1a1a; font-family: Arial, sans-serif;'>{System.Net.WebUtility.HtmlEncode(subject)}</span>
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 12px 0;'>
                                    <span style='font-size: 12px; font-weight: 600; color: #888; text-transform: uppercase; letter-spacing: 1px;'>Message</span><br>
                                    <p style='font-size: 15px; color: #1a1a1a; line-height: 1.6; font-family: Arial, sans-serif; white-space: pre-wrap;'>{System.Net.WebUtility.HtmlEncode(message)}</p>
                                </td>
                            </tr>
                        </table>
                        <p style='font-size: 12px; color: #999; font-family: Arial, sans-serif;'>
                            You can reply directly to this email to respond to {System.Net.WebUtility.HtmlEncode(name)}.
                        </p>
                    </td>
                </tr>" + GetEmailFooter();

                var emailMessage = new Resend.EmailMessage();
                emailMessage.From = _defaultSender;
                emailMessage.To.Add(_contactFormDestination);
                emailMessage.ReplyTo = senderEmail;
                emailMessage.Subject = $"[SilkRoad Contact] {subject} — {name}";
                emailMessage.HtmlBody = htmlBody;

                var response = await _resend.EmailSendAsync(emailMessage);
                Console.WriteLine($"[Resend] Contact form email sent: Id={response.Content}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Resend] Error sending contact form email: {ex.Message}");
                Console.WriteLine($"[Resend] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[Resend] Inner exception: {ex.InnerException.Message}");
                return false;
            }
        }

        // === Shared email layout helpers ===

        private string GetEmailHeader(string title)
        {
            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{title}</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #FCFCFC;'>
    <div style='width: 100%; background-color: #FCFCFC; padding: 60px 0;'>
        <!-- Logo -->
        <div style='text-align: center; margin-bottom: 60px;'>
            <img src='{_imgBase}/silkroad-logo.png' alt='SilkRoad Logo' style='width: 258px; height: auto;'>
        </div>
        <!-- Email Container -->
        <table cellpadding='0' cellspacing='0' border='0' align='center' style='width: 600px; max-width: 100%; margin: 0 auto; background-color: #FCFCFC; border: 1px solid black;'>";
        }

        private string GetEmailFooter()
        {
            return $@"
        </table>
        <!-- Footer -->
        <table cellpadding='0' cellspacing='0' border='0' align='center' style='width: 600px; max-width: 100%; margin: 0 auto; background-color: black; padding: 20px;'>
            <tr>
                <td style='padding: 20px;'>
                    <table cellpadding='0' cellspacing='0' border='0'>
                        <tr>
                            <td style='vertical-align: top; padding-right: 30px;'>
                                <img src='{_imgBase}/silkroad-icon-white.png' alt='SilkRoad Icon' style='width: 68px; height: auto;'>
                            </td>
                            <td style='vertical-align: top; border-left: 1px solid white; padding-left: 30px;'>
                                <p style='color: white; font-size: 16px; font-weight: 700; letter-spacing: 1px; margin: 0 0 12px 0; font-family: Arial, sans-serif;'>The SilkRoad Team</p>
                                <p style='color: white; font-size: 11px; font-weight: 400; letter-spacing: 1px; margin: 0 0 8px 0; font-family: Arial, sans-serif;'>
                                    <a href='mailto:support@silkroad.com' style='color: white; text-decoration: underline;'>support@silkroad.com</a>
                                </p>
                                <p style='color: white; font-size: 11px; font-weight: 400; letter-spacing: 1px; margin: 0; font-family: Arial, sans-serif;'>+1 234 555 5555</p>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>
    </div>
</body>
</html>";
        }

        private string GetAppStoreButtons()
        {
            return $@"
            <tr>
                <td style='padding: 40px 0; text-align: center;'>
                    <!--[if mso]>
                    <table role='presentation' cellspacing='0' cellpadding='0' border='0' align='center'><tr><td style='padding: 0 10px;'>
                    <![endif]-->
                    <a href='https://apps.apple.com' style='display: inline-block; margin: 5px 10px;'>
                        <img src='{_imgBase}/appstore.png' alt='Download on the App Store' style='height: 40px; width: auto; border: 0;'>
                    </a>
                    <!--[if mso]>
                    </td><td style='padding: 0 10px;'>
                    <![endif]-->
                    <a href='https://play.google.com' style='display: inline-block; margin: 5px 10px;'>
                        <img src='{_imgBase}/googleplay.png' alt='Get it on Google Play' style='height: 40px; width: auto; border: 0;'>
                    </a>
                    <!--[if mso]>
                    </td></tr></table>
                    <![endif]-->
                </td>
            </tr>";
        }

        // === Email Templates ===

        private string GetRegistrationConfirmationTemplate(string userName, string companyName)
        {
            return $@"{GetEmailHeader("Welcome to SilkRoad!")}
            <!-- Header -->
            <tr>
                <td style='padding: 40px; text-align: center;'>
                    <h1 style='margin: 0; color: black; font-size: 45px; font-weight: 700; font-family: Arial, sans-serif;'>Welcome to SilkRoad!</h1>
                </td>
            </tr>
            <!-- Hero Image -->
            <tr>
                <td>
                    <img src='{_imgBase}/hero-image.png' alt='SilkRoad App Preview' style='width: 100%; height: auto; display: block;'>
                </td>
            </tr>
            <!-- Content -->
            <tr>
                <td style='padding: 40px;'>
                    <h2 style='margin: 0 0 40px 0; color: black; font-size: 25px; font-weight: 700; font-family: Arial, sans-serif;'>Hi {userName}, welcome aboard!</h2>
                    <p style='margin: 0 0 20px 0; color: black; font-size: 16px; font-weight: 700; font-family: Arial, sans-serif;'>Your company <strong>{companyName}</strong> has been registered on SilkRoad.</p>
                    <p style='margin: 0; color: black; font-size: 16px; font-weight: 500; font-family: Arial, sans-serif;'>To start using SilkRoad Tradesman, download the app from the links below:</p>
                </td>
            </tr>
            <!-- App Store Buttons -->
            {GetAppStoreButtons()}
{GetEmailFooter()}";
        }

        private string GetPasswordResetEmailTemplate(string userName, string resetUrl)
        {
            return $@"{GetEmailHeader("Reset Your Password - SilkRoad")}
            <!-- Header -->
            <tr>
                <td style='padding: 40px; text-align: center;'>
                    <h1 style='margin: 0; color: black; font-size: 45px; font-weight: 700; font-family: Arial, sans-serif;'>Reset Your Password</h1>
                </td>
            </tr>
            <!-- Content -->
            <tr>
                <td style='padding: 40px;'>
                    <h2 style='margin: 0 0 40px 0; color: black; font-size: 25px; font-weight: 700; font-family: Arial, sans-serif;'>Hi {userName},</h2>
                    <p style='margin: 0 0 20px 0; color: black; font-size: 16px; font-weight: 700; font-family: Arial, sans-serif;'>We received a request to reset your password.</p>
                    <p style='margin: 0; color: black; font-size: 16px; font-weight: 500; font-family: Arial, sans-serif;'>Click the button below to create a new password. This link will expire in 24 hours for security reasons.</p>
                </td>
            </tr>
            <!-- CTA Button -->
            <tr>
                <td style='padding: 0 40px 20px 40px; text-align: center;'>
                    <a href='{resetUrl}' style='display: inline-block; background-color: black; color: white; padding: 14px 40px; text-decoration: none; font-size: 16px; font-weight: 700; font-family: Arial, sans-serif;'>Reset Password</a>
                </td>
            </tr>
            <!-- Note -->
            <tr>
                <td style='padding: 0 40px 40px 40px;'>
                    <p style='margin: 0; color: #666; font-size: 14px; font-family: Arial, sans-serif;'>If you didn't request a password reset, you can safely ignore this email.</p>
                </td>
            </tr>
{GetEmailFooter()}";
        }

        private string GetTeamMemberInviteEmailTemplate(string teamMemberName, string companyName, string invitedByName, string inviteUrl)
        {
            return $@"{GetEmailHeader($"You're Invited to {companyName}!")}
            <!-- Header -->
            <tr>
                <td style='padding: 40px; text-align: center;'>
                    <h1 style='margin: 0; color: black; font-size: 45px; font-weight: 700; font-family: Arial, sans-serif;'>You're Invited!</h1>
                </td>
            </tr>
            <!-- Hero Image -->
            <tr>
                <td>
                    <img src='{_imgBase}/hero-image.png' alt='SilkRoad App Preview' style='width: 100%; height: auto; display: block;'>
                </td>
            </tr>
            <!-- Content -->
            <tr>
                <td style='padding: 40px;'>
                    <h2 style='margin: 0 0 40px 0; color: black; font-size: 25px; font-weight: 700; font-family: Arial, sans-serif;'>Hi {teamMemberName},</h2>
                    <p style='margin: 0 0 20px 0; color: black; font-size: 16px; font-weight: 700; font-family: Arial, sans-serif;'><strong>{invitedByName}</strong> has invited you to join <strong>{companyName}</strong> on SilkRoad.</p>
                    <p style='margin: 0; color: black; font-size: 16px; font-weight: 500; font-family: Arial, sans-serif;'>Download the app and accept your invitation to get started:</p>
                </td>
            </tr>
            <!-- CTA Button -->
            <tr>
                <td style='padding: 0 40px 10px 40px; text-align: center;'>
                    <a href='{inviteUrl}' style='display: inline-block; background-color: black; color: white; padding: 14px 40px; text-decoration: none; font-size: 16px; font-weight: 700; font-family: Arial, sans-serif;'>Accept Invitation</a>
                </td>
            </tr>
            <!-- Note -->
            <tr>
                <td style='padding: 10px 40px 20px 40px;'>
                    <p style='margin: 0; color: #666; font-size: 14px; font-family: Arial, sans-serif; text-align: center;'>This invitation will expire in 7 days.</p>
                </td>
            </tr>
            <!-- App Store Buttons -->
            {GetAppStoreButtons()}
{GetEmailFooter()}";
        }

        private string GetEstimateDeliveryTemplate(string clientName, string estimateNumber, string webViewUrl, decimal totalCost)
        {
            return $@"{GetEmailHeader($"Estimate {estimateNumber}")}
            <!-- Header -->
            <tr>
                <td style='padding: 40px; text-align: center;'>
                    <h1 style='margin: 0; color: black; font-size: 45px; font-weight: 700; font-family: Arial, sans-serif;'>New Estimate</h1>
                </td>
            </tr>
            <!-- Content -->
            <tr>
                <td style='padding: 40px;'>
                    <h2 style='margin: 0 0 40px 0; color: black; font-size: 25px; font-weight: 700; font-family: Arial, sans-serif;'>Hi {clientName},</h2>
                    <p style='margin: 0 0 20px 0; color: black; font-size: 16px; font-weight: 500; font-family: Arial, sans-serif;'>You've received a new estimate. Here are the details:</p>
                    <table cellpadding='0' cellspacing='0' border='0' style='width: 100%; background: #f8f9fa; border-radius: 8px; margin: 20px 0;'>
                        <tr>
                            <td style='padding: 20px;'>
                                <p style='margin: 0; font-size: 16px; font-weight: 700; font-family: Arial, sans-serif;'>Estimate Number: {estimateNumber}</p>
                                <p style='margin: 8px 0 0; font-size: 16px; font-weight: 700; font-family: Arial, sans-serif;'>Total: ${totalCost:N2}</p>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
            <!-- CTA Button -->
            <tr>
                <td style='padding: 0 40px 40px 40px; text-align: center;'>
                    <a href='{webViewUrl}' style='display: inline-block; background-color: black; color: white; padding: 14px 40px; text-decoration: none; font-size: 16px; font-weight: 700; font-family: Arial, sans-serif;'>View Full Estimate</a>
                </td>
            </tr>
{GetEmailFooter()}";
        }

        private string GetTimeCardExportTemplate(string userName, string dateRange, string attachmentUrl)
        {
            return $@"{GetEmailHeader("Time Card Export")}
            <!-- Header -->
            <tr>
                <td style='padding: 40px; text-align: center;'>
                    <h1 style='margin: 0; color: black; font-size: 45px; font-weight: 700; font-family: Arial, sans-serif;'>Time Card Export</h1>
                </td>
            </tr>
            <!-- Content -->
            <tr>
                <td style='padding: 40px;'>
                    <h2 style='margin: 0 0 40px 0; color: black; font-size: 25px; font-weight: 700; font-family: Arial, sans-serif;'>Hi {userName},</h2>
                    <p style='margin: 0 0 20px 0; color: black; font-size: 16px; font-weight: 500; font-family: Arial, sans-serif;'>Your time card export for <strong>{dateRange}</strong> is ready.</p>
                </td>
            </tr>
            <!-- CTA Button -->
            <tr>
                <td style='padding: 0 40px 20px 40px; text-align: center;'>
                    <a href='{attachmentUrl}' style='display: inline-block; background-color: black; color: white; padding: 14px 40px; text-decoration: none; font-size: 16px; font-weight: 700; font-family: Arial, sans-serif;'>Download Export (CSV)</a>
                </td>
            </tr>
            <!-- Note -->
            <tr>
                <td style='padding: 0 40px 40px 40px;'>
                    <p style='margin: 0; color: #666; font-size: 14px; font-family: Arial, sans-serif; text-align: center;'>This download link will expire in 7 days.</p>
                </td>
            </tr>
{GetEmailFooter()}";
        }

        private string GetPaymentSummaryTemplate(string userName, string period, decimal totalAmount, int transactionCount)
        {
            return $@"{GetEmailHeader("Payment Summary")}
            <!-- Header -->
            <tr>
                <td style='padding: 40px; text-align: center;'>
                    <h1 style='margin: 0; color: black; font-size: 45px; font-weight: 700; font-family: Arial, sans-serif;'>Payment Summary</h1>
                </td>
            </tr>
            <!-- Content -->
            <tr>
                <td style='padding: 40px;'>
                    <h2 style='margin: 0 0 40px 0; color: black; font-size: 25px; font-weight: 700; font-family: Arial, sans-serif;'>Hi {userName},</h2>
                    <p style='margin: 0 0 20px 0; color: black; font-size: 16px; font-weight: 500; font-family: Arial, sans-serif;'>Here's your payment summary for <strong>{period}</strong>:</p>
                    <table cellpadding='0' cellspacing='0' border='0' style='width: 100%; background: #f8f9fa; border-radius: 8px; margin: 20px 0;'>
                        <tr>
                            <td style='padding: 20px;'>
                                <p style='margin: 0; font-size: 18px; font-weight: 700; font-family: Arial, sans-serif;'>Total Collected: ${totalAmount:N2}</p>
                                <p style='margin: 8px 0 0; color: #666; font-size: 14px; font-family: Arial, sans-serif;'>{transactionCount} transaction{(transactionCount != 1 ? "s" : "")}</p>
                            </td>
                        </tr>
                    </table>
                    <p style='margin: 20px 0 0; color: black; font-size: 16px; font-weight: 500; font-family: Arial, sans-serif;'>View your complete payment history in the SilkRoad app under Settings → Payments.</p>
                </td>
            </tr>
{GetEmailFooter()}";
        }

        private string GetChurnMitigationTemplate(string userName, string companyName, int daysSinceLastLogin)
        {
            return $@"{GetEmailHeader("We miss you!")}
            <!-- Header -->
            <tr>
                <td style='padding: 40px; text-align: center;'>
                    <h1 style='margin: 0; color: black; font-size: 45px; font-weight: 700; font-family: Arial, sans-serif;'>We miss you!</h1>
                </td>
            </tr>
            <!-- Hero Image -->
            <tr>
                <td>
                    <img src='{_imgBase}/hero-image.png' alt='SilkRoad App Preview' style='width: 100%; height: auto; display: block;'>
                </td>
            </tr>
            <!-- Content -->
            <tr>
                <td style='padding: 40px;'>
                    <h2 style='margin: 0 0 40px 0; color: black; font-size: 25px; font-weight: 700; font-family: Arial, sans-serif;'>Hi {userName},</h2>
                    <p style='margin: 0 0 20px 0; color: black; font-size: 16px; font-weight: 500; font-family: Arial, sans-serif;'>It's been {daysSinceLastLogin} days since you last logged into <strong>{companyName}</strong> on SilkRoad.</p>
                    <p style='margin: 0 0 20px 0; color: black; font-size: 16px; font-weight: 700; font-family: Arial, sans-serif;'>Here's what you might be missing:</p>
                    <ul style='padding-left: 20px; color: black; font-size: 16px; font-family: Arial, sans-serif;'>
                        <li style='margin-bottom: 8px;'>New features and improvements</li>
                        <li style='margin-bottom: 8px;'>Your team might need your attention</li>
                        <li>Pending estimates and job updates</li>
                    </ul>
                </td>
            </tr>
            <!-- CTA Button -->
            <tr>
                <td style='padding: 0 40px 20px 40px; text-align: center;'>
                    <a href='{_resetLinkBaseUrl}' style='display: inline-block; background-color: black; color: white; padding: 14px 40px; text-decoration: none; font-size: 16px; font-weight: 700; font-family: Arial, sans-serif;'>Open SilkRoad</a>
                </td>
            </tr>
            <!-- Note -->
            <tr>
                <td style='padding: 0 40px 40px 40px;'>
                    <p style='margin: 0; color: #666; font-size: 14px; font-family: Arial, sans-serif; text-align: center;'>If you no longer wish to receive these emails, you can update your notification preferences in the app.</p>
                </td>
            </tr>
{GetEmailFooter()}";
        }
    }
}
