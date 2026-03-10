using System;

namespace Business.ViewModel
{
    public class CreateClientVM
    {
        public string contactName { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string companyName { get; set; } = string.Empty;
        public CreateLocationVM? location { get; set; }
    }

    public class UpdateClientVM
    {
        public Guid id { get; set; }
        public string? contactName { get; set; }
        public string? phone { get; set; }
        public string? email { get; set; }
        public string? companyName { get; set; }
        public UpdateLocationVM? location { get; set; }
    }

    public class ClientListRequestVM
    {
        public int pageNumber { get; set; } = 1;
        public int pageSize { get; set; } = 10;
        public string? searchTerm { get; set; }
    }

    // Bulk client creation request model
    public class BulkCreateClientVM
    {
        public List<CreateClientVM> clients { get; set; } = new List<CreateClientVM>();
    }

    // Individual result item for bulk client creation
    public class BulkClientResultItem
    {
        public int index { get; set; }
        public Guid? id { get; set; }
        public string contactName { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;  // "created", "duplicate", "error"
        public string? errorMessage { get; set; }
        public string? duplicateField { get; set; }  // "email" or "phone"
        public Guid? existingClientId { get; set; }
    }

    // Response model for bulk client creation
    public class BulkCreateClientResultVM
    {
        public int totalRequested { get; set; }
        public int totalCreated { get; set; }
        public int totalDuplicates { get; set; }
        public int totalErrors { get; set; }
        public List<BulkClientResultItem> results { get; set; } = new List<BulkClientResultItem>();
    }
}
