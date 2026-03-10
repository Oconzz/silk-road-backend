using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Model;

public class Job
{
    public Guid id { get; set; }
    public Guid companyId { get; set; }
    public string? jobNumber { get; set; }
    public string title { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public string status { get; set; }

    // Source estimate (if converted from estimate)
    public Guid? estimateId { get; set; }
    public Estimates? estimate { get; set; }
    
    // Scheduling
    public DateTime? scheduledStartDate { get; set; }
    public DateTime? scheduledEndDate { get; set; }
    
    // Financial fields
    public decimal estimatedTotal { get; set; }
    public decimal laborCost { get; set; }
    public decimal materialsCost { get; set; }

    public Client? client { get; set; }
    public Location? location { get; set; }
    // Removed: List<Inventory>? inventory - use JobInventory join table instead
    public List<JobInventory>? jobInventory { get; set; }

    // Recurring Job fields
    public bool isRecurring { get; set; }
    public string? recurrenceRule { get; set; }
    public DateTime? recurrenceEndDate { get; set; }
    public string? timeZone { get; set; }
    public DateTime? lastOccurrenceCreated { get; set; }
    public string? billingFrequency { get; set; } // per_visit, monthly, on_completion, custom

    // Parent/Child relationship for recurring jobs
    public Guid? parentJobId { get; set; }
    public Job? parentJob { get; set; }
    public DateTime? occurrenceDate { get; set; }

    // Google Calendar Integration (NMJ-40, NMJ-41)
    public string? googleCalendarEventId { get; set; }

    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        //builder.ToTable("CallNote");
        builder.HasKey("id");
        builder.Property(c => c.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.title)
            .IsRequired(true)
            .HasMaxLength(100);
        builder.Property(c => c.companyId)
            .IsRequired(true);
        builder.Property(c => c.description)
            .IsRequired(false)
            .HasMaxLength(500);
        builder.Property(c => c.status)
            .IsRequired(false)
            .HasMaxLength(50);
        builder.Property(c => c.estimateId)
            .IsRequired(false);
        builder.Property(c => c.jobNumber)
            .IsRequired(false)
            .HasMaxLength(50);
        builder.Property(c => c.scheduledStartDate)
            .IsRequired(false);
        builder.Property(c => c.scheduledEndDate)
            .IsRequired(false);
        builder.Property(c => c.estimatedTotal)
            .HasPrecision(18, 2);
        builder.Property(c => c.laborCost)
            .HasPrecision(18, 2);
        builder.Property(c => c.materialsCost)
            .HasPrecision(18, 2);
        builder.Property(c => c.createDatetime);
        builder.Property(c => c.updateDatetime);

        // Recurring Job fields
        builder.Property(c => c.isRecurring).HasDefaultValue(false);
        builder.Property(c => c.recurrenceRule).IsRequired(false).HasMaxLength(500);
        builder.Property(c => c.recurrenceEndDate).IsRequired(false);
        builder.Property(c => c.timeZone).IsRequired(false).HasMaxLength(100);
        builder.Property(c => c.lastOccurrenceCreated).IsRequired(false);
        builder.Property(c => c.billingFrequency).IsRequired(false).HasMaxLength(50);
        builder.Property(c => c.parentJobId).IsRequired(false);
        builder.Property(c => c.occurrenceDate).IsRequired(false);

        builder.HasOne(c => c.parentJob)
            .WithMany()
            .HasForeignKey(c => c.parentJobId)
            .OnDelete(DeleteBehavior.NoAction);

        // Google Calendar Integration (NMJ-40, NMJ-41)
        builder.Property(c => c.googleCalendarEventId).IsRequired(false).HasMaxLength(500);

        builder.HasOne(c => c.client)
               .WithMany()
               .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(c => c.location)
               .WithMany()
               .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(c => c.estimate)
            .WithMany()
            .HasForeignKey(c => c.estimateId)
            .OnDelete(DeleteBehavior.SetNull);
        // Removed: Inventory relationship - use JobInventory join table instead
        builder.HasMany(c => c.jobInventory)
            .WithOne(ji => ji.job)
            .HasForeignKey(ji => ji.jobId);

        // Relationship configured in UserCompanyConfiguration
    }
}
