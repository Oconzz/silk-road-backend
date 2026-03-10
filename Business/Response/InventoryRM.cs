using System;
using System.Collections.Generic;

namespace Business.Response
{
    public class InventoryRM
    {
        public Guid id { get; set; }
        public Guid companyId { get; set; }
        public Guid productId { get; set; }
        public Guid warehouseId { get; set; }
        public int quantity { get; set; }
        public decimal price { get; set; }
        public string title { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string barcode { get; set; } = string.Empty;
        public string? category { get; set; }
        public string? imageS3Key { get; set; }
        public string? imageUrl { get; set; }
        public WarehouseBasicInfo? warehouse { get; set; }
        public List<InventoryBatchRM> batches { get; set; } = new();
        public DateTime createDatetime { get; set; }
        public DateTime updateDatetime { get; set; }
    }

    public class WarehouseBasicInfo
    {
        public Guid id { get; set; }
        public string name { get; set; } = string.Empty;
        public string location { get; set; } = string.Empty;
    }

    public class InventoryListItem
    {
        public Guid productId { get; set; }
        public string title { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string barcode { get; set; } = string.Empty;
        public decimal price { get; set; }
        public int totalQuantity { get; set; }
        public string? category { get; set; }
        public string? imageS3Key { get; set; }
        public string? imageUrl { get; set; }
        public List<WarehouseInventoryDetail> warehouseDetails { get; set; } = new();
        public DateTime lastUpdateDatetime { get; set; }
    }

    public class WarehouseInventoryDetail
    {
        public Guid inventoryId { get; set; }
        public Guid warehouseId { get; set; }
        public string warehouseName { get; set; } = string.Empty;
        public string warehouseLocation { get; set; } = string.Empty;
        public int quantity { get; set; }
    }
}
