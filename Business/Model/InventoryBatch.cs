using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Business.Model;

public class InventoryBatch
{
    public Guid id { get; set; }
    public Guid inventoryId { get; set; }
    public Guid companyId { get; set; }
    public decimal quantityAdded { get; set; }
    public decimal quantityRemaining { get; set; }
    public DateTime addedAt { get; set; }
    public DateTime expiresAt { get; set; }
    public bool isExpired { get; set; }
    public Inventory? inventory { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

public class InventoryBatchConfiguration : IEntityTypeConfiguration<InventoryBatch>
{
    public void Configure(EntityTypeBuilder<InventoryBatch> builder)
    {
        builder.HasKey("id");
        builder.Property(c => c.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.inventoryId).IsRequired(true);
        builder.Property(c => c.companyId).IsRequired(true);
        builder.Property(c => c.quantityAdded).IsRequired(true).HasPrecision(18, 2);
        builder.Property(c => c.quantityRemaining).IsRequired(true).HasPrecision(18, 2);
        builder.Property(c => c.addedAt).IsRequired(true);
        builder.Property(c => c.expiresAt).IsRequired(true);
        builder.Property(c => c.isExpired).IsRequired(true).HasDefaultValue(false);
        builder.HasOne(c => c.inventory)
               .WithMany(c => c.batches)
               .HasForeignKey(c => c.inventoryId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.Property(c => c.createDatetime);
        builder.Property(c => c.updateDatetime);
        
        // Index for expiration queries
        builder.HasIndex(c => c.expiresAt);
        builder.HasIndex(c => new { c.companyId, c.expiresAt });
    }
}
