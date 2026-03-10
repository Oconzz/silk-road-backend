using Business.Data;
using Business.Model;
using Business.Response;
using Business.ViewModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Handlers;

public class TimeCardHandler
{
    private readonly DBContext _context;

    public TimeCardHandler(DBContext context)
    {
        _context = context;
    }

    public async Task<TimeCardRM> ClockInAsync(ClockInVM request, Guid companyId, Guid userId)
    {
        // Check if user already has an active time card
        var activeCard = await _context.TimeCards
            .FirstOrDefaultAsync(t => t.userId == userId && t.companyId == companyId && t.clockOutTime == null);

        if (activeCard != null)
        {
            throw new InvalidOperationException("User already has an active time card. Please clock out first.");
        }

        var companyConfig = await _context.CompanyConfig
            .FirstOrDefaultAsync(c => c.companyId == companyId);

        var timeCard = new TimeCard
        {
            id = Guid.NewGuid(),
            userId = userId,
            companyId = companyId,
            jobId = request.jobId,
            clockInTime = DateTime.UtcNow,
            regularHours = 0,
            overtimeHours = 0,
            totalHours = 0,
            breakMinutes = 0,
            billableHours = 0,
            status = companyConfig?.requireApproval == true ? "pending" : "approved",
            isAutoClockIn = request.isAutoClockIn,
            clockInLatitude = request.latitude,
            clockInLongitude = request.longitude,
            createDatetime = DateTime.UtcNow,
            updateDatetime = DateTime.UtcNow
        };

        _context.TimeCards.Add(timeCard);
        await _context.SaveChangesAsync();

        return await GetTimeCardByIdAsync(timeCard.id, companyId) 
            ?? throw new InvalidOperationException("Failed to retrieve created time card");
    }

    public async Task<TimeCardRM> ClockOutAsync(ClockOutVM request, Guid companyId, Guid userId)
    {
        var timeCard = await _context.TimeCards
            .FirstOrDefaultAsync(t => t.id == request.timeCardId && t.userId == userId && t.companyId == companyId);

        if (timeCard == null)
        {
            throw new InvalidOperationException("Time card not found");
        }

        if (timeCard.clockOutTime != null)
        {
            throw new InvalidOperationException("Time card already clocked out");
        }

        timeCard.clockOutTime = DateTime.UtcNow;
        timeCard.isAutoClockOut = request.isAutoClockOut;
        timeCard.clockOutLatitude = request.latitude;
        timeCard.clockOutLongitude = request.longitude;
        timeCard.notes = request.notes;
        timeCard.updateDatetime = DateTime.UtcNow;

        // Calculate hours
        var totalMinutes = (timeCard.clockOutTime.Value - timeCard.clockInTime).TotalMinutes;
        timeCard.totalHours = (decimal)(totalMinutes / 60.0);
        
        // Get company config for automatic deductions and overtime thresholds
        var companyConfig = await _context.CompanyConfig
            .FirstOrDefaultAsync(c => c.companyId == companyId);

        var lunchDeductionMinutes = companyConfig?.lunchDeductionMinutes ?? 60;
        var overtimeThreshold = companyConfig?.overtimeThresholdHours ?? 8;

        // Auto-apply lunch deduction if work time exceeds 6 hours
        if (timeCard.totalHours > 6)
        {
            timeCard.breakMinutes = lunchDeductionMinutes;
        }

        // Calculate billable hours (total - breaks/60)
        var billableMinutes = totalMinutes - (double)timeCard.breakMinutes;
        timeCard.billableHours = (decimal)(billableMinutes / 60.0);

        // Calculate regular vs overtime based on daily threshold
        var regularHours = Math.Min(timeCard.billableHours, overtimeThreshold);
        var overtimeHours = Math.Max(0, timeCard.billableHours - overtimeThreshold);

        timeCard.regularHours = regularHours;
        timeCard.overtimeHours = overtimeHours;

        await _context.SaveChangesAsync();

        return await GetTimeCardByIdAsync(timeCard.id, companyId) 
            ?? throw new InvalidOperationException("Failed to retrieve updated time card");
    }

    public async Task<ActiveTimeCardRM?> GetActiveTimeCardAsync(Guid userId, Guid companyId)
    {
        var activeCard = await _context.TimeCards
            .Where(t => t.userId == userId && t.companyId == companyId && t.clockOutTime == null)
            .Include(t => t.job)
            .FirstOrDefaultAsync();

        if (activeCard == null) return null;

        var elapsedMinutes = (DateTime.UtcNow - activeCard.clockInTime).TotalMinutes;

        return new ActiveTimeCardRM
        {
            id = activeCard.id,
            jobId = activeCard.jobId,
            jobName = activeCard.job?.title,
            clockInTime = activeCard.clockInTime,
            elapsedHours = (decimal)(elapsedMinutes / 60.0)
        };
    }

    public async Task<PagedResponse<TimeCardListItem>> GetTimeCardListAsync(
        TimeCardListRequestVM request, 
        Guid companyId,
        Guid requestingUserId)
    {
        var query = _context.TimeCards
            .Where(t => t.companyId == companyId)
            .AsQueryable();

        // Filter by userId or viewTeam
        // If viewTeam is true AND user provided userId in request, filter by that userId (admin viewing specific user)
        // If viewTeam is false OR no userId in request, filter by requesting user (crew viewing own timecards)
        if (request.viewTeam && request.userId.HasValue)
        {
            query = query.Where(t => t.userId == request.userId.Value);
        }
        else if (!request.viewTeam)
        {
            query = query.Where(t => t.userId == requestingUserId);
        }
        // If viewTeam is true and no userId filter, admin sees all timecards (no filter)

        // Filter by status
        if (!string.IsNullOrEmpty(request.status) && request.status != "all")
        {
            query = query.Where(t => t.status == request.status);
        }

        if (request.jobId.HasValue)
        {
            query = query.Where(t => t.jobId == request.jobId.Value);
        }

        if (request.startDate.HasValue)
        {
            query = query.Where(t => t.clockInTime >= request.startDate.Value);
        }

        if (request.endDate.HasValue)
        {
            query = query.Where(t => t.clockInTime <= request.endDate.Value);
        }

        var totalCount = await query.CountAsync();

        var timeCards = await query
            .OrderByDescending(t => t.clockInTime)
            .Skip((request.pageNumber - 1) * request.pageSize)
            .Take(request.pageSize)
            .Include(t => t.user)
            .Include(t => t.job)
            .Select(t => new TimeCardListItem
            {
                id = t.id,
                userId = t.userId,
                userName = t.user != null ? t.user.fullName : "Unknown",
                jobName = t.job != null ? t.job.title : null,
                clockInTime = t.clockInTime,
                clockOutTime = t.clockOutTime,
                totalHours = t.totalHours,
                overtimeHours = t.overtimeHours,
                billableHours = t.billableHours,
                status = t.status,
                jobId = t.jobId
            })
            .ToListAsync();

        return new PagedResponse<TimeCardListItem>
        {
            Items = timeCards,
            TotalCount = totalCount,
            PageNumber = request.pageNumber,
            PageSize = request.pageSize
        };
    }

    public async Task<TimeCardRM?> GetTimeCardByIdAsync(Guid id, Guid companyId)
    {
        var timeCard = await _context.TimeCards
            .Where(t => t.id == id && t.companyId == companyId)
            .Include(t => t.user)
            .Include(t => t.job)
            .Include(t => t.approvedByUser)
            .FirstOrDefaultAsync();

        if (timeCard == null) return null;

        return new TimeCardRM
        {
            id = timeCard.id,
            userId = timeCard.userId,
            userName = timeCard.user?.fullName ?? "Unknown",
            companyId = timeCard.companyId,
            jobId = timeCard.jobId,
            jobName = timeCard.job?.title,
            clockInTime = timeCard.clockInTime,
            clockOutTime = timeCard.clockOutTime,
            regularHours = timeCard.regularHours,
            overtimeHours = timeCard.overtimeHours,
            totalHours = timeCard.totalHours,
            breakMinutes = timeCard.breakMinutes,
            billableHours = timeCard.billableHours,
            notes = timeCard.notes,
            status = timeCard.status,
            approvedByUserId = timeCard.approvedByUserId,
            approvedByUserName = timeCard.approvedByUser?.fullName,
            approvedDatetime = timeCard.approvedDatetime,
            rejectionReason = timeCard.rejectionReason,
            isAutoClockIn = timeCard.isAutoClockIn,
            isAutoClockOut = timeCard.isAutoClockOut,
            createDatetime = timeCard.createDatetime,
            updateDatetime = timeCard.updateDatetime
        };
    }

    public async Task<TimeCardRM?> UpdateTimeCardAsync(UpdateTimeCardVM request, Guid companyId)
    {
        var timeCard = await _context.TimeCards
            .FirstOrDefaultAsync(t => t.id == request.id && t.companyId == companyId);

        if (timeCard == null) return null;

        if (request.clockInTime.HasValue)
            timeCard.clockInTime = request.clockInTime.Value;

        if (request.clockOutTime.HasValue)
            timeCard.clockOutTime = request.clockOutTime.Value;

        if (request.breakMinutes.HasValue)
            timeCard.breakMinutes = request.breakMinutes.Value;

        if (request.notes != null)
            timeCard.notes = request.notes;

        // Recalculate hours if times changed
        if (timeCard.clockOutTime.HasValue)
        {
            var totalMinutes = (timeCard.clockOutTime.Value - timeCard.clockInTime).TotalMinutes;
            
            var companyConfig = await _context.CompanyConfig
                .FirstOrDefaultAsync(c => c.companyId == companyId);

            var overtimeThreshold = companyConfig?.overtimeThresholdHours ?? 8;

            timeCard.totalHours = (decimal)(totalMinutes / 60.0);
            
            // Calculate billable hours
            var billableMinutes = totalMinutes - (double)timeCard.breakMinutes;
            timeCard.billableHours = (decimal)(billableMinutes / 60.0);

            // Calculate regular vs overtime
            var regularHours = Math.Min(timeCard.billableHours, overtimeThreshold);
            var overtimeHours = Math.Max(0, timeCard.billableHours - overtimeThreshold);

            timeCard.regularHours = regularHours;
            timeCard.overtimeHours = overtimeHours;
        }

        timeCard.updateDatetime = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetTimeCardByIdAsync(timeCard.id, companyId);
    }

    public async Task<bool> DeleteTimeCardAsync(Guid id, Guid companyId)
    {
        var timeCard = await _context.TimeCards
            .FirstOrDefaultAsync(t => t.id == id && t.companyId == companyId);

        if (timeCard == null) return false;

        _context.TimeCards.Remove(timeCard);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<TimeCardSummaryRM> GetTimeCardSummaryAsync(
        Guid userId, 
        DateTime startDate, 
        DateTime endDate, 
        Guid companyId)
    {
        var timeCards = await _context.TimeCards
            .Where(t => t.userId == userId && 
                       t.companyId == companyId && 
                       t.clockInTime >= startDate && 
                       t.clockInTime <= endDate &&
                       t.clockOutTime != null)
            .Include(t => t.user)
            .ToListAsync();

        return new TimeCardSummaryRM
        {
            userId = userId,
            userName = timeCards.FirstOrDefault()?.user?.fullName ?? "Unknown",
            startDate = startDate,
            endDate = endDate,
            totalRegularHours = timeCards.Sum(t => t.regularHours),
            totalOvertimeHours = timeCards.Sum(t => t.overtimeHours),
            totalHours = timeCards.Sum(t => t.totalHours),
            totalDays = timeCards.Select(t => t.clockInTime.Date).Distinct().Count()
        };
    }

    public async Task<TimeCardRM?> ApproveTimeCardAsync(ApproveTimeCardVM request, Guid companyId, Guid approverUserId)
    {
        var timeCard = await _context.TimeCards
            .FirstOrDefaultAsync(t => t.id == request.timeCardId && t.companyId == companyId);

        if (timeCard == null) return null;

        if (timeCard.clockOutTime == null)
        {
            throw new InvalidOperationException("Cannot approve a time card that hasn't been clocked out");
        }

        timeCard.status = request.approved ? "approved" : "rejected";
        timeCard.approvedByUserId = approverUserId;
        timeCard.approvedDatetime = DateTime.UtcNow;
        timeCard.rejectionReason = request.approved ? null : request.rejectionReason;
        timeCard.updateDatetime = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetTimeCardByIdAsync(timeCard.id, companyId);
    }

    public async Task<List<TimeCardRM>> BulkApproveTimeCardsAsync(BulkApproveTimeCardsVM request, Guid companyId, Guid approverUserId)
    {
        var timeCards = await _context.TimeCards
            .Where(t => request.timeCardIds.Contains(t.id) && t.companyId == companyId)
            .ToListAsync();

        foreach (var timeCard in timeCards)
        {
            if (timeCard.clockOutTime == null) continue;

            timeCard.status = request.approved ? "approved" : "rejected";
            timeCard.approvedByUserId = approverUserId;
            timeCard.approvedDatetime = DateTime.UtcNow;
            timeCard.rejectionReason = request.approved ? null : request.rejectionReason;
            timeCard.updateDatetime = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        var results = new List<TimeCardRM>();
        foreach (var timeCard in timeCards)
        {
            var result = await GetTimeCardByIdAsync(timeCard.id, companyId);
            if (result != null) results.Add(result);
        }

        return results;
    }

    public async Task<List<TimeCardsByJobReportRM>> GetTimeCardsByJobReportAsync(
        DateTime startDate, 
        DateTime endDate, 
        Guid companyId)
    {
        var timeCards = await _context.TimeCards
            .Where(t => t.companyId == companyId && 
                       t.clockInTime >= startDate && 
                       t.clockInTime <= endDate &&
                       t.clockOutTime != null &&
                       t.jobId != null)
            .Include(t => t.job)
            .GroupBy(t => new { t.jobId, t.job!.title })
            .Select(g => new TimeCardsByJobReportRM
            {
                jobId = g.Key.jobId!.Value,
                jobName = g.Key.title,
                totalHours = g.Sum(t => t.totalHours),
                totalBillableHours = g.Sum(t => t.billableHours),
                totalCost = 0, // TODO: Calculate based on user hourly rates
                timeCardCount = g.Count()
            })
            .ToListAsync();

        return timeCards;
    }

    public async Task<List<TimeCardsByUserReportRM>> GetTimeCardsByUserReportAsync(
        DateTime startDate, 
        DateTime endDate, 
        Guid companyId)
    {
        var timeCards = await _context.TimeCards
            .Where(t => t.companyId == companyId && 
                       t.clockInTime >= startDate && 
                       t.clockInTime <= endDate &&
                       t.clockOutTime != null)
            .Include(t => t.user)
            .GroupBy(t => new { t.userId, t.user!.fullName })
            .Select(g => new TimeCardsByUserReportRM
            {
                userId = g.Key.userId,
                userName = g.Key.fullName,
                totalHours = g.Sum(t => t.totalHours),
                totalBillableHours = g.Sum(t => t.billableHours),
                totalOvertimeHours = g.Sum(t => t.overtimeHours),
                daysWorked = g.Select(t => t.clockInTime.Date).Distinct().Count()
            })
            .ToListAsync();

        return timeCards;
    }

    public async Task<PayrollReportVM> GetPayrollReportAsync(
        DateTime startDate, 
        DateTime endDate, 
        Guid companyId)
    {
        var timeCards = await _context.TimeCards
            .Where(t => t.companyId == companyId && 
                       t.clockInTime >= startDate && 
                       t.clockInTime <= endDate &&
                       t.clockOutTime != null &&
                       t.status == "approved")
            .Include(t => t.user)
            .ToListAsync();

        var groupedByUser = timeCards
            .GroupBy(t => t.userId)
            .Select(g => new PayrollItemVM
            {
                userId = g.Key,
                userName = g.First().user?.fullName ?? "Unknown",
                userEmail = g.First().user?.email ?? "Unknown",
                regularHours = g.Sum(t => t.regularHours),
                overtimeHours = g.Sum(t => t.overtimeHours),
                totalBillableHours = g.Sum(t => t.billableHours),
                daysWorked = g.Select(t => t.clockInTime.Date).Distinct().Count(),
                timeCardIds = g.Select(t => t.id).ToList()
            })
            .ToList();

        return new PayrollReportVM
        {
            startDate = startDate,
            endDate = endDate,
            items = groupedByUser
        };
    }
}
