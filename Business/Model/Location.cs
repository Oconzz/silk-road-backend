using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Model;

public class Location
{
    public Guid id { get; set; }
    public Guid companyId { get; set; }
    
    // Foreign key to Client (1:N relationship)
    public Guid? clientId { get; set; }
    public Client? client { get; set; }
    
    // Optional name/label for the location (e.g., "Main Office", "Warehouse")
    public string name { get; set; } = string.Empty;
    
    public string addressLine1 { get; set; } = string.Empty;
    public string addressLine2 { get; set; } = string.Empty;
    public string city { get; set; } = string.Empty;
    public string state { get; set; } = string.Empty;
    public string zipcode { get; set; } = string.Empty;
    public string country { get; set; } = string.Empty;
    public double latitude { get; set; }
    public double longitude { get; set; }
    public bool isPrimary { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        //builder.ToTable("CallNote");
        builder.HasKey("id");
        builder.Property(c => c.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.companyId).IsRequired(true);
        builder.Property(c => c.addressLine1).IsRequired(true)
            .HasMaxLength(100);
        builder.Property(c => c.addressLine2).IsRequired(false).HasMaxLength(100);
        builder.Property(c => c.city).IsRequired(true).HasMaxLength(50);
        builder.Property(c => c.state).IsRequired(true).HasMaxLength(50);
        builder.Property(c => c.zipcode).IsRequired(false).HasMaxLength(10);
        builder.Property(c => c.country).IsRequired(false).HasMaxLength(100);
        builder.Property(c => c.latitude);
        builder.Property(c => c.longitude);
        builder.Property(c => c.isPrimary).HasDefaultValue(false);
        
        // Foreign key to Client
        builder.Property(c => c.clientId).IsRequired(false);
        builder.Property(c => c.name).IsRequired(false).HasMaxLength(100);

        builder.Property(c => c.createDatetime);
        builder.Property(c => c.updateDatetime);
        
        // Relationship configured in ClientConfiguration
    }
}
