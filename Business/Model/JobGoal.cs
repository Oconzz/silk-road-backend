using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;

namespace Business.Model;

public class JobGoal
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
    public Guid companyId { get; set; }
    public string title { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public decimal bonusAmount { get; set; }
    public string? completionBonus { get; set; } // Optional monetary reward title
    public DateTime targetDate { get; set; } // Deadline
    
    // Assignment - null means assigned to entire team
    public Guid? assignedToUserId { get; set; }
    public Guid? createdByUserId { get; set; }
    
    // Status tracking
    public string status { get; set; } = "pending"; // pending, completed, overdue
    public bool completed { get; set; }
    public DateTime? completedDate { get; set; }
    public Guid? completedBy { get; set; }
    public bool? completedOnTime { get; set; } // true = before deadline, false = after deadline
    
    // Verification (optional)
    public bool requiresVerification { get; set; }
    public bool verified { get; set; }
    public Guid? verifiedBy { get; set; }
    public DateTime? verifiedDate { get; set; }
    
    // Vote-based completion for team goals (assignedToUserId == null)
    public List<JobGoalVote> votes { get; set; } = new List<JobGoalVote>();
    
    // Ordering
    public int sortOrder { get; set; }
    
    public List<JobGoalMedia> media { get; set; } = new List<JobGoalMedia>();
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }

    // Navigation properties
    public Job? job { get; set; }
    public User? assignedToUser { get; set; }
    public User? createdByUser { get; set; }
    public User? completedByUser { get; set; }
    public User? verifiedByUser { get; set; }
}

public class JobGoalVote
{
    public Guid id { get; set; }
    public Guid goalId { get; set; }
    public Guid userId { get; set; }
    public bool voteComplete { get; set; }
    public DateTime voteDate { get; set; }
    public DateTime createDatetime { get; set; }
    
    // Navigation properties
    public JobGoal? goal { get; set; }
    public User? user { get; set; }
}

public class JobGoalMedia
{
    public Guid id { get; set; }
    public Guid goalId { get; set; }
    public string webUrl { get; set; } = string.Empty;
    public string thumbUrl { get; set; } = string.Empty;
    public DateTime createDatetime { get; set; }

    // Navigation properties
    public JobGoal? goal { get; set; }
}

public class JobGoalConfiguration : IEntityTypeConfiguration<JobGoal>
{
    public void Configure(EntityTypeBuilder<JobGoal> builder)
    {
        builder.HasKey(g => g.id);
        
        builder.Property(g => g.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(g => g.jobId)
            .IsRequired(true);
        
        builder.Property(g => g.companyId)
            .IsRequired(true);
        
        builder.Property(g => g.title)
            .IsRequired(true)
            .HasMaxLength(200);
        
        builder.Property(g => g.description)
            .IsRequired(false)
            .HasMaxLength(1000);
        
        builder.Property(g => g.bonusAmount)
            .IsRequired(true)
            .HasPrecision(10, 2);
        
        builder.Property(g => g.completionBonus)
            .IsRequired(false)
            .HasMaxLength(100);
        
        builder.Property(g => g.targetDate)
            .IsRequired(true);
        
        // Assignment fields
        builder.Property(g => g.assignedToUserId)
            .IsRequired(false);
        
        builder.Property(g => g.createdByUserId)
            .IsRequired(false);
        
        // Status tracking
        builder.Property(g => g.status)
            .IsRequired(true)
            .HasMaxLength(20)
            .HasDefaultValue("pending");
        
        builder.Property(g => g.completed)
            .IsRequired(true)
            .HasDefaultValue(false);
        
        builder.Property(g => g.completedDate)
            .IsRequired(false);
        
        builder.Property(g => g.completedBy)
            .IsRequired(false);
        
        builder.Property(g => g.completedOnTime)
            .IsRequired(false);
        
        // Verification
        builder.Property(g => g.requiresVerification)
            .IsRequired(true)
            .HasDefaultValue(false);
        
        builder.Property(g => g.verified)
            .IsRequired(true)
            .HasDefaultValue(false);
        
        builder.Property(g => g.verifiedBy)
            .IsRequired(false);
        
        builder.Property(g => g.verifiedDate)
            .IsRequired(false);
        
        // Ordering
        builder.Property(g => g.sortOrder)
            .HasDefaultValue(0);
        
        builder.Property(g => g.createDatetime)
            .IsRequired(true);
        
        builder.Property(g => g.updateDatetime)
            .IsRequired(true);

        // Relationships
        builder.HasOne(g => g.job)
            .WithMany()
            .HasForeignKey(g => g.jobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.assignedToUser)
            .WithMany()
            .HasForeignKey(g => g.assignedToUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(g => g.createdByUser)
            .WithMany()
            .HasForeignKey(g => g.createdByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(g => g.completedByUser)
            .WithMany()
            .HasForeignKey(g => g.completedBy)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(g => g.verifiedByUser)
            .WithMany()
            .HasForeignKey(g => g.verifiedBy)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(g => g.media)
            .WithOne(m => m.goal)
            .HasForeignKey(m => m.goalId);
        
        builder.HasMany(g => g.votes)
            .WithOne(v => v.goal)
            .HasForeignKey(v => v.goalId);

        // Indexes for fast lookups
        builder.HasIndex(g => new { g.jobId, g.status });
        builder.HasIndex(g => new { g.assignedToUserId, g.status });
        builder.HasIndex(g => new { g.companyId, g.status });
        builder.HasIndex(g => g.targetDate);
    }
}

public class JobGoalVoteConfiguration : IEntityTypeConfiguration<JobGoalVote>
{
    public void Configure(EntityTypeBuilder<JobGoalVote> builder)
    {
        builder.HasKey(v => v.id);
        
        builder.Property(v => v.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(v => v.goalId)
            .IsRequired(true);
        
        builder.Property(v => v.userId)
            .IsRequired(true);
        
        builder.Property(v => v.voteComplete)
            .IsRequired(true)
            .HasDefaultValue(false);
        
        builder.Property(v => v.voteDate)
            .IsRequired(true);
        
        builder.Property(v => v.createDatetime)
            .IsRequired(true);
        
        builder.HasOne(v => v.user)
            .WithMany()
            .HasForeignKey(v => v.userId)
            .OnDelete(DeleteBehavior.NoAction);
        
        // Unique constraint: one vote per user per goal
        builder.HasIndex(v => new { v.goalId, v.userId }).IsUnique();
    }
}

public class JobGoalMediaConfiguration : IEntityTypeConfiguration<JobGoalMedia>
{
    public void Configure(EntityTypeBuilder<JobGoalMedia> builder)
    {
        builder.HasKey(m => m.id);
        
        builder.Property(m => m.id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(m => m.goalId)
            .IsRequired(true);
        
        builder.Property(m => m.webUrl)
            .IsRequired(true)
            .HasMaxLength(500);
        
        builder.Property(m => m.thumbUrl)
            .IsRequired(true)
            .HasMaxLength(500);
        
        builder.Property(m => m.createDatetime)
            .IsRequired(true);
    }
}
