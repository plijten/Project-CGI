using Domain.Entities;
using Infrastructure.Persistence;
using Web.Infrastructure;

namespace Web.Infrastructure;

public class AuditService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string subjectType, string? subjectId = null, string? details = null, CancellationToken cancellationToken = default)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var actorName = user?.GetActorName() ?? "Systeem";
        var actorRole = user?.GetActorRole() ?? "Systeem";

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorName = actorName,
            ActorRole = actorRole,
            Action = action,
            SubjectType = subjectType,
            SubjectId = subjectId ?? string.Empty,
            Details = details ?? string.Empty
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task LogSystemAsync(string action, string subjectType, string? subjectId = null, string? details = null, CancellationToken cancellationToken = default)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorName = "GitHub webhook",
            ActorRole = "System",
            Action = action,
            SubjectType = subjectType,
            SubjectId = subjectId ?? string.Empty,
            Details = details ?? string.Empty
        });

        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
