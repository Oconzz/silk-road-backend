using System;
using System.Collections.Generic;

namespace Business.Response
{
    public class EstimateTemplateRM
    {
        public Guid id { get; set; }
        public Guid companyId { get; set; }
        public string name { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public List<EstimateItemRM> items { get; set; } = new List<EstimateItemRM>();
        public decimal total { get; set; }
        public DateTime createDatetime { get; set; }
        public DateTime updateDatetime { get; set; }
    }

    public class EstimateTemplateListItem
    {
        public Guid id { get; set; }
        public Guid companyId { get; set; }
        public string name { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public decimal total { get; set; }
        public int inventoryCount { get; set; }
        public DateTime createDatetime { get; set; }
    }
}
