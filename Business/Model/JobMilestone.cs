using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Business.Model;

public class JobMilestone
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
    public Guid companyId { get; set; }
    public string title { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public bool completed { get; set; }
    public DateTime? completedDate { get; set; }
    public Guid? completedBy { get; set; }
    public int orderIndex { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }

    // Navigation properties
    public Job? job { get; set; }
    public User? completedByUser { get; set; }
}

public class JobMilestoneConfiguration : IEntityTypeConfiguration<JobMilestone>
{
    public void Configure(EntityTypeBuilder<JobMilestone> builder)
    {
        builder.HasKey(c => c.id);
        
        builder.Property(c => c.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(c => c.jobId)
            .IsRequired(true);
        
        builder.Property(c => c.companyId)
            .IsRequired(true);
        
        builder.Property(c => c.title)
            .IsRequired(true)
            .HasMaxLength(200);
        
        builder.Property(c => c.description)
            .IsRequired(false)
            .HasMaxLength(1000);
        
        builder.Property(c => c.completed)
            .IsRequired(true)
            .HasDefaultValue(false);
        
        builder.Property(c => c.completedDate)
            .IsRequired(false);
        
        builder.Property(c => c.completedBy)
            .IsRequired(false);
        
        builder.Property(c => c.orderIndex)
            .IsRequired(true)
            .HasDefaultValue(0);
        
        builder.Property(c => c.createDatetime)
            .IsRequired(true);
        
        builder.Property(c => c.updateDatetime)
            .IsRequired(true);

        builder.HasOne(c => c.job)
            .WithMany()
            .HasForeignKey(c => c.jobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.completedByUser)
            .WithMany()
            .HasForeignKey(c => c.completedBy)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
