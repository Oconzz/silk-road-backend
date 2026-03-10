using System;
using System.Collections.Generic;

namespace Business.ViewModel
{
    public class UserListRequestVM
    {
        // Paging
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Search
        public string? SearchTerm { get; set; }

        // Filters
        public bool? Active { get; set; }
        public bool? Verified { get; set; }
        public string? Role { get; set; }

        // Sorting
        // Allowed: fullName, email, createDatetime, updateDatetime, active, verified, city, state, country
        public string? SortBy { get; set; } = "createDatetime";
        public bool SortDesc { get; set; } = true;
    }
}
