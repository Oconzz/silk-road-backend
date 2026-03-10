using System;
using System.Collections.Generic;

namespace Business.ViewModel
{
    public class CreateEstimateVM
    {
        public string name { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public Guid? clientId { get; set; }
        public Guid? locationId { get; set; }
        public DateTime? proposedStartDate { get; set; }
        public DateTime? expirationDate { get; set; }
        public List<CreateEstimateItemVM>? items { get; set; }
        public string? termsAndConditions { get; set; }
    }

    public class UpdateEstimateVM
    {
        public Guid id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public Guid? clientId { get; set; }
        public Guid? locationId { get; set; }
        public bool? sent { get; set; }
        public bool? approved { get; set; }
        public DateTime? proposedStartDate { get; set; }
        public DateTime? expirationDate { get; set; }
        public List<CreateEstimateItemVM>? items { get; set; }
        public string? termsAndConditions { get; set; }
    }

    public class EstimateListRequestVM
    {
        public int pageNumber { get; set; } = 1;
        public int pageSize { get; set; } = 10;
        public string? searchTerm { get; set; }
        public bool? sent { get; set; }
        public bool? approved { get; set; }
        public Guid? clientId { get; set; }
    }

    public class ConvertEstimateToJobVM
    {
        public Guid estimateId { get; set; }
        public string? jobTitle { get; set; }
        public string? jobDescription { get; set; }
        public string status { get; set; } = "pending";
    }
}
