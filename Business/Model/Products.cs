using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Model;

public class Products{
    public Guid id { get; set; }
    public Guid productId { get; set; }
    public string name { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public decimal price { get; set; }
    public string barcode { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }

}

public class ProductsConfiguration : IEntityTypeConfiguration<Products>
{
    public void Configure(EntityTypeBuilder<Products> builder)
    {
        //builder.ToTable("CallNote");
        builder.HasKey("id");
        builder.Property(c => c.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.name)
            .IsRequired(true)
            .HasMaxLength(100);
        builder.Property(c => c.price)
            .IsRequired(true)
            .HasColumnType("decimal(18,2)");
        builder.Property(c => c.description)
            .IsRequired(true)
            .HasMaxLength(1000);
        builder.Property(c => c.barcode)
             .IsRequired(true)
             .HasMaxLength(100);
        builder.Property(c => c.createDatetime);
        builder.Property(c => c.updateDatetime);

    }
}

