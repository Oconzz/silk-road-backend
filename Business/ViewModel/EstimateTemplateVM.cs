using System;
using System.Collections.Generic;

namespace Business.ViewModel
{
    public class CreateEstimateTemplateVM
    {
        public string name { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public List<CreateEstimateItemVM>? items { get; set; }
    }

    public class UpdateEstimateTemplateVM
    {
        public Guid id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public List<CreateEstimateItemVM>? items { get; set; }
    }

    public class EstimateTemplateListRequestVM
    {
        public int pageNumber { get; set; } = 1;
        public int pageSize { get; set; } = 10;
        public string? searchTerm { get; set; }
    }

    public class CreateTemplateFromEstimateVM
    {
        public Guid estimateId { get; set; }
        public string name { get; set; } = string.Empty;
        public string? description { get; set; }
    }
}
