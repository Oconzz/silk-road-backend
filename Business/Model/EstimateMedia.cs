using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Business.Model;

public class EstimateMedia
{
    public Guid id { get; set; }
    public Guid estimateId { get; set; }
    public Guid companyId { get; set; }
    public Guid? albumId { get; set; }
    public Guid? estimateItemId { get; set; }
    public string webUrl { get; set; } = string.Empty;
    public string thumbUrl { get; set; } = string.Empty;
    public string? caption { get; set; }
    public string type { get; set; } = "photo"; // photo, video, document
    public int orderIndex { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }

    // Navigation properties
    public Estimates? estimate { get; set; }
    public EstimateAlbum? album { get; set; }
    public EstimateItem? estimateItem { get; set; }
}

public class EstimateMediaConfiguration : IEntityTypeConfiguration<EstimateMedia>
{
    public void Configure(EntityTypeBuilder<EstimateMedia> builder)
    {
        builder.HasKey(c => c.id);
        
        builder.Property(c => c.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(c => c.estimateId)
            .IsRequired(true);
        
        builder.Property(c => c.companyId)
            .IsRequired(true);
        
        builder.Property(c => c.albumId)
            .IsRequired(false);
        
        builder.Property(c => c.estimateItemId)
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
        
        builder.Property(c => c.orderIndex)
            .IsRequired(true)
            .HasDefaultValue(0);
        
        builder.Property(c => c.createDatetime)
            .IsRequired(true);
        
        builder.Property(c => c.updateDatetime)
            .IsRequired(true);

        builder.HasOne(c => c.estimate)
            .WithMany()
            .HasForeignKey(c => c.estimateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.album)
            .WithMany()
            .HasForeignKey(c => c.albumId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.estimateItem)
            .WithMany()
            .HasForeignKey(c => c.estimateItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
