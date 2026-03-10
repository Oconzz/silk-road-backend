using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Business.Model
{
    public class Payment
    {
        public Guid id { get; set; }
        public Guid companyId { get; set; }
        public Company? company { get; set; }
        public Guid jobId { get; set; }
        public Job? job { get; set; }
        public Guid userId { get; set; }
        public User? user { get; set; }

        // Square transaction details
        public string? squareTransactionId { get; set; }
        public string? squarePaymentId { get; set; }

        // Square Payment Link details
        public string? squarePaymentLinkId { get; set; }
        public string? squarePaymentLinkUrl { get; set; }
        public string? squareOrderId { get; set; }

        // Payment details
        public decimal amount { get; set; }
        public string currency { get; set; } = "USD";
        public string status { get; set; } = "pending"; // pending, completed, failed, cancelled
        public string? paymentMethod { get; set; }
        public string? cardBrand { get; set; }
        public string? lastFourDigits { get; set; }

        // Notes and metadata
        public string? notes { get; set; }
        public string? failureReason { get; set; }

        // Timestamps
        public DateTime createDatetime { get; set; }
        public DateTime? completedDatetime { get; set; }
        public DateTime updateDatetime { get; set; }
    }

    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(p => p.id);
            builder.Property(p => p.id)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("gen_random_uuid()");

            // Foreign keys
            builder.HasOne(p => p.company)
                .WithMany()
                .HasForeignKey(p => p.companyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.job)
                .WithMany()
                .HasForeignKey(p => p.jobId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.user)
                .WithMany()
                .HasForeignKey(p => p.userId)
                .OnDelete(DeleteBehavior.Restrict);

            // Square transaction fields
            builder.Property(p => p.squareTransactionId).IsRequired(false).HasMaxLength(256);
            builder.Property(p => p.squarePaymentId).IsRequired(false).HasMaxLength(256);

            // Square Payment Link fields
            builder.Property(p => p.squarePaymentLinkId).IsRequired(false).HasMaxLength(256);
            builder.Property(p => p.squarePaymentLinkUrl).IsRequired(false).HasMaxLength(2000);
            builder.Property(p => p.squareOrderId).IsRequired(false).HasMaxLength(256);

            // Payment details
            builder.Property(p => p.amount).HasPrecision(18, 2);
            builder.Property(p => p.currency).IsRequired().HasMaxLength(3).HasDefaultValue("USD");
            builder.Property(p => p.status).IsRequired().HasMaxLength(50).HasDefaultValue("pending");
            builder.Property(p => p.paymentMethod).IsRequired(false).HasMaxLength(50);
            builder.Property(p => p.cardBrand).IsRequired(false).HasMaxLength(50);
            builder.Property(p => p.lastFourDigits).IsRequired(false).HasMaxLength(4);

            // Notes and metadata
            builder.Property(p => p.notes).IsRequired(false).HasMaxLength(1000);
            builder.Property(p => p.failureReason).IsRequired(false).HasMaxLength(500);

            // Timestamps
            builder.Property(p => p.createDatetime).IsRequired();
            builder.Property(p => p.completedDatetime).IsRequired(false);
            builder.Property(p => p.updateDatetime).IsRequired();

            // Indexes for common queries
            builder.HasIndex(p => p.jobId);
            builder.HasIndex(p => p.userId);
            builder.HasIndex(p => p.companyId);
            builder.HasIndex(p => p.status);
            builder.HasIndex(p => p.squarePaymentId);
            builder.HasIndex(p => p.squarePaymentLinkId);
        }
    }
}
