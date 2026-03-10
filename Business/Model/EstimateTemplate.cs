using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Model;

public class EstimateTemplate{
    public Guid id { get; set; }
    public Guid companyId { get; set; }
    public string name { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public List<EstimateItem> items { get; set; } = new List<EstimateItem>();
    public decimal total { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

public class EstimateTemplateConfiguration : IEntityTypeConfiguration<EstimateTemplate>
{
    public void Configure(EntityTypeBuilder<EstimateTemplate> builder)
    {
        //builder.ToTable("CallNote");
        builder.HasKey("id");
        builder.Property(c => c.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.name)
            .IsRequired(true)
            .HasMaxLength(100);
        builder.Property(c => c.description)
            .IsRequired(true)
            .HasMaxLength(1000);
        builder.Property(c => c.companyId)
            .IsRequired(true); ;
        builder.Property(c => c.total)
            .IsRequired(true)
            .HasPrecision(18, 2);
        builder.Property(c => c.createDatetime);
        builder.Property(c => c.updateDatetime);

        builder.HasMany(c => c.items)
               .WithOne()
               .HasForeignKey(i => i.estimateTemplateId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
