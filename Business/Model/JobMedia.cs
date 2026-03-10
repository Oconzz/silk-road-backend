using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Business.Model;

public class JobMedia
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
    public Guid companyId { get; set; }
    public Guid? albumId { get; set; }
    public Guid? jobInventoryId { get; set; }
    public string webUrl { get; set; } = string.Empty;
    public string thumbUrl { get; set; } = string.Empty;
    public string? caption { get; set; }
    public string type { get; set; } = "photo"; // photo, video, document
    public string category { get; set; } = "general"; // before, during, after, general
    public int orderIndex { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }

    // Navigation properties
    public Job? job { get; set; }
    public JobAlbum? album { get; set; }
    public JobInventory? jobInventoryItem { get; set; }
}

public class JobMediaConfiguration : IEntityTypeConfiguration<JobMedia>
{
    public void Configure(EntityTypeBuilder<JobMedia> builder)
    {
        builder.HasKey(c => c.id);
        
        builder.Property(c => c.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(c => c.jobId)
            .IsRequired(true);
        
        builder.Property(c => c.companyId)
            .IsRequired(true);
        
        builder.Property(c => c.albumId)
            .IsRequired(false);
        
        builder.Property(c => c.jobInventoryId)
            .IsRequired(false);
        
        builder.Property(c => c.webUrl)
            .IsRequired(true)
            .HasMaxLength(500);
        
        builder.Property(c => c.thumbUrl)
            .IsRequired(true)
            .HasMaxLength(500);
        
        builder.Property(c => c.caption)
            .IsRequired(false)
            .HasMaxLength(500);
        
        builder.Property(c => c.type)
            .IsRequired(true)
            .HasMaxLength(50);
        
        builder.Property(c => c.category)
            .IsRequired(true)
            .HasMaxLength(50);
        
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

        builder.HasOne(c => c.album)
            .WithMany()
            .HasForeignKey(c => c.albumId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.jobInventoryItem)
            .WithMany()
            .HasForeignKey(c => c.jobInventoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
