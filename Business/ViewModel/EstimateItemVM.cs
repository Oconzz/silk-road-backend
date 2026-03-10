using System;
using System.Collections.Generic;

namespace Business.ViewModel
{
    public class CreateEstimateItemVM
    {
        public Guid productId { get; set; }
        public Guid warehouseId { get; set; }
        public Guid? inventoryId { get; set; }
        public decimal quantity { get; set; }
        public decimal price { get; set; }
        public decimal labor { get; set; }
        public string title { get; set; } = string.Empty;
        public string? description { get; set; }
        public string? barcode { get; set; }
        public string? source { get; set; }
        public List<Guid>? mediaIds { get; set; }
    }

    public class UpdateEstimateItemVM
    {
        public Guid id { get; set; }
        public Guid? productId { get; set; }
        public Guid? warehouseId { get; set; }
        public Guid? inventoryId { get; set; }
        public decimal? quantity { get; set; }
        public decimal? price { get; set; }
        public decimal? labor { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
        public string? barcode { get; set; }
        public string? source { get; set; }
    }
}
