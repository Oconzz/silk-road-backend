using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Business.Model
{
    public class User
    {
        public Guid id { get; set; }
        public string fullName { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public string phone { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string state { get; set; } = string.Empty;
        public string zipcode { get; set; } = string.Empty;
        public string country { get; set; } = string.Empty;
        public string verificationToken { get; set; }
        public bool active { get; set; }
        public bool verified { get; set; }
        public DateTime createDatetime { get; set; }
        public DateTime updateDatetime { get; set; }
        public bool changePasswordRequired { get; set; }
        public string? personalPhoto { get; set; }
        public string? personalPhotoThumbnail { get; set; }
        public string? passwordResetToken { get; set; }
        public DateTime? passwordResetTokenExpiry { get; set; }
        public string? inviteToken { get; set; }
        public DateTime? inviteTokenExpiry { get; set; }

        // Google Calendar Integration (NMJ-40, NMJ-41, NMJ-42)
        public string? googleAccessToken { get; set; }
        public string? googleRefreshToken { get; set; }
        public DateTime? googleTokenExpiry { get; set; }
        public bool googleCalendarLinked { get; set; } = false;
        public string? googleEmail { get; set; }

        // Square Payment Integration
        public string? squareAccessToken { get; set; }
        public string? squareRefreshToken { get; set; }
        public DateTime? squareTokenExpiry { get; set; }
        public bool squareLinked { get; set; } = false;
        public string? squareMerchantId { get; set; }
        public string? squareLocationId { get; set; }

        // Many-to-many with roles: a user can be assigned to many companies with different roles
        public ICollection<UserCompany> userCompanies { get; set; } = new List<UserCompany>();
    }
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            //builder.ToTable("CallNote");
            builder.HasKey("id");
            builder.Property(c => c.id)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("gen_random_uuid()");
            builder.Property(c => c.fullName)
                .IsRequired(true)
                .HasMaxLength(100);
            builder.Property(c => c.email).IsRequired(true).HasMaxLength(50);
            builder.Property(c => c.password).IsRequired(true).HasMaxLength(250);
            builder.Property(c => c.phone).IsRequired(false).HasMaxLength(25);
            builder.Property(c => c.city).IsRequired(false).HasMaxLength(25);
            builder.Property(c => c.state).IsRequired(false).HasMaxLength(50);
            builder.Property(c => c.zipcode).IsRequired(false).HasMaxLength(15);
            builder.Property(c => c.country).IsRequired(false).HasMaxLength(30);
            builder.Property(c => c.active);

            builder.Property(c => c.personalPhoto).HasMaxLength(500);
            builder.Property(c => c.personalPhotoThumbnail).HasMaxLength(500);
            builder.Property(c => c.passwordResetToken).IsRequired(false).HasMaxLength(500);
            builder.Property(c => c.passwordResetTokenExpiry).IsRequired(false);
            builder.Property(c => c.inviteToken).IsRequired(false).HasMaxLength(500);
            builder.Property(c => c.inviteTokenExpiry).IsRequired(false);

            // Google Calendar Integration (NMJ-40, NMJ-41, NMJ-42)
            builder.Property(c => c.googleAccessToken).IsRequired(false).HasMaxLength(2000);
            builder.Property(c => c.googleRefreshToken).IsRequired(false).HasMaxLength(2000);
            builder.Property(c => c.googleTokenExpiry).IsRequired(false);
            builder.Property(c => c.googleCalendarLinked).HasDefaultValue(false);
            builder.Property(c => c.googleEmail).IsRequired(false).HasMaxLength(256);

            // Square Payment Integration
            builder.Property(c => c.squareAccessToken).IsRequired(false).HasMaxLength(2000);
            builder.Property(c => c.squareRefreshToken).IsRequired(false).HasMaxLength(2000);
            builder.Property(c => c.squareTokenExpiry).IsRequired(false);
            builder.Property(c => c.squareLinked).HasDefaultValue(false);
            builder.Property(c => c.squareMerchantId).IsRequired(false).HasMaxLength(256);
            builder.Property(c => c.squareLocationId).IsRequired(false).HasMaxLength(256);

            // Relationship configured in UserCompanyConfiguration
        }
    }
}
