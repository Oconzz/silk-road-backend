using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Business.Model
{
    public class UserCompany
    {
        public Guid userId { get; set; }
        public User user { get; set; }

        public Guid companyId { get; set; }
        public Company company { get; set; }

        public string role { get; set; }
        public DateTime assignedDatetime { get; set; }
    }

    public class UserCompanyConfiguration : IEntityTypeConfiguration<UserCompany>
    {
        public void Configure(EntityTypeBuilder<UserCompany> builder)
        {
            builder.ToTable("UserCompany");
            
            // Composite primary key
            builder.HasKey(uc => new { uc.userId, uc.companyId });

            // Configure role property
            builder.Property(uc => uc.role)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(uc => uc.assignedDatetime)
                .IsRequired();

            // Configure relationships
            builder.HasOne(uc => uc.user)
                .WithMany(u => u.userCompanies)
                .HasForeignKey(uc => uc.userId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(uc => uc.company)
                .WithMany(c => c.userCompanies)
                .HasForeignKey(uc => uc.companyId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
