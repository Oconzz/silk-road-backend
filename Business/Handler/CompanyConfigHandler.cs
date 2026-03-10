using Business.Data;
using Business.Model;
using Business.Response;
using Business.ViewModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Business.Handlers
{
    public class CompanyConfigHandler
    {
        private readonly DBContext _ctx;

        public CompanyConfigHandler(DBContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<CompanyConfigRM?> GetCompanyConfigAsync(Guid companyId)
        {
            var config = await _ctx.CompanyConfig
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.companyId == companyId);

            if (config == null)
                return null;

            return new CompanyConfigRM
            {
                id = config.id,
                companyId = config.companyId,
                defaultTermsAndConditions = config.defaultTermsAndConditions,
                lunchDeductionMinutes = config.lunchDeductionMinutes,
                workDayStartHour = config.workDayStartHour,
                workDayStartMinute = config.workDayStartMinute,
                workDayEndHour = config.workDayEndHour,
                workDayEndMinute = config.workDayEndMinute,
                workDays = config.workDays,
                enableClockInReminder = config.enableClockInReminder,
                enableClockOutReminder = config.enableClockOutReminder,
                reminderMinutesBefore = config.reminderMinutesBefore,
                requireApproval = config.requireApproval,
                overtimeThresholdHours = config.overtimeThresholdHours,
                weeklyOvertimeThresholdHours = config.weeklyOvertimeThresholdHours,
                createDatetime = config.createDatetime,
                updateDatetime = config.updateDatetime
            };
        }

        public async Task<CompanyConfigRM> CreateCompanyConfigAsync(CreateCompanyConfigVM vm, Guid companyId)
        {
            // Check if config already exists for this company
            var existingConfig = await _ctx.CompanyConfig
                .FirstOrDefaultAsync(c => c.companyId == companyId);

            if (existingConfig != null)
            {
                throw new InvalidOperationException("Company configuration already exists. Use update endpoint instead.");
            }

            var now = DateTime.UtcNow;

            var config = new CompanyConfig
            {
                id = Guid.NewGuid(),
                companyId = companyId,
                defaultTermsAndConditions = vm.defaultTermsAndConditions,
                lunchDeductionMinutes = vm.lunchDeductionMinutes,
                workDayStartHour = vm.workDayStartHour,
                workDayStartMinute = vm.workDayStartMinute,
                workDayEndHour = vm.workDayEndHour,
                workDayEndMinute = vm.workDayEndMinute,
                workDays = vm.workDays,
                enableClockInReminder = vm.enableClockInReminder,
                enableClockOutReminder = vm.enableClockOutReminder,
                reminderMinutesBefore = vm.reminderMinutesBefore,
                requireApproval = vm.requireApproval,
                overtimeThresholdHours = vm.overtimeThresholdHours,
                weeklyOvertimeThresholdHours = vm.weeklyOvertimeThresholdHours,
                createDatetime = now,
                updateDatetime = now
            };

            _ctx.CompanyConfig.Add(config);
            await _ctx.SaveChangesAsync();

            return new CompanyConfigRM
            {
                id = config.id,
                companyId = config.companyId,
                defaultTermsAndConditions = config.defaultTermsAndConditions,
                lunchDeductionMinutes = config.lunchDeductionMinutes,
                workDayStartHour = config.workDayStartHour,
                workDayStartMinute = config.workDayStartMinute,
                workDayEndHour = config.workDayEndHour,
                workDayEndMinute = config.workDayEndMinute,
                workDays = config.workDays,
                enableClockInReminder = config.enableClockInReminder,
                enableClockOutReminder = config.enableClockOutReminder,
                reminderMinutesBefore = config.reminderMinutesBefore,
                requireApproval = config.requireApproval,
                overtimeThresholdHours = config.overtimeThresholdHours,
                weeklyOvertimeThresholdHours = config.weeklyOvertimeThresholdHours,
                createDatetime = config.createDatetime,
                updateDatetime = config.updateDatetime
            };
        }

        public async Task<CompanyConfigRM?> UpdateCompanyConfigAsync(UpdateCompanyConfigVM vm, Guid companyId)
        {
            var config = await _ctx.CompanyConfig
                .FirstOrDefaultAsync(c => c.companyId == companyId);

            if (config == null)
                return null;

            if (vm.defaultTermsAndConditions != null)
                config.defaultTermsAndConditions = vm.defaultTermsAndConditions;
            
            if (vm.lunchDeductionMinutes.HasValue)
                config.lunchDeductionMinutes = vm.lunchDeductionMinutes.Value;
            
            if (vm.workDayStartHour.HasValue)
                config.workDayStartHour = vm.workDayStartHour.Value;
            
            if (vm.workDayStartMinute.HasValue)
                config.workDayStartMinute = vm.workDayStartMinute.Value;
            
            if (vm.workDayEndHour.HasValue)
                config.workDayEndHour = vm.workDayEndHour.Value;
            
            if (vm.workDayEndMinute.HasValue)
                config.workDayEndMinute = vm.workDayEndMinute.Value;
            
            if (vm.workDays != null)
                config.workDays = vm.workDays;
            
            if (vm.enableClockInReminder.HasValue)
                config.enableClockInReminder = vm.enableClockInReminder.Value;
            
            if (vm.enableClockOutReminder.HasValue)
                config.enableClockOutReminder = vm.enableClockOutReminder.Value;
            
            if (vm.reminderMinutesBefore.HasValue)
                config.reminderMinutesBefore = vm.reminderMinutesBefore.Value;
            
            if (vm.requireApproval.HasValue)
                config.requireApproval = vm.requireApproval.Value;
            
            if (vm.overtimeThresholdHours.HasValue)
                config.overtimeThresholdHours = vm.overtimeThresholdHours.Value;
            
            if (vm.weeklyOvertimeThresholdHours.HasValue)
                config.weeklyOvertimeThresholdHours = vm.weeklyOvertimeThresholdHours.Value;
            
            config.updateDatetime = DateTime.UtcNow;

            _ctx.CompanyConfig.Update(config);
            await _ctx.SaveChangesAsync();

            return new CompanyConfigRM
            {
                id = config.id,
                companyId = config.companyId,
                defaultTermsAndConditions = config.defaultTermsAndConditions,
                lunchDeductionMinutes = config.lunchDeductionMinutes,
                workDayStartHour = config.workDayStartHour,
                workDayStartMinute = config.workDayStartMinute,
                workDayEndHour = config.workDayEndHour,
                workDayEndMinute = config.workDayEndMinute,
                workDays = config.workDays,
                enableClockInReminder = config.enableClockInReminder,
                enableClockOutReminder = config.enableClockOutReminder,
                reminderMinutesBefore = config.reminderMinutesBefore,
                requireApproval = config.requireApproval,
                overtimeThresholdHours = config.overtimeThresholdHours,
                weeklyOvertimeThresholdHours = config.weeklyOvertimeThresholdHours,
                createDatetime = config.createDatetime,
                updateDatetime = config.updateDatetime
            };
        }

        public async Task<bool> DeleteCompanyConfigAsync(Guid companyId)
        {
            var config = await _ctx.CompanyConfig
                .FirstOrDefaultAsync(c => c.companyId == companyId);

            if (config == null)
                return false;

            _ctx.CompanyConfig.Remove(config);
            await _ctx.SaveChangesAsync();

            return true;
        }
    }
}
