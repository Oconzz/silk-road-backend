using System;
using System.Collections.Generic;

namespace Business.Response
{
    public class PagedResponse<T>
    {
        public IEnumerable<T> Items { get; set; } = Array.Empty<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public string? SortBy { get; set; }
        public bool SortDesc { get; set; }
        public string? SearchTerm { get; set; }
    }
}
