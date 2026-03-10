using System;
using System.Collections.Generic;
using System.Linq;

namespace Business.Response
{
    public class ClientRM
    {
        public Guid id { get; set; }
        public Guid companyId { get; set; }
        public string contactName { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string companyName { get; set; } = string.Empty;
        
        // Multiple locations (1:N relationship)
        public List<LocationRM>? locations { get; set; }
        
        // Legacy field for backward compatibility - returns first location
        public LocationRM? location => locations?.FirstOrDefault();
        
        public DateTime createDatetime { get; set; }
        public DateTime updateDatetime { get; set; }
    }

    public class ClientListItem
    {
        public Guid id { get; set; }
        public Guid companyId { get; set; }
        public string contactName { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string companyName { get; set; } = string.Empty;
        public string? primaryAddress { get; set; }
        public DateTime createDatetime { get; set; }
    }
}
