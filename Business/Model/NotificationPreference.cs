using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Business.Model
{
    public class NotificationPreference
    {
        public Guid id { get; set; }
        public Guid userId { get; set; }
        public Guid companyId { get; set; }
        public bool enabled { get; set; } = true;
        public bool materialReturn { get; set; } = true;
        public bool timecardReminder { get; set; } = true;
        public bool jobAssignment { get; set; } = true;
        public bool rewardGoal { get; set; } = true;
        public bool reviewPrompt { get; set; } = true;
        public DateTime createDatetime { get; set; }
        public DateTime updateDatetime { get; set; }

        // Navigation
        public User? user { get; set; }
        public Company? company { get; set; }
    }

    public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
    {
        public void Configure(EntityTypeBuilder<NotificationPreference> builder)
        {
            builder.HasKey(n => n.id);
            builder.Property(n => n.id)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("gen_random_uuid()");
            builder.Property(n => n.enabled).HasDefaultValue(true);
            builder.Property(n => n.materialReturn).HasDefaultValue(true);
            builder.Property(n => n.timecardReminder).HasDefaultValue(true);
            builder.Property(n => n.jobAssignment).HasDefaultValue(true);
            builder.Property(n => n.rewardGoal).HasDefaultValue(true);
            builder.Property(n => n.reviewPrompt).HasDefaultValue(true);

            builder.HasOne(n => n.user)
                .WithMany()
                .HasForeignKey(n => n.userId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(n => n.company)
                .WithMany()
                .HasForeignKey(n => n.companyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(n => new { n.userId, n.companyId }).IsUnique();
        }
    }
}
