using System;
using System.Collections.Generic;

namespace Business.Response
{
    public class JobRM
    {
        public Guid id { get; set; }
        public Guid companyId { get; set; }
        public string? jobNumber { get; set; }
        public string title { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public Guid? estimateId { get; set; }
        public string? estimateTitle { get; set; }
        public DateTime? scheduledStartDate { get; set; }
        public DateTime? scheduledEndDate { get; set; }
        public decimal estimatedTotal { get; set; }
        public decimal laborCost { get; set; }
        public decimal materialsCost { get; set; }
        public ClientListItem? client { get; set; }
        public LocationRM? location { get; set; }
        public List<JobInventoryRM>? jobInventory { get; set; }
        public List<JobMediaRM>? media { get; set; }
        public List<JobAlbumRM>? albums { get; set; }
        public List<AssignedTeamMemberRM>? assignments { get; set; }
        public List<JobNoteListItem>? notes { get; set; }

        // Recurring Job fields
        public bool isRecurring { get; set; }
        public string? recurrenceRule { get; set; }
        public DateTime? recurrenceEndDate { get; set; }
        public string? timeZone { get; set; }
        public DateTime? lastOccurrenceCreated { get; set; }
        public string? billingFrequency { get; set; }

        // Parent/Child recurring relationship
        public Guid? parentJobId { get; set; }
        public string? parentJobTitle { get; set; }
        public DateTime? occurrenceDate { get; set; }
        public int childJobsCount { get; set; }

        // Google Calendar Integration (NMJ-40, NMJ-41)
        public string? googleCalendarEventId { get; set; }

        public DateTime createDatetime { get; set; }
        public DateTime updateDatetime { get; set; }
    }

    public class JobListItem
    {
        public Guid id { get; set; }
        public Guid companyId { get; set; }
        public string title { get; set; } = string.Empty;
        public string? jobNumber { get; set; }
        public string status { get; set; } = string.Empty;
        public Guid? clientId { get; set; }
        public string clientName { get; set; } = string.Empty;
        public string locationCity { get; set; } = string.Empty;
        public DateTime? scheduledStartDate { get; set; }
        public decimal? estimatedTotal { get; set; }
        public Guid? estimateId { get; set; }
        public string? estimateTitle { get; set; }
        public bool isRecurring { get; set; }
        public Guid? parentJobId { get; set; }
        public DateTime? occurrenceDate { get; set; }
        public int childJobsCount { get; set; }
        public int? completionPercentage { get; set; }
        public DateTime createDatetime { get; set; }
    }
}
