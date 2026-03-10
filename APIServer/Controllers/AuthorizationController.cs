using AutoMapper;
using Business.Data;
using Business.Handlers;
using Business.Response;
using Business.Services;
using Business.Utilities;
using Business.ViewModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

//using Business.Interfaces;

namespace ApiServer.Controllers
{
    [ApiController]
    [Route("Authorization")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AuthorizationController : ControllerBase
    {


        public AuthorizationController()
        {
        }

        /// <summary>
        /// Login Function
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<LoginRM> Login([FromBody] LoginVM message, [FromServices] AuthorizationHandler _handler)
        {

            return await _handler.Authorize(message);

        }
        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterVM message, [FromServices] AuthorizationHandler _handler, [FromServices] IEmailService _emailService)
        {
            try
            {
                var ret = await _handler.Register(message, _emailService);

                if (string.IsNullOrEmpty(ret.errorMessage))
                {
                    return Ok(ret);
                }

                if (ret.errorMessage.StartsWith("Account already exists!"))
                {
                    return BadRequest(new { error = "email_taken", message = "An account with this email already exists. Please use a different email or try logging in." });
                }
                if (ret.errorMessage.StartsWith("Password and Confirm Password do not match!"))
                {
                    return BadRequest(new { error = "password_mismatch", message = "Password and confirmation password do not match." });
                }
                return BadRequest(new { error = "registration_failed", message = ret.errorMessage });
            }
            catch (Exception ex)
            {
                // Do not return Exception object directly; serialize a simple payload instead
                return BadRequest(new { error = "Registration failed", message = ex.Message });
            }
            //commenting this because we don't have a way for the user to login in this moment (no credentials because the db wanst been create)
            //var loginMessage = new LoginVM();
            //loginMessage.email = message.email;
            //loginMessage.password = message.password;

            //return await auth.Authorize(loginMessage);
        }
        //[AllowAnonymous]
        //[HttpPost("ForgotPassword")]
        //public async Task<string> ForgotPassword([FromBody] ForgotPasswordVM message, [FromServices] DBContext _ctx, [FromServices] IMapper _mapper, [FromServices] IJwtUtils _jwtUtils, [FromServices] Connection Conn, [FromServices] IEmailService emailService )
        //{
        //    var auth = new Authorization(_ctx, _mapper, _jwtUtils, Conn);
        //    return await auth.ForgotPassword(message, emailService);
        //}

        //[AllowAnonymous]
        //[HttpPost("ResetPassword")]
        //public async Task<string> ResetPassword([FromBody] ResetPasswordVM message, [FromServices] DBContext _ctx, [FromServices] IMapper _mapper, [FromServices] IJwtUtils _jwtUtils, [FromServices] Connection Conn)
        //{
        //    var auth = new Authorization(_ctx, _mapper, _jwtUtils, Conn);
        //    return await auth.ResetPassword(message);
        //}


        //[HttpPost("ValidatePassword")]
        //public async Task<IActionResult> ValidatePassword(
        //    [FromBody] LoginVM model,
        //    [FromServices] DBContext _ctx,
        //    [FromServices] IMapper _mapper,
        //    [FromServices] IJwtUtils _jwtUtils,
        //    [FromServices] Connection Conn
        //)
        //        {
        //            var auth = new Authorization(_ctx, _mapper, _jwtUtils, Conn);
        //            var isValid = await auth.ValidatePassword(model.email, model.password);

        //            if (!isValid)
        //                return Unauthorized(new { message = "Invalid credentials" });

        //            return Ok(new { message = "Password is valid" });
        //        }

        /// <summary>
        /// Change password for first login
        /// </summary>
        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordVM message, 
            [FromServices] AuthorizationHandler _handler)
        {
            try
            {
                var userId = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var result = await _handler.ChangePassword(Guid.Parse(userId), message.newPassword);
                
                if (!result.success)
                {
                    return BadRequest(new { message = result.message });
                }
                
                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Password change failed", message = ex.Message });
            }
        }

        /// <summary>
        /// Request password reset - sends email with reset link
        /// </summary>
        [AllowAnonymous]
        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordVM message,
            [FromServices] AuthorizationHandler _handler,
            [FromServices] IEmailService _emailService)
        {
            try
            {
                var result = await _handler.ForgotPassword(message.email, _emailService);
                
                // Always return success to prevent email enumeration attacks
                return Ok(new { message = "If an account exists with this email, a password reset link has been sent." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ForgotPassword] Error: {ex.Message}");
                // Still return success to prevent enumeration
                return Ok(new { message = "If an account exists with this email, a password reset link has been sent." });
            }
        }

        /// <summary>
        /// Reset password using token from email
        /// </summary>
        [AllowAnonymous]
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordVM message,
            [FromServices] AuthorizationHandler _handler)
        {
            try
            {
                var result = await _handler.ResetPassword(message.token, message.newPassword);
                
                if (!result.success)
                {
                    return BadRequest(new { message = result.message });
                }
                
                return Ok(new { message = "Password has been reset successfully. You can now login with your new password." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Password reset failed", message = ex.Message });
            }
        }

    }
}
