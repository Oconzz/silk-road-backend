using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Business.Model
{
    public class DeviceToken
    {
        public Guid id { get; set; }
        public Guid userId { get; set; }
        public Guid companyId { get; set; }
        public string token { get; set; } = string.Empty;
        public string platform { get; set; } = string.Empty; // "ios" or "android"
        public string? deviceId { get; set; }
        public bool active { get; set; } = true;
        public DateTime createDatetime { get; set; }
        public DateTime updateDatetime { get; set; }

        // Navigation
        public User? user { get; set; }
        public Company? company { get; set; }
    }

    public class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
    {
        public void Configure(EntityTypeBuilder<DeviceToken> builder)
        {
            builder.HasKey(d => d.id);
            builder.Property(d => d.id)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("gen_random_uuid()");
            builder.Property(d => d.token).IsRequired().HasMaxLength(512);
            builder.Property(d => d.platform).IsRequired().HasMaxLength(10);
            builder.Property(d => d.deviceId).IsRequired(false).HasMaxLength(256);
            builder.Property(d => d.active).HasDefaultValue(true);

            builder.HasOne(d => d.user)
                .WithMany()
                .HasForeignKey(d => d.userId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.company)
                .WithMany()
                .HasForeignKey(d => d.companyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: one token per user+device
            builder.HasIndex(d => new { d.userId, d.token }).IsUnique();
        }
    }
}
