using System;

namespace Business.ViewModel;

public class ClockInVM
{
    public Guid? jobId { get; set; }
    public double? latitude { get; set; }
    public double? longitude { get; set; }
    public bool isAutoClockIn { get; set; }
}

public class ClockOutVM
{
    public Guid timeCardId { get; set; }
    public double? latitude { get; set; }
    public double? longitude { get; set; }
    public bool isAutoClockOut { get; set; }
    public string? notes { get; set; }
}

public class UpdateTimeCardVM
{
    public Guid id { get; set; }
    public DateTime? clockInTime { get; set; }
    public DateTime? clockOutTime { get; set; }
    public decimal? breakMinutes { get; set; }
    public string? notes { get; set; }
}

public class ApproveTimeCardVM
{
    public Guid timeCardId { get; set; }
    public bool approved { get; set; }
    public string? rejectionReason { get; set; }
}

public class BulkApproveTimeCardsVM
{
    public List<Guid> timeCardIds { get; set; } = new();
    public bool approved { get; set; }
    public string? rejectionReason { get; set; }
}

public class AddBreakVM
{
    public Guid timeCardId { get; set; }
    public decimal breakMinutes { get; set; }
}

public class TimeCardListRequestVM
{
    public Guid? userId { get; set; }
    public Guid? jobId { get; set; }
    public DateTime? startDate { get; set; }
    public DateTime? endDate { get; set; }
    public string? status { get; set; } // pending, approved, rejected, all
    public bool viewTeam { get; set; } = false; // If true, admin/leader can see all team timecards
    public int pageNumber { get; set; } = 1;
    public int pageSize { get; set; } = 50;
}

public class TimeCardExportRequestVM
{
    public Guid? userId { get; set; }
    public Guid? jobId { get; set; }
    public DateTime startDate { get; set; }
    public DateTime endDate { get; set; }
    public string format { get; set; } = "csv"; // csv, pdf
}

public class TimeCardReportRequestVM
{
    public DateTime startDate { get; set; }
    public DateTime endDate { get; set; }
    public Guid? userId { get; set; }
    public Guid? jobId { get; set; }
    public string? status { get; set; } // pending, approved, rejected
    public int pageNumber { get; set; } = 1;
    public int pageSize { get; set; } = 50;
}

public class TimeCardsByJobReportRM
{
    public Guid jobId { get; set; }
    public string jobName { get; set; } = string.Empty;
    public decimal totalHours { get; set; }
    public decimal totalBillableHours { get; set; }
    public decimal totalCost { get; set; }
    public int timeCardCount { get; set; }
}

public class TimeCardsByUserReportRM
{
    public Guid userId { get; set; }
    public string userName { get; set; } = string.Empty;
    public decimal totalHours { get; set; }
    public decimal totalBillableHours { get; set; }
    public decimal totalOvertimeHours { get; set; }
    public int daysWorked { get; set; }
}

public class PayrollReportVM
{
    public DateTime startDate { get; set; }
    public DateTime endDate { get; set; }
    public List<PayrollItemVM> items { get; set; } = new();
}

public class PayrollItemVM
{
    public Guid userId { get; set; }
    public string userName { get; set; } = string.Empty;
    public string userEmail { get; set; } = string.Empty;
    public decimal regularHours { get; set; }
    public decimal overtimeHours { get; set; }
    public decimal totalBillableHours { get; set; }
    public int daysWorked { get; set; }
    public List<Guid> timeCardIds { get; set; } = new();
}
