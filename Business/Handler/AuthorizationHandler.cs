using AutoMapper;
using Business.Data;
using Business.Model;
using Business.Response;
using Business.Utilities;
using Business.ViewModel;
using Microsoft.AspNet.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Handlers
{
    public class AuthorizationHandler
    {
        private readonly DBContext _ctx;
        private readonly IMapper _mapper;
        private readonly IJwtUtils _jwtUtils;

        public AuthorizationHandler(DBContext ctx, IMapper mapper, IJwtUtils jwtUtils)
        {
            _ctx = ctx;
            _mapper = mapper;
            _jwtUtils = jwtUtils;
        }

        public async Task<LoginRM> Authorize(LoginVM loginVM)
        {
            var ph = new PasswordHasher();
            
            // Find user by email
            var user = await _ctx.User
                .Include(u => u.userCompanies)
                    .ThenInclude(uc => uc.company)
                .FirstOrDefaultAsync(u => u.email == loginVM.email && u.active);

            if (user == null)
            {
                return new LoginRM
                {
                    errorMessage = "Invalid credentials",
                    token = string.Empty
                };
            }

            // Verify password
            var passwordVerification = ph.VerifyHashedPassword(user.password, loginVM.password);
            if (passwordVerification == PasswordVerificationResult.Failed)
            {
                return new LoginRM
                {
                    errorMessage = "Invalid credentials",
                    token = string.Empty
                };
            }

            // Check if user has any company assignments
            if (!user.userCompanies.Any())
            {
                return new LoginRM
                {
                    errorMessage = "User is not assigned to any company",
                    token = string.Empty
                };
            }

            // Get list of companies user is assigned to
            var companies = user.userCompanies
                .Where(uc => uc.company.active)
                .Select(uc => new CompanyAssignment
                {
                    companyId = uc.companyId,
                    companyName = uc.company.name,
                    role = uc.role.ToString(),
                    companyLogo = uc.company.logo,
                    companyLogoThumbnail = uc.company.logoThumbnail
                })
                .ToList();

            if (!companies.Any())
            {
                return new LoginRM
                {
                    errorMessage = "User has no active company assignments",
                    token = string.Empty
                };
            }

            // Generate token (without company context initially - company will be sent with each request)
            var token = _jwtUtils.GenerateJwtToken(user);

            return new LoginRM
            {
                userId = user.id,
                token = token,
                email = user.email,
                errorMessage = string.Empty,
                tokenExpiration = DateTime.UtcNow.AddMonths(3),
                changePasswordRequired = user.changePasswordRequired,
                companies = companies
            };
        }

        public async Task<LoginRM> Register(RegisterVM registerVM, Services.IEmailService emailService)
        {
            var ph = new PasswordHasher();

            // Check if user already exists
            var existingUser = await _ctx.User.FirstOrDefaultAsync(u => u.email == registerVM.email);
            if (existingUser != null)
            {
                return new LoginRM
                {
                    errorMessage = "Account already exists!",
                    token = string.Empty
                };
            }

            // Validate password confirmation
            if (registerVM.password != registerVM.confirmPassword)
            {
                return new LoginRM
                {
                    errorMessage = "Password and Confirm Password do not match!",
                    token = string.Empty
                };
            }

            // All users who register create their own company and become Admin
            // This is the owner/admin registration flow
            var now = DateTime.UtcNow;
            var user = new User
            {
                id = Guid.NewGuid(),
                fullName = registerVM.fullName,
                email = registerVM.email,
                password = ph.HashPassword(registerVM.password),
                phone = registerVM.phone ?? string.Empty,
                city = registerVM.city ?? string.Empty,
                state = registerVM.state ?? string.Empty,
                zipcode = registerVM.zipcode ?? string.Empty,
                country = registerVM.country ?? string.Empty,
                verificationToken = string.Empty,
                active = true,
                verified = false,
                createDatetime = now,
                updateDatetime = now,
                changePasswordRequired = false
            };

            _ctx.User.Add(user);

            // Create a new company for this user
            var company = new Company
            {
                id = Guid.NewGuid(),
                name = registerVM.company ?? $"{registerVM.fullName}'s Company",
                email = registerVM.email,
                phone = registerVM.phone ?? string.Empty,
                city = registerVM.city ?? string.Empty,
                state = registerVM.state ?? string.Empty,
                zipcode = registerVM.zipcode ?? string.Empty,
                country = registerVM.country ?? string.Empty,
                category = registerVM.category,  // Business category from registration
                logo = registerVM.companyLogo,  // Company logo from registration
                active = true,
                createDatetime = now,
                updateDatetime = now
            };
            _ctx.Company.Add(company);

            // Create default CompanyConfig for new company
            var companyConfig = new CompanyConfig
            {
                id = Guid.NewGuid(),
                companyId = company.id,
                defaultTermsAndConditions = string.Empty,
                workDayStartHour = 9,
                workDayStartMinute = 0,
                workDayEndHour = 17,
                workDayEndMinute = 0,
                workDays = "1,2,3,4,5", // Monday-Friday
                lunchDeductionMinutes = 30,
                enableClockInReminder = true,
                enableClockOutReminder = true,
                reminderMinutesBefore = 5,
                requireApproval = true,
                overtimeThresholdHours = 8,
                weeklyOvertimeThresholdHours = 40,
                enableAutoClockIn = false,
                autoClockInRadiusMeters = 100,
                createDatetime = now,
                updateDatetime = now
            };
            _ctx.CompanyConfig.Add(companyConfig);

            // Assign user as Admin of their company
            var userCompany = new UserCompany
            {
                userId = user.id,
                companyId = company.id,
                role = RoleNames.Admin,
                assignedDatetime = now
            };
            _ctx.UserCompany.Add(userCompany);

            await _ctx.SaveChangesAsync();

            // Send welcome email (non-blocking: registration succeeds even if email fails)
            try
            {
                await emailService.SendRegistrationConfirmationAsync(
                    user.email,
                    user.fullName ?? "User",
                    company.name
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Register] Welcome email failed for {user.email}: {ex.Message}");
            }

            // Prepare response with company assignment
            var companies = new List<CompanyAssignment>
            {
                new CompanyAssignment
                {
                    companyId = company.id,
                    companyName = company.name,
                    role = RoleNames.Admin,
                    companyLogo = company.logo,
                    companyLogoThumbnail = company.logoThumbnail
                }
            };

            // Generate JWT token for the newly registered user
            var token = _jwtUtils.GenerateJwtToken(user);

            return new LoginRM
            {
                userId = user.id,
                email = user.email,
                token = token,
                tokenExpiration = DateTime.UtcNow.AddMonths(3),
                errorMessage = string.Empty,
                companies = companies,
                changePasswordRequired = false
            };
        }

        public async Task<(bool success, string message)> ChangePassword(Guid userId, string newPassword)
        {
            var user = await _ctx.User.FirstOrDefaultAsync(u => u.id == userId && u.active);
            
            if (user == null)
            {
                return (false, "User not found");
            }

            // Validate password strength
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                return (false, "Password must be at least 8 characters");
            }

            // Hash the new password
            var ph = new PasswordHasher();
            user.password = ph.HashPassword(newPassword);
            user.changePasswordRequired = false;
            user.updateDatetime = DateTime.UtcNow;

            await _ctx.SaveChangesAsync();

            return (true, "Password changed successfully");
        }

        public async Task<(bool success, string message)> ForgotPassword(string email, Services.IEmailService emailService)
        {
            var user = await _ctx.User.FirstOrDefaultAsync(u => u.email == email && u.active);
            
            if (user == null)
            {
                // Don't reveal if email exists - just log and return success
                Console.WriteLine($"[ForgotPassword] No user found for email: {email}");
                return (true, "If an account exists, a reset email has been sent");
            }

            // Generate reset token
            var resetToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            user.passwordResetToken = resetToken;
            user.passwordResetTokenExpiry = DateTime.UtcNow.AddHours(24);
            user.updateDatetime = DateTime.UtcNow;

            await _ctx.SaveChangesAsync();

            // Send email
            var emailSent = await emailService.SendPasswordResetEmailAsync(
                user.email,
                resetToken,
                user.fullName ?? "User"
            );

            Console.WriteLine($"[ForgotPassword] Reset email sent to {email}: {emailSent}");

            return (true, "Password reset email sent");
        }

        public async Task<(bool success, string message)> ResetPassword(string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return (false, "Invalid reset token");
            }

            var user = await _ctx.User.FirstOrDefaultAsync(u => 
                u.passwordResetToken == token && 
                u.passwordResetTokenExpiry > DateTime.UtcNow &&
                u.active);
            
            if (user == null)
            {
                return (false, "Invalid or expired reset token");
            }

            // Validate password strength
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                return (false, "Password must be at least 8 characters");
            }

            // Hash the new password and clear the reset token
            var ph = new PasswordHasher();
            user.password = ph.HashPassword(newPassword);
            user.passwordResetToken = null;
            user.passwordResetTokenExpiry = null;
            user.changePasswordRequired = false;
            user.updateDatetime = DateTime.UtcNow;

            await _ctx.SaveChangesAsync();

            Console.WriteLine($"[ResetPassword] Password reset successful for user: {user.email}");

            return (true, "Password reset successfully");
        }

        public async Task<(bool success, string message)> SendTeamMemberInvite(
            Guid userId, 
            string companyName, 
            string invitedByName,
            Services.IEmailService emailService)
        {
            var user = await _ctx.User.FirstOrDefaultAsync(u => u.id == userId);
            
            if (user == null)
            {
                return (false, "User not found");
            }

            // Generate invite token
            var inviteToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            user.inviteToken = inviteToken;
            user.inviteTokenExpiry = DateTime.UtcNow.AddDays(7);
            user.updateDatetime = DateTime.UtcNow;

            await _ctx.SaveChangesAsync();

            // Send email
            var emailSent = await emailService.SendTeamMemberInviteEmailAsync(
                user.email,
                inviteToken,
                user.fullName ?? "Team Member",
                companyName,
                invitedByName
            );

            Console.WriteLine($"[SendTeamMemberInvite] Invite email sent to {user.email}: {emailSent}");

            return (emailSent, emailSent ? "Invitation sent successfully" : "Failed to send invitation email");
        }
    }
}
