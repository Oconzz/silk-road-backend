using Business.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.ViewModel
{
    public class UsersVM
    {
        public Guid id { get; set; }
        public string fullName { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public string phone { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string zipcode { get; set; }
        public string country { get; set; }
        public string? verificationToken { get; set; }
        public bool active { get; set; }
        public bool? verified { get; set; }

        public DateTime CreateDatetime { get; set; }
    }

    public class UsersOutVM
    {
        public Guid id { get; set; }
        public string fullName { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string zipcode { get; set; }
        public string country { get; set; }
        public bool active { get; set; }
        public bool? verified { get; set; }

        public DateTime createDatetime { get; set; }
        public DateTime updateDatetime { get; set; }
        public string? personalPhoto { get; set; }
        public string? personalPhotoThumbnail { get; set; }

        // Company assignments for this user (with roles)
        public List<UserCompanyAssignmentInfo> companyAssignments { get; set; } = new List<UserCompanyAssignmentInfo>();
    }

    public class UserCompanyAssignmentInfo
    {
        public Guid companyId { get; set; }
        public string companyName { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
        public DateTime assignedDatetime { get; set; }
    }
}
