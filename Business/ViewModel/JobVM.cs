using System;
using System.Collections.Generic;

namespace Business.ViewModel
{
    public class CreateJobVM
    {
        public string title { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public Guid? clientId { get; set; }
        public Guid? locationId { get; set; }
        public Guid? estimateId { get; set; }
        public DateTime? scheduledStartDate { get; set; }
        public DateTime? scheduledEndDate { get; set; }
        public decimal? estimatedTotal { get; set; }
        public decimal? laborCost { get; set; }
        public decimal? materialsCost { get; set; }
        public List<CreateJobInventoryVM>? jobInventory { get; set; }

        // Recurring Job fields
        public bool isRecurring { get; set; }
        public string? recurrenceRule { get; set; }
        public DateTime? recurrenceEndDate { get; set; }
        public string? timeZone { get; set; }
        public string? billingFrequency { get; set; }
    }

    public class UpdateJobVM
    {
        public Guid id { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
        public string? status { get; set; }
        public Guid? clientId { get; set; }
        public Guid? locationId { get; set; }
        public Guid? estimateId { get; set; }
        public DateTime? scheduledStartDate { get; set; }
        public DateTime? scheduledEndDate { get; set; }
        public decimal? estimatedTotal { get; set; }
        public decimal? laborCost { get; set; }
        public decimal? materialsCost { get; set; }
        public List<UpdateJobInventoryVM>? jobInventory { get; set; }

        // Recurring Job fields
        public bool? isRecurring { get; set; }
        public string? recurrenceRule { get; set; }
        public DateTime? recurrenceEndDate { get; set; }
        public string? timeZone { get; set; }
        public string? billingFrequency { get; set; }
    }

    public class SetRecurrenceVM
    {
        public string? recurrenceRule { get; set; }
        public DateTime? recurrenceEndDate { get; set; }
        public string? timeZone { get; set; }
    }

    public class JobListRequestVM
    {
        public int pageNumber { get; set; } = 1;
        public int pageSize { get; set; } = 10;
        public string? searchTerm { get; set; }
        public string? status { get; set; }
        public Guid? clientId { get; set; }
    }
}
