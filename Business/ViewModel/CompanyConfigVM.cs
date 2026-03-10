using System;

namespace Business.ViewModel
{
    public class CreateCompanyConfigVM
    {
        public string? defaultTermsAndConditions { get; set; }
        
        // Time Card Configuration
        public int lunchDeductionMinutes { get; set; } = 60;
        public int workDayStartHour { get; set; } = 7;
        public int workDayStartMinute { get; set; } = 30;
        public int workDayEndHour { get; set; } = 15;
        public int workDayEndMinute { get; set; } = 30;
        public string workDays { get; set; } = "1,2,3,4,5";
        public bool enableClockInReminder { get; set; } = true;
        public bool enableClockOutReminder { get; set; } = true;
        public int reminderMinutesBefore { get; set; } = 5;
        public bool requireApproval { get; set; } = true;
        public decimal overtimeThresholdHours { get; set; } = 8;
        public decimal weeklyOvertimeThresholdHours { get; set; } = 40;
    }

    public class UpdateCompanyConfigVM
    {
        public string? defaultTermsAndConditions { get; set; }
        
        // Time Card Configuration
        public int? lunchDeductionMinutes { get; set; }
        public int? workDayStartHour { get; set; }
        public int? workDayStartMinute { get; set; }
        public int? workDayEndHour { get; set; }
        public int? workDayEndMinute { get; set; }
        public string? workDays { get; set; }
        public bool? enableClockInReminder { get; set; }
        public bool? enableClockOutReminder { get; set; }
        public int? reminderMinutesBefore { get; set; }
        public bool? requireApproval { get; set; }
        public decimal? overtimeThresholdHours { get; set; }
        public decimal? weeklyOvertimeThresholdHours { get; set; }
    }
}
