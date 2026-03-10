using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Business.Model;

public class JobNote
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
    public Guid companyId { get; set; }
    public Guid createdBy { get; set; }
    public string content { get; set; } = string.Empty;
    public string type { get; set; } = "general"; // general, progress, issue, etc
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }

    // Navigation properties
    public Job? job { get; set; }
    public User? creator { get; set; }
}

public class JobNoteConfiguration : IEntityTypeConfiguration<JobNote>
{
    public void Configure(EntityTypeBuilder<JobNote> builder)
    {
        builder.HasKey(c => c.id);
        
        builder.Property(c => c.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(c => c.jobId)
            .IsRequired(true);
        
        builder.Property(c => c.companyId)
            .IsRequired(true);
        
        builder.Property(c => c.createdBy)
            .IsRequired(true);
        
        builder.Property(c => c.content)
            .IsRequired(true)
            .HasMaxLength(2000);
        
        builder.Property(c => c.type)
            .IsRequired(true)
            .HasMaxLength(50);
        
        builder.Property(c => c.createDatetime)
            .IsRequired(true);
        
        builder.Property(c => c.updateDatetime)
            .IsRequired(true);

        builder.HasOne(c => c.job)
            .WithMany()
            .HasForeignKey(c => c.jobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.creator)
            .WithMany()
            .HasForeignKey(c => c.createdBy)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
