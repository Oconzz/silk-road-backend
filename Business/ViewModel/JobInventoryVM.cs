using System;

namespace Business.ViewModel
{
    public class CreateJobInventoryVM
    {
        public Guid? productId { get; set; }
        public Guid? warehouseId { get; set; }
        public Guid? inventoryId { get; set; }
        public decimal idealQuantity { get; set; }
        public decimal actualQuantity { get; set; }
        public decimal price { get; set; }
        public decimal labor { get; set; }
        public string title { get; set; } = string.Empty;
        public string? description { get; set; }
        public string? barcode { get; set; }
        public string? source { get; set; }
    }

    public class UpdateJobInventoryVM
    {
        public Guid id { get; set; }
        public Guid? productId { get; set; }
        public Guid? warehouseId { get; set; }
        public Guid? inventoryId { get; set; }
        public decimal idealQuantity { get; set; }
        public decimal actualQuantity { get; set; }
        public decimal price { get; set; }
        public decimal labor { get; set; }
        public string title { get; set; } = string.Empty;
        public string? description { get; set; }
        public string? barcode { get; set; }
        public string? source { get; set; }
    }
}
