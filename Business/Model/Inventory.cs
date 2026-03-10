using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Model;

public class Inventory
{
    public Guid id { get; set; }
    public Guid companyId { get; set; }
    public Guid productId { get; set; }
    public Guid warehouseId { get; set; }
    public decimal quantity { get; set; }
    public decimal price { get; set; }
    public string title { get; set; }
    public string description { get; set; }
    public string barcode { get; set; }
    public string source { get; set; }
    public string? category { get; set; }
    public string? imageS3Key { get; set; }
    public Warehouse? warehouse { get; set; }
    public ICollection<InventoryBatch>? batches { get; set; }
    // Removed: Job navigation property was causing FK_Inventory_Job_id constraint error
    // If inventory needs to be linked to a job, use a separate JobInventory join table
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}
public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        //builder.ToTable("CallNote");
        builder.HasKey("id");
        builder.Property(c => c.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.companyId).IsRequired(true);
        builder.Property(c => c.productId).IsRequired(true);
        builder.Property(c => c.warehouseId).IsRequired(true);
        builder.Property(c => c.quantity).IsRequired(true).HasPrecision(18, 2);
        builder.Property(c => c.price).IsRequired(true).HasPrecision(18, 2);
        builder.Property(c => c.title).IsRequired(true).HasMaxLength(100);
        builder.Property(c => c.description).IsRequired(false).HasMaxLength(500);
        builder.Property(c => c.barcode).IsRequired(false).HasMaxLength(100);
        builder.Property(c => c.source).IsRequired(false).HasMaxLength(1000);
        builder.Property(c => c.category).IsRequired(false).HasMaxLength(100);
        builder.Property(c => c.imageS3Key).IsRequired(false).HasMaxLength(500);
        builder.HasOne(c => c.warehouse)
               .WithMany(c => c.inventory)
               .HasForeignKey(c => c.warehouseId)
               .OnDelete(DeleteBehavior.ClientNoAction);
        builder.Property(c => c.createDatetime);
        builder.Property(c => c.updateDatetime);
    }
}

