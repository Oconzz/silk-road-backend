using System;

namespace Business.Response;

public class TimeCardRM
{
    public Guid id { get; set; }
    public Guid userId { get; set; }
    public string userName { get; set; } = string.Empty;
    public Guid companyId { get; set; }
    public Guid? jobId { get; set; }
    public string? jobName { get; set; }
    public DateTime clockInTime { get; set; }
    public DateTime? clockOutTime { get; set; }
    public decimal regularHours { get; set; }
    public decimal overtimeHours { get; set; }
    public decimal totalHours { get; set; }
    public decimal breakMinutes { get; set; }
    public decimal billableHours { get; set; }
    public string? notes { get; set; }
    public string status { get; set; } = "pending";
    public Guid? approvedByUserId { get; set; }
    public string? approvedByUserName { get; set; }
    public DateTime? approvedDatetime { get; set; }
    public string? rejectionReason { get; set; }
    public bool isAutoClockIn { get; set; }
    public bool isAutoClockOut { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

public class TimeCardListItem
{
    public Guid id { get; set; }
    public Guid userId { get; set; }
    public string userName { get; set; } = string.Empty;
    public string? jobName { get; set; }
    public DateTime clockInTime { get; set; }
    public DateTime? clockOutTime { get; set; }
    public decimal totalHours { get; set; }
    public decimal overtimeHours { get; set; }
    public decimal billableHours { get; set; }
    public string status { get; set; } = "pending";
    public Guid? jobId { get; set; }
}

public class TimeCardSummaryRM
{
    public Guid userId { get; set; }
    public string userName { get; set; } = string.Empty;
    public DateTime startDate { get; set; }
    public DateTime endDate { get; set; }
    public decimal totalRegularHours { get; set; }
    public decimal totalOvertimeHours { get; set; }
    public decimal totalHours { get; set; }
    public int totalDays { get; set; }
}

public class ActiveTimeCardRM
{
    public Guid id { get; set; }
    public Guid? jobId { get; set; }
    public string? jobName { get; set; }
    public DateTime clockInTime { get; set; }
    public decimal elapsedHours { get; set; }
}
