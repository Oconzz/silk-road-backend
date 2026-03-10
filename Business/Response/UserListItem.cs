using System;
using System.Collections.Generic;

namespace Business.Response
{
    public class UserListItem
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
        public bool verified { get; set; }
        public DateTime createDatetime { get; set; }
        public DateTime updateDatetime { get; set; }
        public string? personalPhoto { get; set; }
        public string? personalPhotoThumbnail { get; set; }
        
        // Company assignments for this user
        public List<UserCompanyInfo> companyAssignments { get; set; } = new List<UserCompanyInfo>();
    }

    public class UserCompanyInfo
    {
        public Guid companyId { get; set; }
        public string companyName { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
    }
}
