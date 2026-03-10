using Business.Services;
using Business.ViewModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiServer.Controllers
{
    [ApiController]
    [Route("Contact")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ContactController : ControllerBase
    {
        /// <summary>
        /// Receives a contact form submission and sends it via email.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("SendMessage")]
        public async Task<IActionResult> SendMessage(
            [FromBody] ContactFormVM model,
            [FromServices] IEmailService emailService)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, error = "Invalid form data." });
            }

            try
            {
                var sent = await emailService.SendContactFormEmailAsync(
                    model.Name,
                    model.Email,
                    model.Subject,
                    model.Message
                );

                if (sent)
                {
                    return Ok(new { success = true, message = "Message sent successfully." });
                }

                return StatusCode(500, new { success = false, error = "Failed to send message. Please try again." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ContactController] Error: {ex.Message}");
                Console.WriteLine($"[ContactController] Stack: {ex.StackTrace}");
                return StatusCode(500, new { success = false, error = $"Email error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Webhook endpoint for Resend's email.received event.
        /// When someone sends an email to support@silkroadapp.com (or any @silkroadapp.com),
        /// Resend captures it and POSTs the event data here.
        ///
        /// Setup in Resend Dashboard → Webhooks → Add Webhook:
        ///   URL: https://staging-api.silkroadapp.com/Contact/InboundEmail
        ///   Event: email.received
        /// </summary>
        [AllowAnonymous]
        [HttpPost("InboundEmail")]
        public Task<IActionResult> InboundEmail(
            [FromBody] ResendWebhookPayload payload)
        {
            try
            {
                if (payload?.Data == null)
                {
                    Console.WriteLine("[InboundEmail] Received empty or invalid payload");
                    return Task.FromResult<IActionResult>(BadRequest("Invalid payload"));
                }

                var data = payload.Data;

                Console.WriteLine($"[InboundEmail] Received email from: {data.From}");
                Console.WriteLine($"[InboundEmail] To: {string.Join(", ", data.To)}");
                Console.WriteLine($"[InboundEmail] Subject: {data.Subject}");
                Console.WriteLine($"[InboundEmail] Email ID: {data.EmailId}");

                // NOTE: Do NOT send notification emails here.
                // Since DestinationEmail is support@silkroadapp.com (handled by Resend Receiving),
                // sending an email would trigger this webhook again → infinite loop.
                // All inbound emails are visible in Resend Dashboard → Receiving tab.

                // Return 200 so Resend knows the webhook was processed
                return Task.FromResult<IActionResult>(Ok(new { received = true }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InboundEmail] Error processing webhook: {ex.Message}");
                // Still return 200 to prevent Resend from retrying
                return Task.FromResult<IActionResult>(Ok(new { received = true, warning = "Processed with errors" }));
            }
        }
    }
}
