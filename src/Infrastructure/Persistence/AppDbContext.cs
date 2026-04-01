using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Repository> Repositories => Set<Repository>();
    public DbSet<RepositoryEnrollment> RepositoryEnrollments => Set<RepositoryEnrollment>();
    public DbSet<PushEvent> PushEvents => Set<PushEvent>();
    public DbSet<PushCommit> PushCommits => Set<PushCommit>();
    public DbSet<ChangedFile> ChangedFiles => Set<ChangedFile>();
    public DbSet<CgiSession> CgiSessions => Set<CgiSession>();
    public DbSet<CgiQuestion> CgiQuestions => Set<CgiQuestion>();
    public DbSet<CgiOutcome> CgiOutcomes => Set<CgiOutcome>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<RuleProfile> RuleProfiles => Set<RuleProfile>();
    public DbSet<StudentGroup> StudentGroups => Set<StudentGroup>();
    public DbSet<StudentGroupMembership> StudentGroupMemberships => Set<StudentGroupMembership>();
    public DbSet<WorkProcessAssessment> WorkProcessAssessments => Set<WorkProcessAssessment>();
    public DbSet<WorkProcessAssessmentScore> WorkProcessAssessmentScores => Set<WorkProcessAssessmentScore>();
    public DbSet<AssessmentAuditTrail> AssessmentAuditTrails => Set<AssessmentAuditTrail>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Student>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Teacher>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<CgiOutcome>().HasIndex(x => x.CgiSessionId).IsUnique();
        modelBuilder.Entity<PushEvent>().HasIndex(x => x.DeliveryId).IsUnique();
        modelBuilder.Entity<Repository>().HasIndex(x => x.GitHubRepoId).IsUnique();
        modelBuilder.Entity<StudentGroupMembership>()
            .HasIndex(x => new { x.StudentGroupId, x.StudentId })
            .IsUnique();
    }
}
