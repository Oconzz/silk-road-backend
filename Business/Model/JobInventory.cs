using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Business.Model;

public class JobInventory
{
    public Guid id { get; set; }
    public Guid companyId { get; set; }
    public Guid jobId { get; set; }
    public Guid? productId { get; set; }
    public Guid? warehouseId { get; set; }
    public Guid? inventoryId { get; set; } // Reference to the specific inventory item
    public decimal idealQuantity { get; set; }
    public decimal actualQuantity { get; set; }
    public decimal price { get; set; }
    public decimal labor { get; set; }
    public string title { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public string barcode { get; set; } = string.Empty;
    public string source { get; set; } = string.Empty;
    public Job? job { get; set; }
    public Warehouse? warehouse { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

public class JobInventoryConfiguration : IEntityTypeConfiguration<JobInventory>
{
    public void Configure(EntityTypeBuilder<JobInventory> builder)
    {
        builder.HasKey("id");
        builder.Property(c => c.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.companyId).IsRequired(true);
        builder.Property(c => c.jobId).IsRequired(true);
        builder.Property(c => c.productId).IsRequired(false);
        builder.Property(c => c.warehouseId).IsRequired(false);
        builder.Property(c => c.inventoryId).IsRequired(false);
        builder.Property(c => c.idealQuantity).IsRequired(true).HasPrecision(18, 2);
        builder.Property(c => c.actualQuantity).IsRequired(true).HasPrecision(18, 2);
        builder.Property(c => c.price).IsRequired(true).HasPrecision(18, 2);
        builder.Property(c => c.labor).IsRequired(true).HasPrecision(18, 2);
        builder.Property(c => c.title).IsRequired(true).HasMaxLength(100);
        builder.Property(c => c.description).IsRequired(false).HasMaxLength(500);
        builder.Property(c => c.barcode).IsRequired(false).HasMaxLength(100);
        builder.Property(c => c.source).IsRequired(false).HasMaxLength(1000);
        builder.HasOne(c => c.job)
               .WithMany(j => j.jobInventory)
               .HasForeignKey(c => c.jobId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(c => c.warehouse)
               .WithMany()
               .HasForeignKey(c => c.warehouseId)
               .OnDelete(DeleteBehavior.ClientNoAction);
        builder.Property(c => c.createDatetime);
        builder.Property(c => c.updateDatetime);
    }
}
