using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Business.Model;

public class CompanyConfig
{
    public Guid id { get; set; }
    public Guid companyId { get; set; }
    public string? defaultTermsAndConditions { get; set; }
    
    // Time Card Configuration
    public int lunchDeductionMinutes { get; set; } = 60; // Default 1 hour
    public int workDayStartHour { get; set; } = 7; // Default 7 AM
    public int workDayStartMinute { get; set; } = 30; // Default 7:30 AM
    public int workDayEndHour { get; set; } = 15; // Default 3 PM
    public int workDayEndMinute { get; set; } = 30; // Default 3:30 PM
    public string workDays { get; set; } = "1,2,3,4,5"; // Mon-Fri (1=Mon, 7=Sun)
    public bool enableClockInReminder { get; set; } = true;
    public bool enableClockOutReminder { get; set; } = true;
    public int reminderMinutesBefore { get; set; } = 5; // 5 minutes before
    public bool requireApproval { get; set; } = true; // Timecards need approval
    public decimal overtimeThresholdHours { get; set; } = 8; // Hours before OT kicks in
    public decimal weeklyOvertimeThresholdHours { get; set; } = 40; // Weekly OT threshold
    public bool enableAutoClockIn { get; set; } = false;
    public double autoClockInRadiusMeters { get; set; } = 100; // Default 100m radius
    
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

public class CompanyConfigConfiguration : IEntityTypeConfiguration<CompanyConfig>
{
    public void Configure(EntityTypeBuilder<CompanyConfig> builder)
    {
        builder.HasKey("id");
        builder.Property(c => c.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.companyId)
            .IsRequired(true);
        builder.Property(c => c.defaultTermsAndConditions)
            .HasMaxLength(5000);
        
        builder.Property(c => c.lunchDeductionMinutes)
            .IsRequired(true)
            .HasDefaultValue(60);
        
        builder.Property(c => c.workDayStartHour)
            .IsRequired(true)
            .HasDefaultValue(7);
        
        builder.Property(c => c.workDayStartMinute)
            .IsRequired(true)
            .HasDefaultValue(30);
        
        builder.Property(c => c.workDayEndHour)
            .IsRequired(true)
            .HasDefaultValue(15);
        
        builder.Property(c => c.workDayEndMinute)
            .IsRequired(true)
            .HasDefaultValue(30);
        
        builder.Property(c => c.workDays)
            .IsRequired(true)
            .HasMaxLength(20)
            .HasDefaultValue("1,2,3,4,5");
        
        builder.Property(c => c.enableClockInReminder)
            .IsRequired(true)
            .HasDefaultValue(true);
        
        builder.Property(c => c.enableClockOutReminder)
            .IsRequired(true)
            .HasDefaultValue(true);
        
        builder.Property(c => c.reminderMinutesBefore)
            .IsRequired(true)
            .HasDefaultValue(5);
        
        builder.Property(c => c.requireApproval)
            .IsRequired(true)
            .HasDefaultValue(true);
        
        builder.Property(c => c.overtimeThresholdHours)
            .IsRequired(true)
            .HasPrecision(5, 2)
            .HasDefaultValue(8);
        
        builder.Property(c => c.weeklyOvertimeThresholdHours)
            .IsRequired(true)
            .HasPrecision(5, 2)
            .HasDefaultValue(40);
        
        builder.Property(c => c.enableAutoClockIn)
            .IsRequired(true)
            .HasDefaultValue(false);
        
        builder.Property(c => c.autoClockInRadiusMeters)
            .IsRequired(true)
            .HasDefaultValue(100);
        
        builder.Property(c => c.createDatetime)
            .IsRequired(true);
        builder.Property(c => c.updateDatetime)
            .IsRequired(true);

        // Create unique index on companyId to ensure one config per company
        builder.HasIndex(c => c.companyId)
            .IsUnique();
    }
}
