using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Business.Model;

public class InventoryUsageHistory
{
    public Guid id { get; set; }
    public Guid companyId { get; set; }
    public Guid inventoryId { get; set; }
    public Guid? jobId { get; set; }
    public string actionType { get; set; } = string.Empty; // "used", "returned", "adjusted", "added"
    public decimal quantity { get; set; }
    public decimal previousQuantity { get; set; }
    public decimal newQuantity { get; set; }
    public string? notes { get; set; }
    public Guid? performedByUserId { get; set; }
    
    public Inventory? inventory { get; set; }
    public Job? job { get; set; }
    public User? performedByUser { get; set; }
    
    public DateTime createDatetime { get; set; }
}

public class InventoryUsageHistoryConfiguration : IEntityTypeConfiguration<InventoryUsageHistory>
{
    public void Configure(EntityTypeBuilder<InventoryUsageHistory> builder)
    {
        builder.HasKey(x => x.id);
        builder.Property(x => x.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.companyId).IsRequired(true);
        builder.Property(x => x.inventoryId).IsRequired(true);
        builder.Property(x => x.jobId).IsRequired(false);
        builder.Property(x => x.actionType).IsRequired(true).HasMaxLength(50);
        builder.Property(x => x.quantity).IsRequired(true).HasPrecision(18, 2);
        builder.Property(x => x.previousQuantity).IsRequired(true).HasPrecision(18, 2);
        builder.Property(x => x.newQuantity).IsRequired(true).HasPrecision(18, 2);
        builder.Property(x => x.notes).IsRequired(false).HasMaxLength(500);
        builder.Property(x => x.performedByUserId).IsRequired(false);
        builder.Property(x => x.createDatetime);
        
        builder.HasOne(x => x.inventory)
            .WithMany()
            .HasForeignKey(x => x.inventoryId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(x => x.job)
            .WithMany()
            .HasForeignKey(x => x.jobId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(x => x.performedByUser)
            .WithMany()
            .HasForeignKey(x => x.performedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasIndex(x => x.inventoryId);
        builder.HasIndex(x => x.jobId);
    }
}
