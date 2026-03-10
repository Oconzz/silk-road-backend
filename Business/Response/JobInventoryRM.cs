using System;

namespace Business.Response
{
    public class JobInventoryRM
    {
        public Guid id { get; set; }
        public Guid companyId { get; set; }
        public Guid jobId { get; set; }
        public Guid? productId { get; set; }
        public Guid? warehouseId { get; set; }
        public Guid? inventoryId { get; set; }
        public decimal idealQuantity { get; set; }
        public decimal actualQuantity { get; set; }
        public decimal quantityDifference { get; set; }
        public decimal price { get; set; }
        public decimal labor { get; set; }
        public string title { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string barcode { get; set; } = string.Empty;
        public string source { get; set; } = string.Empty;
        public WarehouseBasicInfo? warehouse { get; set; }
        public DateTime createDatetime { get; set; }
        public DateTime updateDatetime { get; set; }
    }
}
