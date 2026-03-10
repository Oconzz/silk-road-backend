using System;

namespace Business.Response
{
    public class ProductRM
    {
        public Guid id { get; set; }
        public Guid productId { get; set; }
        public string name { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public decimal price { get; set; }
        public string barcode { get; set; } = string.Empty;
        public DateTime createDatetime { get; set; }
        public DateTime updateDatetime { get; set; }
    }

    public class ProductListItem
    {
        public Guid id { get; set; }
        public Guid productId { get; set; }
        public string name { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public decimal price { get; set; }
        public string barcode { get; set; } = string.Empty;
        public DateTime createDatetime { get; set; }
    }
}
