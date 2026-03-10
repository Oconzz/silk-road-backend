using System;

namespace Business.Response
{
    public class LocationRM
    {
        public Guid id { get; set; }
        public Guid companyId { get; set; }
        public Guid? clientId { get; set; }
        public string name { get; set; } = string.Empty;
        public string addressLine1 { get; set; } = string.Empty;
        public string addressLine2 { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string state { get; set; } = string.Empty;
        public string zipcode { get; set; } = string.Empty;
        public string country { get; set; } = string.Empty;
        public double latitude { get; set; }
        public double longitude { get; set; }
        public bool isPrimary { get; set; }
        public DateTime createDatetime { get; set; }
        public DateTime updateDatetime { get; set; }
    }

    public class LocationListItem
    {
        public Guid id { get; set; }
        public Guid companyId { get; set; }
        public Guid? clientId { get; set; }
        public string name { get; set; } = string.Empty;
        public string addressLine1 { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string state { get; set; } = string.Empty;
        public string zipcode { get; set; } = string.Empty;
        public bool isPrimary { get; set; }
        public DateTime createDatetime { get; set; }
    }
}
