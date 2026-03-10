using System;

namespace Business.Response
{
    public class ReceiptRM
    {
        public Guid id { get; set; }
        public Guid companyId { get; set; }
        public string s3Name { get; set; } = string.Empty;
        public string fileName { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
        public double size { get; set; } // in KB
        public string contentType { get; set; } = string.Empty;
        public DateTime createDatetime { get; set; }
        public DateTime updateDatetime { get; set; }
    }

    public class ReceiptListItem
    {
        public Guid id { get; set; }
        public Guid companyId { get; set; }
        public string fileName { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
        public double size { get; set; } // in KB
        public string contentType { get; set; } = string.Empty;
        public DateTime createDatetime { get; set; }
    }
}
