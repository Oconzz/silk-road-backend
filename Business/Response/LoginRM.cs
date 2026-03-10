using Business.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Response
{
    public class LoginRM
    {
        public Guid userId { get; set; }
        public string token { get; set; }
        public string email { get; set; }
        public string errorMessage { get; set; }
        public DateTime? tokenExpiration { get; set; }
        public bool changePasswordRequired { get; set; }
        // List of companies the user is assigned to with their roles
        public List<CompanyAssignment> companies { get; set; } = new List<CompanyAssignment>();
    }

    // Represents a company assignment for a user
    public class CompanyAssignment
    {
        public Guid companyId { get; set; }
        public string companyName { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
        public string? companyLogo { get; set; }
        public string? companyLogoThumbnail { get; set; }
    }

    // Legacy - kept for backward compatibility
    public class ClientInfo
    {
        public Guid clientId { get; set; }
        public string name { get; set; } = string.Empty;
        public string ckId { get; set; } = string.Empty;
        public bool isPotentialClient { get; set; }
        public bool active { get; set; }
    }
}