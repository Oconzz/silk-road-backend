using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Business.Model;

public class TimeCard
{
    public Guid id { get; set; }
    public Guid userId { get; set; }
    public Guid companyId { get; set; }
    public Guid? jobId { get; set; }
    public DateTime clockInTime { get; set; }
    public DateTime? clockOutTime { get; set; }
    public decimal regularHours { get; set; }
    public decimal overtimeHours { get; set; }
    public decimal totalHours { get; set; }
    public decimal breakMinutes { get; set; } // Lunch/break time in minutes
    public decimal billableHours { get; set; } // totalHours - (breakMinutes/60) - lunchDeduction
    public string? notes { get; set; }
    public string status { get; set; } // pending, approved, rejected
    public Guid? approvedByUserId { get; set; }
    public DateTime? approvedDatetime { get; set; }
    public string? rejectionReason { get; set; }
    public bool isAutoClockIn { get; set; } // Geolocation triggered
    public bool isAutoClockOut { get; set; }
    public double? clockInLatitude { get; set; }
    public double? clockInLongitude { get; set; }
    public double? clockOutLatitude { get; set; }
    public double? clockOutLongitude { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }

    // Navigation properties
    public User? user { get; set; }
    public Job? job { get; set; }
    public User? approvedByUser { get; set; }
}

public class TimeCardConfiguration : IEntityTypeConfiguration<TimeCard>
{
    public void Configure(EntityTypeBuilder<TimeCard> builder)
    {
        builder.HasKey(t => t.id);
        
        builder.Property(t => t.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(t => t.userId)
            .IsRequired(true);
        
        builder.Property(t => t.companyId)
            .IsRequired(true);
        
        builder.Property(t => t.jobId)
            .IsRequired(false);
        
        builder.Property(t => t.clockInTime)
            .IsRequired(true);
        
        builder.Property(t => t.clockOutTime)
            .IsRequired(false);
        
        builder.Property(t => t.regularHours)
            .IsRequired(true)
            .HasPrecision(5, 2)
            .HasDefaultValue(0);
        
        builder.Property(t => t.overtimeHours)
            .IsRequired(true)
            .HasPrecision(5, 2)
            .HasDefaultValue(0);
        
        builder.Property(t => t.totalHours)
            .IsRequired(true)
            .HasPrecision(5, 2)
            .HasDefaultValue(0);
        
        builder.Property(t => t.breakMinutes)
            .IsRequired(true)
            .HasPrecision(5, 2)
            .HasDefaultValue(0);
        
        builder.Property(t => t.billableHours)
            .IsRequired(true)
            .HasPrecision(5, 2)
            .HasDefaultValue(0);
        
        builder.Property(t => t.notes)
            .IsRequired(false)
            .HasMaxLength(500);
        
        builder.Property(t => t.status)
            .IsRequired(true)
            .HasMaxLength(20)
            .HasDefaultValue("pending");
        
        builder.Property(t => t.approvedByUserId)
            .IsRequired(false);
        
        builder.Property(t => t.approvedDatetime)
            .IsRequired(false);
        
        builder.Property(t => t.rejectionReason)
            .IsRequired(false)
            .HasMaxLength(500);
        
        builder.Property(t => t.isAutoClockIn)
            .IsRequired(true)
            .HasDefaultValue(false);
        
        builder.Property(t => t.isAutoClockOut)
            .IsRequired(true)
            .HasDefaultValue(false);
        
        builder.Property(t => t.clockInLatitude)
            .IsRequired(false)
            .HasPrecision(10, 7);
        
        builder.Property(t => t.clockInLongitude)
            .IsRequired(false)
            .HasPrecision(10, 7);
        
        builder.Property(t => t.clockOutLatitude)
            .IsRequired(false)
            .HasPrecision(10, 7);
        
        builder.Property(t => t.clockOutLongitude)
            .IsRequired(false)
            .HasPrecision(10, 7);
        
        builder.Property(t => t.createDatetime)
            .IsRequired(true);
        
        builder.Property(t => t.updateDatetime)
            .IsRequired(true);

        builder.HasOne(t => t.user)
            .WithMany()
            .HasForeignKey(t => t.userId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.job)
            .WithMany()
            .HasForeignKey(t => t.jobId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.approvedByUser)
            .WithMany()
            .HasForeignKey(t => t.approvedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => new { t.companyId, t.userId, t.clockInTime });
        builder.HasIndex(t => new { t.companyId, t.status });
    }
}
