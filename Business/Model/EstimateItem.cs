using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Business.Model;

public class EstimateItem
{
    public Guid id { get; set; }
    public Guid companyId { get; set; }
    public Guid? estimateId { get; set; }  // FK para Estimate (nullable)
    public Guid? estimateTemplateId { get; set; }  // FK para EstimateTemplate (nullable)
    public Guid? productId { get; set; }  // Nullable - item pode não vir do inventário
    public Guid? warehouseId { get; set; }  // Nullable
    public Guid? inventoryId { get; set; }  // FK para Inventory - para rastrear deduction
    public decimal quantity { get; set; }
    public decimal price { get; set; }
    public decimal labor { get; set; }
    public string title { get; set; }
    public string description { get; set; }
    public string barcode { get; set; }
    public string source { get; set; }
    // Warehouse navigation property removed to avoid FK issues
    // public Warehouse? warehouse { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

public class EstimateItemConfiguration : IEntityTypeConfiguration<EstimateItem>
{
    public void Configure(EntityTypeBuilder<EstimateItem> builder)
    {
        builder.HasKey("id");
        builder.Property(c => c.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.companyId).IsRequired(true);
        builder.Property(c => c.estimateId).IsRequired(false);
        builder.Property(c => c.estimateTemplateId).IsRequired(false);
        builder.Property(c => c.productId).IsRequired(false);
        builder.Property(c => c.warehouseId).IsRequired(false);
        builder.Property(c => c.inventoryId).IsRequired(false);
        builder.Property(c => c.quantity).IsRequired(true).HasPrecision(18, 2);
        builder.Property(c => c.price).IsRequired(true).HasPrecision(18, 2);
        builder.Property(c => c.labor).IsRequired(true).HasPrecision(18, 2);
        builder.Property(c => c.title).IsRequired(true).HasMaxLength(100);
        builder.Property(c => c.description).IsRequired(false).HasMaxLength(500);
        builder.Property(c => c.barcode).IsRequired(false).HasMaxLength(100);
        builder.Property(c => c.source).IsRequired(false).HasMaxLength(1000);
        // Warehouse FK removed - warehouseId is nullable and not enforced
        // builder.HasOne(c => c.warehouse)
        //        .WithMany()
        //        .HasForeignKey(c => c.warehouseId)
        //        .IsRequired(false)
        //        .OnDelete(DeleteBehavior.ClientNoAction);
        builder.Property(c => c.createDatetime);
        builder.Property(c => c.updateDatetime);
    }
}
