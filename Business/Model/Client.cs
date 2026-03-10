using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Model;

public class Client
{
    public Guid id { get; set; }
    public Guid companyId { get; set; }
    public string contactName { get; set; } = string.Empty;
    public string phone { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string companyName { get; set; } = string.Empty;
    
    // 1:N relationship - Client can have multiple locations
    public List<Location>? locations { get; set; }
    
    // Legacy field for backward compatibility - returns first location
    public Location? location => locations?.FirstOrDefault();

    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}
public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        //builder.ToTable("CallNote");
        builder.HasKey("id");
        builder.Property(c => c.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.companyId).IsRequired(true);
        builder.Property(c => c.contactName).IsRequired(true)
            .HasMaxLength(100);
        builder.Property(c => c.email).IsRequired(true).HasMaxLength(50);
        builder.Property(c => c.phone).IsRequired(true).HasMaxLength(25);
        builder.Property(c => c.companyName).IsRequired(true).HasMaxLength(25);


        builder.Property(c => c.createDatetime);
        builder.Property(c => c.updateDatetime);
        // 1:N relationship with Locations
        builder.HasMany(c => c.locations)
               .WithOne(l => l.client)
               .HasForeignKey(l => l.clientId)
               .OnDelete(DeleteBehavior.Cascade);
        // Relationship configured in UserCompanyConfiguration
    }
}
