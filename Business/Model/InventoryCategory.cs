using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Business.Model;

public class InventoryCategory
{
    public Guid id { get; set; }
    public Guid companyId { get; set; }
    public string name { get; set; }
    public string color { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

public class InventoryCategoryConfiguration : IEntityTypeConfiguration<InventoryCategory>
{
    public void Configure(EntityTypeBuilder<InventoryCategory> builder)
    {
        builder.ToTable("InventoryCategory");
        builder.HasKey(c => c.id);
        builder.Property(c => c.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.companyId).IsRequired(true);
        builder.Property(c => c.name).IsRequired(true).HasMaxLength(100);
        builder.Property(c => c.color).IsRequired(true).HasMaxLength(20);
        builder.Property(c => c.createDatetime);
        builder.Property(c => c.updateDatetime);

        builder.HasIndex(c => new { c.companyId, c.name }).IsUnique();
    }
}
