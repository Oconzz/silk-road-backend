using System;

namespace Business.Response
{
    public class WarehouseRM
    {
        public Guid id { get; set; }
        public Guid companyId { get; set; }
        public string name { get; set; } = string.Empty;
        public string location { get; set; } = string.Empty;
        
        // Structured address fields
        public string addressLine1 { get; set; } = string.Empty;
        public string addressLine2 { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string state { get; set; } = string.Empty;
        public string zipcode { get; set; } = string.Empty;
        public string country { get; set; } = string.Empty;
        
        public double latitude { get; set; }
        public double longitude { get; set; }
        public int totalInventoryItems { get; set; }
        public DateTime createDatetime { get; set; }
        public DateTime updateDatetime { get; set; }
    }

    public class WarehouseListItem
    {
        public Guid id { get; set; }
        public Guid companyId { get; set; }
        public string name { get; set; } = string.Empty;
        public string location { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string state { get; set; } = string.Empty;
        public int totalInventoryItems { get; set; }
        public DateTime createDatetime { get; set; }
    }
}
