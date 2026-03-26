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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<PushEvent>().HasIndex(x => x.DeliveryId).IsUnique();
        modelBuilder.Entity<Repository>().HasIndex(x => x.GitHubRepoId).IsUnique();
    }
}
