using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Model;

public class Estimates{
    public Guid id { get; set; }
    public Guid companyId { get; set; }
    public string name { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public List<EstimateItem> items { get; set; } = new List<EstimateItem>();
    public Client? client { get; set; }
    public Location? location { get; set; }
    public decimal total { get; set; }
    public bool sent { get; set; }
    public bool approved { get; set; }
    public DateTime sentDateTime { get; set; }
    public DateTime? proposedStartDate { get; set; }
    public DateTime? expirationDate { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
    public string? termsAndConditions { get; set; }
    // Public share link ID - allows sharing estimate via link
    public string? publicShareId { get; set; }
    // View tracking
    public int viewCount { get; set; }
    public DateTime? lastViewedAt { get; set; }

    // Versioning
    public int version { get; set; } = 1;
    public Guid? parentEstimateId { get; set; }
    public bool isLatestVersion { get; set; } = true;
}

public class EstimatesConfiguration : IEntityTypeConfiguration<Estimates>
{
    public void Configure(EntityTypeBuilder<Estimates> builder)
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
            .IsRequired(true);
        builder.Property(c => c.total)
            .IsRequired(true)
            .HasPrecision(18,2);
        builder.Property(c => c.sent);
        builder.Property(c => c.approved);
        builder.Property(c => c.sentDateTime);
        builder.Property(c => c.proposedStartDate)
            .IsRequired(false);
        builder.Property(c => c.expirationDate)
            .IsRequired(false);
        builder.Property(c => c.createDatetime);
        builder.Property(c => c.updateDatetime);
        builder.Property(c => c.termsAndConditions)
            .HasMaxLength(5000);
        builder.Property(c => c.publicShareId)
            .HasMaxLength(50)
            .IsRequired(false);
        builder.Property(c => c.viewCount)
            .HasDefaultValue(0);
        builder.Property(c => c.lastViewedAt)
            .IsRequired(false);
        builder.HasIndex(c => c.publicShareId)
            .IsUnique()
            .HasFilter("\"publicShareId\" IS NOT NULL");
        // Versioning
        builder.Property(c => c.version).HasDefaultValue(1);
        builder.Property(c => c.parentEstimateId).IsRequired(false);
        builder.Property(c => c.isLatestVersion).HasDefaultValue(true);
        builder.HasOne(c => c.client)
               .WithMany()
               .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(c => c.location)
               .WithMany()
               .OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(c => c.items)
               .WithOne()
               .HasForeignKey(i => i.estimateId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
