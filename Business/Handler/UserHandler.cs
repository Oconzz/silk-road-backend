using Amazon.S3;
using Amazon.S3.Model;
using AutoMapper;
using Business.Data;
using Business.Model;
using Business.Response;
using Business.Services;
//using Business.Responses;
using Business.Utilities;
using Business.ViewModel;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Business.Handlers
{
    public class UserHandler
    {
        internal DBContext _ctx;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;
        public UserHandler(DBContext ctx, IMapper mapper, IPhotoService photoService)
        {
            _ctx = ctx;
            _mapper = mapper;
            _photoService = photoService;
        }

        // Permission validation: Check if requesting user can modify target user
        private async Task<(bool canModify, string? errorMessage, bool isSuperAdmin, bool isAdmin, Guid? adminCompanyId)> CanModifyUserAsync(Guid requestingUserId, Guid targetUserId)
        {
            // Self-modification is always allowed for basic info
            if (requestingUserId == targetUserId)
            {
                return (true, null, false, false, null);
            }

            // Get requesting user's role(s)
            var requestingUserCompanies = await _ctx.UserCompany
                .AsNoTracking()
                .Include(uc => uc.company)
                .Where(uc => uc.userId == requestingUserId && uc.company.active)
                .ToListAsync();

            if (!requestingUserCompanies.Any())
            {
                return (false, "User has no company assignments", false, false, null);
            }

            // Check if requesting user is SuperAdmin in any company
            var isSuperAdmin = requestingUserCompanies.Any(uc => 
                string.Equals(uc.role, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase));

            if (isSuperAdmin)
            {
                return (true, null, true, false, null);
            }

            // Check if requesting user is Admin in any company
            var adminCompanies = requestingUserCompanies
                .Where(uc => string.Equals(uc.role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
                .Select(uc => uc.companyId)
                .ToList();

            if (adminCompanies.Any())
            {
                // Admin can only modify users in their company
                var targetUserCompanies = await _ctx.UserCompany
                    .AsNoTracking()
                    .Where(uc => uc.userId == targetUserId)
                    .Select(uc => uc.companyId)
                    .ToListAsync();

                var sharedCompanies = adminCompanies.Intersect(targetUserCompanies).ToList();
                if (sharedCompanies.Any())
                {
                    return (true, null, false, true, sharedCompanies.First());
                }

                return (false, "Admin can only modify users within their company", false, true, adminCompanies.FirstOrDefault());
            }

            // Non-admin/non-superadmin users cannot modify other users
            return (false, "Insufficient permissions to modify other users", false, false, null);
        }

        // Permission validation: Check if requesting user can manage company assignments for target user
        private async Task<(bool canManage, string? errorMessage)> CanManageCompanyAssignmentAsync(Guid requestingUserId, Guid targetUserId, Guid companyId)
        {
            var (canModify, errorMessage, isSuperAdmin, isAdmin, adminCompanyId) = await CanModifyUserAsync(requestingUserId, targetUserId);

            if (!canModify)
            {
                return (false, errorMessage);
            }

            // SuperAdmin can manage any company assignments
            if (isSuperAdmin)
            {
                return (true, null);
            }

            // Admin can only manage assignments for their own company
            if (isAdmin)
            {
                if (adminCompanyId == companyId)
                {
                    return (true, null);
                }
                return (false, "Admin can only manage assignments for their own company");
            }

            // Regular users cannot manage company assignments (even their own)
            return (false, "Insufficient permissions to manage company assignments");
        }

        public async Task<UserUpdateVM> GetUserInfoAsync(Guid Id)
        {
            var user = await _ctx.User.Include(u => u.userCompanies).FirstOrDefaultAsync(u => u.id == Id);
            // CRITICAL FIX: Without this null check, users cannot manage their accounts.
            // If user is not found, the app crashes when viewing/updating profile.
            if (user == null)
                throw new KeyNotFoundException($"User with ID {Id} not found");
            return _mapper.Map<User, UserUpdateVM>(user)!;
        }

        // Get user roles for a specific company
        public async Task<string?> GetUserRoleForCompanyAsync(Guid userId, Guid companyId)
        {
            var userCompany = await _ctx.UserCompany
                .AsNoTracking()
                .FirstOrDefaultAsync(uc => uc.userId == userId && uc.companyId == companyId);
            
            return userCompany?.role.ToString();
        }

        // Get all company assignments for a user
        public async Task<List<UserCompanyAssignment>> GetUserCompanyAssignmentsAsync(Guid userId)
        {
            var assignments = await _ctx.UserCompany
                .AsNoTracking()
                .Include(uc => uc.company)
                .Where(uc => uc.userId == userId && uc.company.active)
                .Select(uc => new UserCompanyAssignment
                {
                    companyId = uc.companyId,
                    companyName = uc.company.name,
                    role = uc.role.ToString(),
                    category = uc.company.category,
                    assignedDatetime = uc.assignedDatetime
                })
                .ToListAsync();

            return assignments;
        }

        public async Task<bool> UpdatePaswordAsync(Guid id, string password, bool requireChange = false)
        {
            try
            {
                var user = await _ctx.User.FirstOrDefaultAsync(u => u.id == id);
                if (user == null)
                    return false;

                user.password = password;
                user.changePasswordRequired = requireChange;

                var response = _ctx.User.Update(user);
                await _ctx.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<User> GetUserAsync(Guid id)
        {
            try
            {
                return await _ctx.User.Include(u => u.userCompanies).ThenInclude(uc => uc.company).FirstOrDefaultAsync(u => u.id == id);
            }
            catch
            {
                return null;
            }
        }

        // Assign user to a company with a role
        public async Task<bool> AssignUserToCompanyAsync(Guid requestingUserId, Guid userId, Guid companyId, string role)
        {
            try
            {
                // Validate permissions
                var (canManage, errorMessage) = await CanManageCompanyAssignmentAsync(requestingUserId, userId, companyId);
                
                if (!canManage)
                {
                    throw new UnauthorizedAccessException(errorMessage ?? "Insufficient permissions to manage company assignments");
                }

                // Check if assignment already exists
                var existing = await _ctx.UserCompany.FirstOrDefaultAsync(uc => uc.userId == userId && uc.companyId == companyId);
                if (existing != null)
                {
                    // Update existing role
                    existing.role = role.ToString();
                    _ctx.UserCompany.Update(existing);
                }
                else
                {
                    // Create new assignment
                    var userCompany = new UserCompany
                    {
                        userId = userId,
                        companyId = companyId,
                        role = role.ToString(),
                        assignedDatetime = DateTime.UtcNow
                    };
                    _ctx.UserCompany.Add(userCompany);
                }

                await _ctx.SaveChangesAsync();
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Re-throw authorization exceptions
            }
            catch
            {
                return false;
            }
        }

        // Remove user from a company
        public async Task<bool> RemoveUserFromCompanyAsync(Guid requestingUserId, Guid userId, Guid companyId)
        {
            try
            {
                // Validate permissions
                var (canManage, errorMessage) = await CanManageCompanyAssignmentAsync(requestingUserId, userId, companyId);
                
                if (!canManage)
                {
                    throw new UnauthorizedAccessException(errorMessage ?? "Insufficient permissions to manage company assignments");
                }

                var userCompany = await _ctx.UserCompany.FirstOrDefaultAsync(uc => uc.userId == userId && uc.companyId == companyId);
                if (userCompany == null)
                    return false;

                _ctx.UserCompany.Remove(userCompany);
                await _ctx.SaveChangesAsync();
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Re-throw authorization exceptions
            }
            catch
            {
                return false;
            }
        }

        public virtual async Task<UsersOutVM> UpdateUserAsync(UserUpdateVM uservm, Guid requestingUserId)
        {
            // Validate permissions
            var (canModify, errorMessage, isSuperAdmin, isAdmin, adminCompanyId) = await CanModifyUserAsync(requestingUserId, uservm.id);
            
            if (!canModify)
            {
                throw new UnauthorizedAccessException(errorMessage ?? "Insufficient permissions");
            }

            var ph = new PasswordHasher();

            var user = await _ctx.User.Include(u => u.userCompanies).ThenInclude(uc => uc.company).FirstOrDefaultAsync(u => u.id == uservm.id);
            if (user == null)
                return null;

            // SuperAdmin and Admin can modify all fields
            // Regular users can only modify their own info (and this is their own user since canModify passed)
            bool isSelfUpdate = requestingUserId == uservm.id;

            if (!string.IsNullOrEmpty(uservm.email))
                user.email = uservm.email;
            if (!string.IsNullOrEmpty(uservm.password))
            {
                user.changePasswordRequired = false;
                user.password = ph.HashPassword(uservm.password);
            }
            if (!string.IsNullOrEmpty(uservm.fullName))
                user.fullName = uservm.fullName;

            if (!string.IsNullOrEmpty(uservm.phone))
                user.phone = uservm.phone;
            if (!string.IsNullOrEmpty(uservm.city))
                user.city = uservm.city;
            if (!string.IsNullOrEmpty(uservm.state))
                user.state = uservm.state;
            if (!string.IsNullOrEmpty(uservm.zipcode))
                user.zipcode = uservm.zipcode;
            if (!string.IsNullOrEmpty(uservm.country))
                user.country = uservm.country;

            // Handle photo per new rules: resize to web + thumbnail and upload to public S3; store both URLs
            if (!string.IsNullOrWhiteSpace(uservm.personalPhotoBase64))
            {
                var (webUrl, thumbUrl) = await _photoService.ProcessAndUploadAsync(uservm.personalPhotoBase64!, $"users/{user.id}/personal-photo");
                if (!string.IsNullOrEmpty(webUrl)) user.personalPhoto = webUrl;
                if (!string.IsNullOrEmpty(thumbUrl)) user.personalPhotoThumbnail = thumbUrl;
            }

            // Note: Company assignments are managed separately via AssignUserToCompanyAsync

            var response = _ctx.User.Update(user);
            await _ctx.SaveChangesAsync();

            // CRITICAL FIX: Users cannot manage their accounts without null safety on mapper output.
            // Added defensive null check in case of unexpected mapping failures.
            var _u = _mapper.Map<User, UsersOutVM>(user);
            if (_u == null)
                throw new InvalidOperationException("Failed to map user data for response");
            return _u;
        }

        // Create a new user (company assignments managed separately)
        public virtual async Task<UsersOutVM> CreateUserAsync(CreateUserVM vm)
        {
            var ph = new PasswordHasher();
            var now = DateTime.UtcNow;

            var user = new User
            {
                id = Guid.NewGuid(),
                fullName = vm.fullName,
                email = vm.email,
                password = ph.HashPassword(vm.password ?? string.Empty),
                phone = vm.phone,
                city = vm.city,
                state = vm.state,
                zipcode = vm.zipcode,
                country = vm.country,
                verificationToken = string.Empty,
                active = true,
                verified = true,
                createDatetime = now,
                updateDatetime = now,
                changePasswordRequired = false
            };

            _ctx.User.Add(user);
            await _ctx.SaveChangesAsync();

            // Reload with company assignments for mapping consistency
            var created = await _ctx.User.Include(u => u.userCompanies).ThenInclude(uc => uc.company).FirstOrDefaultAsync(u => u.id == user.id);
            return _mapper.Map<User, UsersOutVM>(created);
        }

        // New: Paginated, searchable, filterable, sortable list of users without password field
        public virtual async Task<PagedResponse<UserListItem>> GetUserListAsync(UserListRequestVM request, Guid? companyId = null)
        {
            if (request == null) request = new UserListRequestVM();
            if (request.PageNumber <= 0) request.PageNumber = 1;
            if (request.PageSize <= 0 || request.PageSize > 200) request.PageSize = 20;

            IQueryable<User> query = _ctx.User.Include(u => u.userCompanies).ThenInclude(uc => uc.company).AsNoTracking();

            // Filter by company if specified
            if (companyId.HasValue)
            {
                query = query.Where(u => u.userCompanies.Any(uc => uc.companyId == companyId.Value));
            }

            // Default to active users unless explicitly specified otherwise
            if (!request.Active.HasValue)
            {
                query = query.Where(u => u.active);
            }

            // Search
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(u =>
                (u.fullName != null && u.fullName.ToLower().Contains(term)) ||
                (u.email != null && u.email.ToLower().Contains(term)) ||
                (u.phone != null && u.phone.ToLower().Contains(term)) ||
                (u.city != null && u.city.ToLower().Contains(term)) ||
                (u.state != null && u.state.ToLower().Contains(term)) ||
                (u.country != null && u.country.ToLower().Contains(term))
                );
            }

            // Filters
            if (request.Active.HasValue)
            {
                query = query.Where(u => u.active == request.Active.Value);
            }
            if (request.Verified.HasValue)
            {
                query = query.Where(u => u.verified == request.Verified.Value);
            }
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                // Filter by role within the company context
                var roleTerm = request.Role.Trim();
                if (companyId.HasValue)
                {
                    query = query.Where(u => u.userCompanies.Any(uc => uc.companyId == companyId.Value && uc.role.ToString() == roleTerm));
                }
                else
                {
                    query = query.Where(u => u.userCompanies.Any(uc => uc.role.ToString() == roleTerm));
                }
            }

            // Sorting
            var sortBy = (request.SortBy ?? "createDatetime").Trim();
            var desc = request.SortDesc;
            query = sortBy switch
            {
                nameof(User.fullName) => desc ? query.OrderByDescending(u => u.fullName) : query.OrderBy(u => u.fullName),
                nameof(User.email) => desc ? query.OrderByDescending(u => u.email) : query.OrderBy(u => u.email),
                nameof(User.updateDatetime) => desc ? query.OrderByDescending(u => u.updateDatetime) : query.OrderBy(u => u.updateDatetime),
                nameof(User.active) => desc ? query.OrderByDescending(u => u.active) : query.OrderBy(u => u.active),
                nameof(User.verified) => desc ? query.OrderByDescending(u => u.verified) : query.OrderBy(u => u.verified),
                nameof(User.city) => desc ? query.OrderByDescending(u => u.city) : query.OrderBy(u => u.city),
                nameof(User.state) => desc ? query.OrderByDescending(u => u.state) : query.OrderBy(u => u.state),
                nameof(User.country) => desc ? query.OrderByDescending(u => u.country) : query.OrderBy(u => u.country),
                _ => desc ? query.OrderByDescending(u => u.createDatetime) : query.OrderBy(u => u.createDatetime)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
            var skip = (request.PageNumber - 1) * request.PageSize;

            var items = await query.Skip(skip).Take(request.PageSize)
                .Select(u => new UserListItem
                {
                    id = u.id,
                    fullName = u.fullName,
                    email = u.email,
                    phone = u.phone,
                    city = u.city,
                    state = u.state,
                    zipcode = u.zipcode,
                    country = u.country,
                    active = u.active,
                    verified = u.verified,
                    createDatetime = u.createDatetime,
                    updateDatetime = u.updateDatetime,
                    personalPhoto = u.personalPhoto,
                    personalPhotoThumbnail = u.personalPhotoThumbnail,
                    // Get company assignments
                    companyAssignments = u.userCompanies
                        .Where(uc => !companyId.HasValue || uc.companyId == companyId.Value)
                        .Select(uc => new UserCompanyInfo
                        {
                            companyId = uc.companyId,
                            companyName = uc.company.name,
                            role = uc.role
                        }).ToList()
                })
                .ToListAsync();

            return new PagedResponse<UserListItem>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                SortBy = sortBy,
                SortDesc = desc,
                SearchTerm = request.SearchTerm
            };
        }

        /// <summary>
        /// Generate a random password for invited users
        /// </summary>
        private string GenerateRandomPassword(int length = 12)
        {
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%&*";
            const string allChars = uppercase + lowercase + digits + special;

            var password = new StringBuilder();
            
            // Ensure at least one of each required type
            password.Append(uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)]);
            password.Append(lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)]);
            password.Append(digits[RandomNumberGenerator.GetInt32(digits.Length)]);
            password.Append(special[RandomNumberGenerator.GetInt32(special.Length)]);

            // Fill the rest with random characters
            for (int i = 4; i < length; i++)
            {
                password.Append(allChars[RandomNumberGenerator.GetInt32(allChars.Length)]);
            }

            // Shuffle the password
            return new string(password.ToString().ToCharArray().OrderBy(_ => RandomNumberGenerator.GetInt32(1000)).ToArray());
        }

        /// <summary>
        /// Invite a new team member to the company. Creates user with random password and sends email.
        /// </summary>
        public async Task<InviteUserResponseVM> InviteUserAsync(InviteUserVM request, Guid companyId, Guid requestingUserId)
        {
            try
            {
                // Validate role
                var validRoles = new[] { RoleNames.Crew, RoleNames.Leader, RoleNames.Admin };
                if (!validRoles.Contains(request.role, StringComparer.OrdinalIgnoreCase))
                {
                    return new InviteUserResponseVM
                    {
                        success = false,
                        message = $"Invalid role. Must be one of: {string.Join(", ", validRoles)}"
                    };
                }

                // Check if email already exists
                var existingUser = await _ctx.User.FirstOrDefaultAsync(u => u.email.ToLower() == request.email.ToLower());
                
                if (existingUser != null)
                {
                    // Check if user is already in this company
                    var existingAssignment = await _ctx.UserCompany
                        .FirstOrDefaultAsync(uc => uc.userId == existingUser.id && uc.companyId == companyId);
                    
                    if (existingAssignment != null)
                    {
                        return new InviteUserResponseVM
                        {
                            success = false,
                            message = "This user is already a member of your company"
                        };
                    }

                    // Add existing user to company
                    var newAssignment = new UserCompany
                    {
                        userId = existingUser.id,
                        companyId = companyId,
                        role = request.role,
                        assignedDatetime = DateTime.UtcNow
                    };
                    _ctx.UserCompany.Add(newAssignment);
                    await _ctx.SaveChangesAsync();

                    return new InviteUserResponseVM
                    {
                        success = true,
                        message = "Existing user added to your company",
                        userId = existingUser.id,
                        email = existingUser.email
                    };
                }

                // Generate random password
                var plainPassword = GenerateRandomPassword();
                var ph = new PasswordHasher();
                var hashedPassword = ph.HashPassword(plainPassword);

                // Get company name for email
                var company = await _ctx.Company.FirstOrDefaultAsync(c => c.id == companyId);
                if (company == null)
                {
                    return new InviteUserResponseVM
                    {
                        success = false,
                        message = "Company not found"
                    };
                }

                // Generate invite token for email link
                var inviteToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

                // Create new user
                var now = DateTime.UtcNow;
                var newUser = new User
                {
                    id = Guid.NewGuid(),
                    fullName = request.fullName,
                    email = request.email.ToLower().Trim(),
                    password = hashedPassword,
                    phone = request.phone,
                    verificationToken = string.Empty,
                    active = true,
                    verified = true,
                    createDatetime = now,
                    updateDatetime = now,
                    changePasswordRequired = true, // Force password change on first login
                    inviteToken = inviteToken,
                    inviteTokenExpiry = now.AddDays(7)
                };

                _ctx.User.Add(newUser);

                // Assign to company
                var userCompany = new UserCompany
                {
                    userId = newUser.id,
                    companyId = companyId,
                    role = request.role,
                    assignedDatetime = now
                };
                _ctx.UserCompany.Add(userCompany);

                await _ctx.SaveChangesAsync();

                // Return success with invite token for email sending by controller
                return new InviteUserResponseVM
                {
                    success = true,
                    message = "User created successfully",
                    userId = newUser.id,
                    email = newUser.email,
                    inviteToken = inviteToken,
                    companyName = company.name,
                    tempPassword = plainPassword
                };
            }
            catch (Exception ex)
            {
                return new InviteUserResponseVM
                {
                    success = false,
                    message = $"Failed to invite user: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get team members for a company with pagination
        /// </summary>
        public async Task<PagedResponse<TeamMemberRM>> GetTeamMembersAsync(TeamMemberListRequestVM request, Guid companyId, Guid? excludeUserId = null)
        {
            if (request == null) request = new TeamMemberListRequestVM();
            if (request.pageNumber <= 0) request.pageNumber = 1;
            if (request.pageSize <= 0 || request.pageSize > 100) request.pageSize = 20;

            var query = _ctx.UserCompany
                .Include(uc => uc.user)
                .Include(uc => uc.company)
                .Where(uc => uc.companyId == companyId)
                .AsNoTracking();

            // Exclude the requesting user from the list
            if (excludeUserId.HasValue)
            {
                query = query.Where(uc => uc.userId != excludeUserId.Value);
            }

            // Filter by active status
            if (request.active.HasValue)
            {
                query = query.Where(uc => uc.user.active == request.active.Value);
            }

            // Filter by role
            if (!string.IsNullOrEmpty(request.role))
            {
                query = query.Where(uc => uc.role == request.role);
            }

            // Search
            if (!string.IsNullOrWhiteSpace(request.searchTerm))
            {
                var term = request.searchTerm.Trim().ToLower();
                query = query.Where(uc =>
                    uc.user.fullName.ToLower().Contains(term) ||
                    uc.user.email.ToLower().Contains(term) ||
                    (uc.user.phone != null && uc.user.phone.ToLower().Contains(term))
                );
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.pageSize);

            var items = await query
                .OrderBy(uc => uc.user.fullName)
                .Skip((request.pageNumber - 1) * request.pageSize)
                .Take(request.pageSize)
                .Select(uc => new TeamMemberRM
                {
                    id = uc.user.id,
                    fullName = uc.user.fullName,
                    email = uc.user.email,
                    phone = uc.user.phone,
                    role = uc.role,
                    personalPhoto = uc.user.personalPhoto,
                    personalPhotoThumbnail = uc.user.personalPhotoThumbnail,
                    joinedDate = uc.assignedDatetime,
                    active = uc.user.active,
                    activeJobsCount = 0, // Will be computed separately if needed
                    completedJobsCount = 0
                })
                .ToListAsync();

            return new PagedResponse<TeamMemberRM>
            {
                Items = items,
                PageNumber = request.pageNumber,
                PageSize = request.pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        /// <summary>
        /// Get a single team member's details
        /// </summary>
        public async Task<TeamMemberRM?> GetTeamMemberAsync(Guid userId, Guid companyId)
        {
            var userCompany = await _ctx.UserCompany
                .Include(uc => uc.user)
                .AsNoTracking()
                .FirstOrDefaultAsync(uc => uc.userId == userId && uc.companyId == companyId);

            if (userCompany == null) return null;

            // Count active and completed jobs
            var activeJobsCount = await _ctx.JobAssignments
                .Where(ja => ja.userId == userId && ja.companyId == companyId)
                .Join(_ctx.Job,
                    ja => ja.jobId,
                    j => j.id,
                    (ja, j) => new { ja, j })
                .Where(x => x.j.companyId == companyId && 
                           (x.j.status == "In Progress" || x.j.status == "Scheduled"))
                .CountAsync();

            var completedJobsCount = await _ctx.JobAssignments
                .Where(ja => ja.userId == userId && ja.companyId == companyId)
                .Join(_ctx.Job,
                    ja => ja.jobId,
                    j => j.id,
                    (ja, j) => new { ja, j })
                .Where(x => x.j.companyId == companyId && x.j.status == "Completed")
                .CountAsync();

            return new TeamMemberRM
            {
                id = userCompany.user.id,
                fullName = userCompany.user.fullName,
                email = userCompany.user.email,
                phone = userCompany.user.phone,
                role = userCompany.role,
                personalPhoto = userCompany.user.personalPhoto,
                personalPhotoThumbnail = userCompany.user.personalPhotoThumbnail,
                joinedDate = userCompany.assignedDatetime,
                active = userCompany.user.active,
                activeJobsCount = activeJobsCount,
                completedJobsCount = completedJobsCount
            };
        }

        /// <summary>
        /// Update team member's role
        /// </summary>
        public async Task<bool> UpdateTeamMemberRoleAsync(Guid userId, Guid companyId, string newRole)
        {
            var validRoles = new[] { RoleNames.Crew, RoleNames.Leader, RoleNames.Admin };
            if (!validRoles.Contains(newRole, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid role. Must be one of: {string.Join(", ", validRoles)}");
            }

            var userCompany = await _ctx.UserCompany
                .FirstOrDefaultAsync(uc => uc.userId == userId && uc.companyId == companyId);

            if (userCompany == null) return false;

            userCompany.role = newRole;
            await _ctx.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Deactivate a team member (soft delete)
        /// </summary>
        public async Task<bool> DeactivateTeamMemberAsync(Guid userId, Guid companyId)
        {
            var userCompany = await _ctx.UserCompany
                .Include(uc => uc.user)
                .FirstOrDefaultAsync(uc => uc.userId == userId && uc.companyId == companyId);

            if (userCompany == null) return false;

            // Remove from company
            _ctx.UserCompany.Remove(userCompany);
            
            // Check if user has other companies
            var otherCompanies = await _ctx.UserCompany
                .Where(uc => uc.userId == userId && uc.companyId != companyId)
                .CountAsync();

            // If no other companies, deactivate the user
            if (otherCompanies == 0)
            {
                userCompany.user.active = false;
                userCompany.user.updateDatetime = DateTime.UtcNow;
            }

            await _ctx.SaveChangesAsync();
            return true;
        }

    }
}
