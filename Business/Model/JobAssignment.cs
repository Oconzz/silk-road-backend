using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Business.Model;

/// <summary>
/// Represents an assignment of a team member to a job
/// </summary>
public class JobAssignment
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
    public Guid userId { get; set; }
    public Guid companyId { get; set; }
    public Guid assignedByUserId { get; set; }
    
    public string role { get; set; } = string.Empty; // Role at time of assignment
    public string notes { get; set; } = string.Empty;
    public bool active { get; set; } = true;
    
    public DateTime assignedDatetime { get; set; }
    public DateTime? unassignedDatetime { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }

    // Navigation properties
    public Job? job { get; set; }
    public User? user { get; set; }
    public User? assignedByUser { get; set; }
    public Company? company { get; set; }
}

public class JobAssignmentConfiguration : IEntityTypeConfiguration<JobAssignment>
{
    public void Configure(EntityTypeBuilder<JobAssignment> builder)
    {
        builder.HasKey(ja => ja.id);
        builder.Property(ja => ja.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(ja => ja.jobId).IsRequired();
        builder.Property(ja => ja.userId).IsRequired();
        builder.Property(ja => ja.companyId).IsRequired();
        builder.Property(ja => ja.assignedByUserId).IsRequired();
        
        builder.Property(ja => ja.role)
            .HasMaxLength(50)
            .IsRequired(false);
        
        builder.Property(ja => ja.notes)
            .HasMaxLength(500)
            .IsRequired(false);
        
        builder.Property(ja => ja.active)
            .HasDefaultValue(true);
        
        builder.Property(ja => ja.assignedDatetime);
        builder.Property(ja => ja.unassignedDatetime);
        builder.Property(ja => ja.createDatetime);
        builder.Property(ja => ja.updateDatetime);

        // Relationships
        builder.HasOne(ja => ja.job)
            .WithMany()
            .HasForeignKey(ja => ja.jobId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(ja => ja.user)
            .WithMany()
            .HasForeignKey(ja => ja.userId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(ja => ja.assignedByUser)
            .WithMany()
            .HasForeignKey(ja => ja.assignedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(ja => ja.company)
            .WithMany()
            .HasForeignKey(ja => ja.companyId)
            .OnDelete(DeleteBehavior.NoAction);

        // Index for fast lookups
        builder.HasIndex(ja => new { ja.jobId, ja.userId, ja.active });
        builder.HasIndex(ja => new { ja.userId, ja.active });
        builder.HasIndex(ja => ja.companyId);
    }
}
