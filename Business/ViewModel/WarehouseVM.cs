using System;

namespace Business.ViewModel
{
    public class CreateWarehouseVM
    {
        public string name { get; set; } = string.Empty;
        public string location { get; set; } = string.Empty; // Legacy - kept for backward compatibility
        
        // Structured address fields
        public string addressLine1 { get; set; } = string.Empty;
        public string addressLine2 { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string state { get; set; } = string.Empty;
        public string zipcode { get; set; } = string.Empty;
        public string country { get; set; } = string.Empty;
        
        public double latitude { get; set; }
        public double longitude { get; set; }
    }

    public class UpdateWarehouseVM
    {
        public Guid id { get; set; }
        public string? name { get; set; }
        public string? location { get; set; }
        
        // Structured address fields
        public string? addressLine1 { get; set; }
        public string? addressLine2 { get; set; }
        public string? city { get; set; }
        public string? state { get; set; }
        public string? zipcode { get; set; }
        public string? country { get; set; }
        
        public double? latitude { get; set; }
        public double? longitude { get; set; }
    }

    public class WarehouseListRequestVM
    {
        public int pageNumber { get; set; } = 1;
        public int pageSize { get; set; } = 10;
        public string? searchTerm { get; set; }
    }
}
