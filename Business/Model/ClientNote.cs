using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Business.Model;

public class ClientNote
{
    public Guid id { get; set; }
    public Guid clientId { get; set; }
    public Guid companyId { get; set; }
    public Guid createdBy { get; set; }
    public string content { get; set; } = string.Empty;
    public string type { get; set; } = "general"; // general, site_logistics, contact_info, etc
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }

    // Navigation properties
    public Client? client { get; set; }
    public User? creator { get; set; }
}

public class ClientNoteConfiguration : IEntityTypeConfiguration<ClientNote>
{
    public void Configure(EntityTypeBuilder<ClientNote> builder)
    {
        builder.HasKey(c => c.id);
        
        builder.Property(c => c.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(c => c.clientId)
            .IsRequired(true);
        
        builder.Property(c => c.companyId)
            .IsRequired(true);
        
        builder.Property(c => c.createdBy)
            .IsRequired(true);
        
        builder.Property(c => c.content)
            .IsRequired(true)
            .HasMaxLength(2000);
        
        builder.Property(c => c.type)
            .IsRequired(true)
            .HasMaxLength(50);
        
        builder.Property(c => c.createDatetime)
            .IsRequired(true);
        
        builder.Property(c => c.updateDatetime)
            .IsRequired(true);

        builder.HasOne(c => c.client)
            .WithMany()
            .HasForeignKey(c => c.clientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.creator)
            .WithMany()
            .HasForeignKey(c => c.createdBy)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
