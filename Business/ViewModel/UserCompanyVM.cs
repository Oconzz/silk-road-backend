using System;

namespace Business.ViewModel
{
    public class UserCompanyAssignment
    {
        public Guid companyId { get; set; }
        public string companyName { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
        public string? category { get; set; }
        public DateTime assignedDatetime { get; set; }
    }

    public class AssignUserToCompanyVM
    {
        public Guid userId { get; set; }
        public Guid companyId { get; set; }
        public string role { get; set; } = "User";
    }

    public class UpdateUserCompanyRoleVM
    {
        public Guid userId { get; set; }
        public Guid companyId { get; set; }
        public string role { get; set; } = string.Empty;
    }
}
