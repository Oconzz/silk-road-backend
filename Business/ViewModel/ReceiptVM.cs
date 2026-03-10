using System;

namespace Business.ViewModel
{
    public class CreateReceiptVM
    {
        /// <summary>
        /// Base64 encoded image data (optionally with data URL prefix like "data:image/jpeg;base64,...")
        /// Max file size: 10MB
        /// Allowed formats: JPEG, PNG, WEBP, GIF, BMP, TIFF
        /// </summary>
        public string receiptBase64 { get; set; } = string.Empty;
        
        /// <summary>
        /// Original filename from the client (for display purposes)
        /// </summary>
        public string fileName { get; set; } = string.Empty;
    }

    public class UpdateReceiptVM
    {
        public Guid id { get; set; }
        
        /// <summary>
        /// Optional: Update with new receipt image (base64 encoded)
        /// </summary>
        public string? receiptBase64 { get; set; }
        
        /// <summary>
        /// Optional: Update filename
        /// </summary>
        public string? fileName { get; set; }
    }

    public class ReceiptListRequestVM
    {
        public int pageNumber { get; set; } = 1;
        public int pageSize { get; set; } = 10;
        public string? searchTerm { get; set; }
        public DateTime? fromDate { get; set; }
        public DateTime? toDate { get; set; }
    }
}
