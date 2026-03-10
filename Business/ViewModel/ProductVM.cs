using System;

namespace Business.ViewModel
{
    public class CreateProductVM
    {
        public string name { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public decimal price { get; set; }
        public string barcode { get; set; } = string.Empty;
    }

    public class UpdateProductVM
    {
        public Guid id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public decimal? price { get; set; }
        public string? barcode { get; set; }
    }

    public class ProductListRequestVM
    {
        public int pageNumber { get; set; } = 1;
        public int pageSize { get; set; } = 10;
        public string? searchTerm { get; set; }
    }
}
