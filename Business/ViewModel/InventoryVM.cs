using System;

namespace Business.ViewModel
{
    public class CreateInventoryVM
    {
        public Guid productId { get; set; }
        public Guid warehouseId { get; set; }
        public int quantity { get; set; }
        public decimal price { get; set; }
        public string title { get; set; } = string.Empty;
        public string? description { get; set; }
        public string? barcode { get; set; }
        public string? imageS3Key { get; set; }
        public string? category { get; set; }
        public string? source { get; set; }
    }

    public class UpdateInventoryVM
    {
        public Guid id { get; set; }
        public int? quantity { get; set; }
        public decimal? price { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
        public string? barcode { get; set; }
        public string? category { get; set; }
        public string? imageS3Key { get; set; }
    }

    public class MoveInventoryVM
    {
        public Guid inventoryId { get; set; }
        public Guid sourceWarehouseId { get; set; }
        public Guid targetWarehouseId { get; set; }
        public int quantityToMove { get; set; }
    }

    public class InventoryListRequestVM
    {
        public int pageNumber { get; set; } = 1;
        public int pageSize { get; set; } = 10;
        public string? searchTerm { get; set; }
        public Guid? warehouseId { get; set; }
        public Guid? productId { get; set; }
    }

    public class AddQuantityVM
    {
        public Guid inventoryId { get; set; }
        public decimal quantity { get; set; }
        public int? returnDays { get; set; } = 30;
        public string? notes { get; set; }
    }
}
