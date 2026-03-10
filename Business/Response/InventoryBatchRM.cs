using System;

namespace Business.Response
{
    public class InventoryBatchRM
    {
        public Guid id { get; set; }
        public Guid inventoryId { get; set; }
        public decimal quantityAdded { get; set; }
        public decimal quantityRemaining { get; set; }
        public DateTime addedAt { get; set; }
        public DateTime expiresAt { get; set; }
        public bool isExpired { get; set; }
        public int daysUntilExpiration { get; set; }
    }
}
