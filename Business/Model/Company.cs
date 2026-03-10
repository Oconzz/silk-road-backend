using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;

namespace Business.Model
{
    public class Company
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string zipcode { get; set; }
        public string country { get; set; }
        public bool active { get; set; }
        public DateTime createDatetime { get; set; }
        public DateTime updateDatetime { get; set; }
        public string? logo { get; set; }
        public string? logoThumbnail { get; set; }
        public string? category { get; set; }  // Business category (e.g., "Painting", "Roofing", etc.)
        
        // RevenueCat Subscription
        public string? revenueCatCustomerId { get; set; }
        public bool subscriptionActive { get; set; } = false;
        public string? subscriptionPlan { get; set; }  // "base_monthly", "base_annual"
        public int subscriptionUserPacks { get; set; } = 0;
        public DateTime? subscriptionExpirationDate { get; set; }
        public DateTime? subscriptionLastVerified { get; set; }

        // Many-to-many with roles: a company can have many users with different roles
        public ICollection<UserCompany> userCompanies { get; set; } = new List<UserCompany>();
    }
    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            //builder.ToTable("CallNote");
            builder.HasKey("id");
            builder.Property(c => c.id)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("gen_random_uuid()");
            builder.Property(c => c.name).IsRequired(true)
                .HasMaxLength(100);
            builder.Property(c => c.email).IsRequired(true).HasMaxLength(50);
            builder.Property(c => c.phone).IsRequired(true).HasMaxLength(25);
            builder.Property(c => c.city).IsRequired(true).HasMaxLength(25);
            builder.Property(c => c.state).IsRequired(true).HasMaxLength(50);
            builder.Property(c => c.zipcode).IsRequired(true).HasMaxLength(15);
            builder.Property(c => c.country).IsRequired(true).HasMaxLength(30);
            builder.Property(c => c.active);

            builder.Property(c => c.logo).HasColumnType("text");
            builder.Property(c => c.logoThumbnail).HasColumnType("text");
            builder.Property(c => c.category).HasMaxLength(50);  // Business category

            // RevenueCat Subscription
            builder.Property(c => c.revenueCatCustomerId).IsRequired(false).HasMaxLength(256);
            builder.Property(c => c.subscriptionActive).HasDefaultValue(false);
            builder.Property(c => c.subscriptionPlan).IsRequired(false).HasMaxLength(50);
            builder.Property(c => c.subscriptionUserPacks).HasDefaultValue(0);
            builder.Property(c => c.subscriptionExpirationDate).IsRequired(false);
            builder.Property(c => c.subscriptionLastVerified).IsRequired(false);
        }
    }
}
