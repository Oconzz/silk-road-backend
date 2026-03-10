using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;

namespace Business.Model;

public class EstimateAlbum
{
    public Guid id { get; set; }
    public Guid estimateId { get; set; }
    public Guid companyId { get; set; }
    public string name { get; set; } = string.Empty;
    public string? description { get; set; }
    public int orderIndex { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }

    // Navigation properties
    public Estimates? estimate { get; set; }
    public List<EstimateMedia> media { get; set; } = new List<EstimateMedia>();
}

public class EstimateAlbumConfiguration : IEntityTypeConfiguration<EstimateAlbum>
{
    public void Configure(EntityTypeBuilder<EstimateAlbum> builder)
    {
        builder.HasKey(c => c.id);
        
        builder.Property(c => c.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(c => c.estimateId)
            .IsRequired(true);
        
        builder.Property(c => c.companyId)
            .IsRequired(true);
        
        builder.Property(c => c.name)
            .IsRequired(true)
            .HasMaxLength(200);
        
        builder.Property(c => c.description)
            .IsRequired(false)
            .HasMaxLength(500);
        
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
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.media)
            .WithOne(m => m.album)
            .HasForeignKey(m => m.albumId);
    }
}
