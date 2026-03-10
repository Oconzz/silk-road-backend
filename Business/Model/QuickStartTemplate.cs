using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Business.Model;

/// <summary>
/// Global read-only templates for quick-starting estimates based on company category.
/// These are seeded by the system and cannot be modified by users.
/// </summary>
public class QuickStartTemplate
{
    public Guid id { get; set; }
    public string category { get; set; } = string.Empty;  // Business category (e.g., "Painting")
    public string name { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public int displayOrder { get; set; }
    public bool active { get; set; } = true;
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

public class QuickStartTemplateConfiguration : IEntityTypeConfiguration<QuickStartTemplate>
{
    public void Configure(EntityTypeBuilder<QuickStartTemplate> builder)
    {
        builder.HasKey(t => t.id);
        builder.Property(t => t.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(t => t.category)
            .IsRequired(true)
            .HasMaxLength(50);
        builder.Property(t => t.name)
            .IsRequired(true)
            .HasMaxLength(100);
        builder.Property(t => t.description)
            .IsRequired(true)
            .HasMaxLength(500);
        builder.Property(t => t.displayOrder)
            .IsRequired(true);
        builder.Property(t => t.active)
            .IsRequired(true)
            .HasDefaultValue(true);
        builder.Property(t => t.createDatetime);
        builder.Property(t => t.updateDatetime);

        // Index for fast lookup by category
        builder.HasIndex(t => t.category);
    }
}
