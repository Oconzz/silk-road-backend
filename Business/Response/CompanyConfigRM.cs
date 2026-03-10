using System;

namespace Business.Response
{
    public class CompanyConfigRM
    {
        public Guid id { get; set; }
        public Guid companyId { get; set; }
        public string? defaultTermsAndConditions { get; set; }
        
        // Time Card Configuration
        public int lunchDeductionMinutes { get; set; }
        public int workDayStartHour { get; set; }
        public int workDayStartMinute { get; set; }
        public int workDayEndHour { get; set; }
        public int workDayEndMinute { get; set; }
        public string workDays { get; set; } = string.Empty;
        public bool enableClockInReminder { get; set; }
        public bool enableClockOutReminder { get; set; }
        public int reminderMinutesBefore { get; set; }
        public bool requireApproval { get; set; }
        public decimal overtimeThresholdHours { get; set; }
        public decimal weeklyOvertimeThresholdHours { get; set; }
        
        public DateTime createDatetime { get; set; }
        public DateTime updateDatetime { get; set; }
    }
}
