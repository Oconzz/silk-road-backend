using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Model;

public class Warehouse
{
    public Guid id { get; set; }
    public Guid companyId { get; set; }
    public string name { get; set; } = string.Empty;
    public string location { get; set; } = string.Empty; // Legacy field - kept for backward compatibility
    
    // Structured address fields
    public string addressLine1 { get; set; } = string.Empty;
    public string addressLine2 { get; set; } = string.Empty;
    public string city { get; set; } = string.Empty;
    public string state { get; set; } = string.Empty;
    public string zipcode { get; set; } = string.Empty;
    public string country { get; set; } = string.Empty;
    
    public double latitude { get; set; }
    public double longitude { get; set; }
    public List<Inventory>? inventory { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}
public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Warehouse> builder)
    {
        //builder.ToTable("CallNote");
        builder.HasKey("id");
        builder.Property(c => c.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.companyId).IsRequired(true);
        builder.Property(c => c.name).IsRequired(true)
            .HasMaxLength(100);
        builder.Property(c => c.location).IsRequired(false).HasMaxLength(200);
        
        // Structured address fields
        builder.Property(c => c.addressLine1).IsRequired(false).HasMaxLength(200);
        builder.Property(c => c.addressLine2).IsRequired(false).HasMaxLength(200);
        builder.Property(c => c.city).IsRequired(false).HasMaxLength(100);
        builder.Property(c => c.state).IsRequired(false).HasMaxLength(100);
        builder.Property(c => c.zipcode).IsRequired(false).HasMaxLength(20);
        builder.Property(c => c.country).IsRequired(false).HasMaxLength(100);
        
        builder.Property(c => c.latitude);
        builder.Property(c => c.longitude);
        builder.Property(c => c.createDatetime);
        builder.HasMany(c => c.inventory)
            .WithOne(c=>c.warehouse);
        builder.Property(c => c.updateDatetime);
    }
}
