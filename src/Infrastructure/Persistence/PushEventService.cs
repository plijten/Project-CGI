using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class PushEventService : IPushEventService
{
    private readonly AppDbContext _dbContext;
    private readonly IAiReviewService _aiReviewService;
    private readonly ICgiQuestionGenerator _questionGenerator;
    private readonly INotificationService _notificationService;
    private readonly RelevancyService _relevancyService;

    public PushEventService(
        AppDbContext dbContext,
        IAiReviewService aiReviewService,
        ICgiQuestionGenerator questionGenerator,
        INotificationService notificationService,
        RelevancyService relevancyService)
    {
        _dbContext = dbContext;
        _aiReviewService = aiReviewService;
        _questionGenerator = questionGenerator;
        _notificationService = notificationService;
        _relevancyService = relevancyService;
    }

    public Task<bool> IsDuplicateAsync(string deliveryId, CancellationToken cancellationToken)
        => _dbContext.PushEvents.AnyAsync(x => x.DeliveryId == deliveryId, cancellationToken);

    public async Task<Guid> HandlePushAsync(string deliveryId, string rawPayload, GitHubPushPayload payload, CancellationToken cancellationToken)
    {
        var branch = payload.Ref.Replace("refs/heads/", string.Empty, StringComparison.OrdinalIgnoreCase);

        var repo = await _dbContext.Repositories.FirstOrDefaultAsync(r => r.GitHubRepoId == payload.Repository.Id, cancellationToken);
        var pushEvent = new PushEvent
        {
            DeliveryId = deliveryId,
            RepoId = repo?.Id,
            Branch = branch,
            PushedAtUtc = DateTime.UtcNow,
            PusherName = payload.Pusher.Name,
            CommitCount = payload.Commits.Count,
            RawPayloadJson = rawPayload,
            Status = repo is null ? PushProcessingStatus.UnmappedRepository : PushProcessingStatus.Received
        };

        foreach (var commit in payload.Commits)
        {
            var pushCommit = new PushCommit
            {
                PushEventId = pushEvent.Id,
                Sha = commit.Id,
                Message = commit.Message,
                TimestampUtc = commit.Timestamp,
                AuthorName = commit.Author.Name
            };

            foreach (var path in commit.Added)
                pushCommit.ChangedFiles.Add(new ChangedFile { PushCommitId = pushCommit.Id, Path = path, ChangeType = "added" });
            foreach (var path in commit.Modified)
                pushCommit.ChangedFiles.Add(new ChangedFile { PushCommitId = pushCommit.Id, Path = path, ChangeType = "modified" });
            foreach (var path in commit.Removed)
                pushCommit.ChangedFiles.Add(new ChangedFile { PushCommitId = pushCommit.Id, Path = path, ChangeType = "removed" });

            pushEvent.Commits.Add(pushCommit);
        }

        _dbContext.PushEvents.Add(pushEvent);

        var enrollment = repo is null
            ? null
            : await _dbContext.RepositoryEnrollments.FirstOrDefaultAsync(e => e.RepositoryId == repo.Id, cancellationToken);

        var ruleProfile = await _dbContext.RuleProfiles.FirstOrDefaultAsync(r => r.IsActive, cancellationToken);
        var filePaths = pushEvent.Commits.SelectMany(c => c.ChangedFiles).Select(c => c.Path).ToList();
        var relevant = _relevancyService.IsRelevant(ruleProfile, branch, payload.Commits.Count, filePaths.Count);

        var session = new CgiSession
        {
            PushEventId = pushEvent.Id,
            StudentId = enrollment?.StudentId,
            TeacherId = enrollment?.TeacherId,
            Priority = _relevancyService.DeterminePriority(payload.Commits.Count, filePaths.Count),
            Status = relevant ? CgiSessionStatus.Draft : CgiSessionStatus.Cancelled
        };

        var questions = _questionGenerator.GenerateQuestions(filePaths);
        var order = 1;
        foreach (var question in questions)
        {
            session.Questions.Add(new CgiQuestion
            {
                CgiSessionId = session.Id,
                OrderNr = order++,
                QuestionText = question,
                SourceType = "Generated"
            });
        }

        var aiReview = await _aiReviewService.ReviewCodeAsync(
            payload.Repository.Name,
            branch,
            payload.Commits.Select(x => x.Message).ToList(),
            filePaths,
            cancellationToken);

        if (aiReview is not null)
        {
            session.AiReviewSummary = aiReview.Summary;
            session.AiTeacherInsight = aiReview.TeacherInsight;
            session.AiSuggestedAssessment = aiReview.SuggestedAssessment;
            session.AiRiskFlags = aiReview.RiskFlags;
            session.AiReviewedAtUtc = DateTime.UtcNow;

            foreach (var reflectionQuestion in aiReview.ReflectionQuestions.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                session.Questions.Add(new CgiQuestion
                {
                    CgiSessionId = session.Id,
                    OrderNr = order++,
                    QuestionText = reflectionQuestion,
                    SourceType = "AIReflection"
                });
            }
        }

        _dbContext.CgiSessions.Add(session);
        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorName = "GitHub webhook",
            ActorRole = "System",
            Action = "Push ontvangen",
            SubjectType = nameof(PushEvent),
            SubjectId = pushEvent.Id.ToString(),
            Details = $"Webhook voor {payload.Repository.Owner.Name}/{payload.Repository.Name} op branch {branch} verwerkt."
        });
        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorName = "GitHub webhook",
            ActorRole = "System",
            Action = "CGI aangemaakt",
            SubjectType = nameof(CgiSession),
            SubjectId = session.Id.ToString(),
            Details = $"CGI-sessie aangemaakt voor {payload.Repository.Name} met prioriteit {session.Priority}."
        });
        if (aiReview is not null)
        {
            _dbContext.AuditLogs.Add(new AuditLog
            {
                ActorName = "GitHub webhook",
                ActorRole = "System",
                Action = "AI code review uitgevoerd",
                SubjectType = nameof(CgiSession),
                SubjectId = session.Id.ToString(),
                Details = $"Automatische review met advies '{aiReview.SuggestedAssessment}' gegenereerd."
            });
        }

        if (relevant && enrollment?.TeacherId is Guid teacherId)
        {
            var teacher = await _dbContext.Teachers.FirstOrDefaultAsync(t => t.Id == teacherId, cancellationToken);
            if (teacher is not null)
            {
                await _notificationService.NotifyTeacherAsync(
                    teacher.Email,
                    $"Nieuwe push van {payload.Repository.Name}",
                    $"Student heeft {payload.Commits.Count} commits gepusht op branch {branch}.",
                    cancellationToken);
            }
        }

        pushEvent.Status = repo is null ? PushProcessingStatus.UnmappedRepository : PushProcessingStatus.Processed;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return pushEvent.Id;
    }
}
