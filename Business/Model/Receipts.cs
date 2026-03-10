using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Model;

public class Receipts{
    public Guid id { get; set; }
    public Guid companyId { get; set; }
    public string s3Name { get; set; } = string.Empty;
    public string fileName { get; set; } = string.Empty;
    public double size { get; set; }
    public string contentType { get; set; } = string.Empty;
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

public class ReceiptsConfiguration : IEntityTypeConfiguration<Receipts>
{
    public void Configure(EntityTypeBuilder<Receipts> builder)
    {
        //builder.ToTable("CallNote");
        builder.HasKey("id");
        builder.Property(c => c.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.s3Name)
            .HasMaxLength(500);
        builder.Property(c => c.fileName)
            .HasMaxLength(100);
        builder.Property(c => c.companyId)
            .IsRequired(true);
        builder.Property(c => c.size);
        builder.Property(c => c.contentType);
        builder.Property(c => c.createDatetime);
        builder.Property(c => c.updateDatetime);

    }
}