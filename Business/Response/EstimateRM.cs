using System;
using System.Collections.Generic;

namespace Business.Response
{
    public class EstimateRM
    {
        public Guid id { get; set; }
        public Guid companyId { get; set; }
        public string name { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public ClientListItem? client { get; set; }
        public LocationRM? location { get; set; }
        public List<EstimateItemRM> items { get; set; } = new List<EstimateItemRM>();
        public List<EstimateMediaRM> media { get; set; } = new List<EstimateMediaRM>();
        public List<EstimateAlbumRM> albums { get; set; } = new List<EstimateAlbumRM>();
        public decimal total { get; set; }
        public bool sent { get; set; }
        public bool approved { get; set; }
        public DateTime sentDateTime { get; set; }
        public DateTime createDatetime { get; set; }
        public DateTime updateDatetime { get; set; }
        public string? termsAndConditions { get; set; }
        public DateTime? proposedStartDate { get; set; }
        public DateTime? expirationDate { get; set; }
        public string? publicShareId { get; set; }
        public string? publicShareUrl { get; set; }
        public int viewCount { get; set; }
        public DateTime? lastViewedAt { get; set; }

        // Versioning
        public int version { get; set; } = 1;
        public Guid? parentEstimateId { get; set; }
        public bool isLatestVersion { get; set; } = true;
    }

    public class EstimateListItem
    {
        public Guid id { get; set; }
        public Guid companyId { get; set; }
        public string name { get; set; } = string.Empty;
        public Guid? clientId { get; set; }
        public string clientName { get; set; } = string.Empty;
        public string locationCity { get; set; } = string.Empty;
        public decimal total { get; set; }
        public bool sent { get; set; }
        public bool approved { get; set; }
        public DateTime createDatetime { get; set; }
        public int version { get; set; } = 1;
        public bool isLatestVersion { get; set; } = true;
    }
}
