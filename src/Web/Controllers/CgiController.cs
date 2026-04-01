using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Infrastructure;
using Web.ViewModels;

namespace Web.Controllers;

[Authorize]
public class CgiController : Controller
{
    private readonly IAiReviewService _aiReviewService;
    private readonly AppDbContext _dbContext;
    private readonly AuditService _auditService;

    public CgiController(IAiReviewService aiReviewService, AppDbContext dbContext, AuditService auditService)
    {
        _aiReviewService = aiReviewService;
        _dbContext = dbContext;
        _auditService = auditService;
    }

    [HttpGet("cgi/{sessionId:guid}")]
    public async Task<IActionResult> Edit(Guid sessionId)
    {
        var session = await _dbContext.CgiSessions
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == sessionId);

        if (session is null)
            return NotFound();

        if (!CanAccessSession(session))
            return Forbid();

        var push = await _dbContext.PushEvents
            .Include(x => x.Commits)
                .ThenInclude(x => x.ChangedFiles)
            .FirstOrDefaultAsync(x => x.Id == session.PushEventId);
        var student = session.StudentId.HasValue ? await _dbContext.Students.FirstOrDefaultAsync(x => x.Id == session.StudentId.Value) : null;
        var teacher = session.TeacherId.HasValue ? await _dbContext.Teachers.FirstOrDefaultAsync(x => x.Id == session.TeacherId.Value) : null;
        var repository = push?.RepoId.HasValue == true ? await _dbContext.Repositories.FirstOrDefaultAsync(x => x.Id == push.RepoId.Value) : null;
        var outcome = await _dbContext.CgiOutcomes.FirstOrDefaultAsync(x => x.CgiSessionId == sessionId);

        var commits = push?.Commits.Select(c => new PushCommitVm
        {
            Sha = c.Sha,
            Message = c.Message,
            ChangedFiles = c.ChangedFiles.Select(f => $"{f.ChangeType}: {f.Path}").ToList()
        }).ToList() ?? new List<PushCommitVm>();

        var changedFiles = commits.SelectMany(x => x.ChangedFiles).Distinct().ToList();
        var orderedQuestions = session.Questions.OrderBy(q => q.OrderNr).ToList();

        return View(new CgiEditVm
        {
            CgiSessionId = session.Id,
            PushEventId = session.PushEventId,
            RepositoryName = repository is null ? "Onbekende repository" : $"{repository.Owner}/{repository.Name}",
            StudentName = student?.Name ?? "Nog niet gekoppeld",
            TeacherName = teacher?.Name ?? "Nog niet gekoppeld",
            Status = session.Status.ToString(),
            Priority = session.Priority,
            Branch = push?.Branch ?? string.Empty,
            CodeSummary = changedFiles.Count == 0
                ? "Geen codewijzigingen beschikbaar."
                : string.Join(Environment.NewLine, changedFiles),
            AiReviewSummary = session.AiReviewSummary,
            AiTeacherInsight = session.AiTeacherInsight,
            AiSuggestedAssessment = session.AiSuggestedAssessment,
            AiRiskFlags = session.AiRiskFlags,
            CanReview = User.IsTeacher() && (!session.TeacherId.HasValue || session.TeacherId == User.GetActorId()),
            CanReflect = User.IsStudent() && session.StudentId == User.GetActorId(),
            Questions = orderedQuestions.Select(q => new CgiQuestionVm
            {
                Id = q.Id,
                QuestionText = q.QuestionText,
                SourceType = q.SourceType,
                StudentAnswer = q.StudentAnswer,
                AnswerAnalysis = q.AnswerAnalysis,
                IsAnswerSuspect = q.IsAnswerSuspect,
                AnswerConfidence = q.AnswerConfidence
            }).ToList(),
            Commits = commits,
            Reflection = new CgiReflectionSubmissionVm
            {
                Answers = orderedQuestions.Select(q => new CgiReflectionAnswerVm
                {
                    QuestionId = q.Id,
                    QuestionText = q.QuestionText,
                    Answer = q.StudentAnswer
                }).ToList()
            },
            Outcome = new CgiOutcomeInputVm
            {
                Rating = outcome?.Rating?.ToString() ?? string.Empty,
                UnderstandingLevel = outcome?.UnderstandingLevel ?? string.Empty,
                ArgumentationLevel = outcome?.ArgumentationLevel ?? string.Empty,
                AiUseLevel = outcome?.AiUseLevel ?? string.Empty,
                Notes = outcome?.Notes ?? string.Empty,
                Tops = outcome?.Tops ?? string.Empty,
                Tips = outcome?.Tips ?? string.Empty,
                FollowUpAction = outcome?.FollowUpAction ?? string.Empty,
                MarkAsCompleted = session.Status == CgiSessionStatus.Completed
            }
        });
    }

    [Authorize(Roles = "Teacher")]
    [HttpPost("cgi/{sessionId:guid}")]
    public async Task<IActionResult> Save(Guid sessionId, CgiOutcomeInputVm input)
    {
        var session = await _dbContext.CgiSessions.FirstOrDefaultAsync(x => x.Id == sessionId);
        if (session is null)
            return NotFound();

        if (session.TeacherId.HasValue && session.TeacherId != User.GetActorId())
            return Forbid();

        var rating = Enum.TryParse<CgiAssessment>(input.Rating, true, out var parsedRating)
            ? parsedRating
            : (CgiAssessment?)null;

        var outcome = await _dbContext.CgiOutcomes.FirstOrDefaultAsync(x => x.CgiSessionId == sessionId)
            ?? new CgiOutcome
            {
                CgiSessionId = sessionId
            };

        outcome.Rating = rating;
        outcome.UnderstandingLevel = NormalizeText(input.UnderstandingLevel);
        outcome.ArgumentationLevel = NormalizeText(input.ArgumentationLevel);
        outcome.AiUseLevel = NormalizeText(input.AiUseLevel);
        outcome.Notes = NormalizeText(input.Notes);
        outcome.Tops = NormalizeText(input.Tops);
        outcome.Tips = NormalizeText(input.Tips);
        outcome.FollowUpAction = NormalizeText(input.FollowUpAction);
        outcome.ReviewedByTeacherId = User.GetActorId();
        outcome.ReviewedByName = User.GetActorName();
        outcome.ReviewedAtUtc = DateTime.UtcNow;

        if (_dbContext.Entry(outcome).State == EntityState.Detached)
            _dbContext.CgiOutcomes.Add(outcome);

        session.Status = input.MarkAsCompleted ? CgiSessionStatus.Completed : CgiSessionStatus.InProgress;
        await _dbContext.SaveChangesAsync();
        await _auditService.LogAsync("CGI beoordeeld", nameof(CgiSession), session.Id.ToString(), $"CGI is beoordeeld als {outcome.Rating?.ToString() ?? "Onbekend"} en status {session.Status}.");

        return RedirectToAction("Edit", new { sessionId });
    }

    [Authorize(Roles = "Student")]
    [HttpPost("cgi/{sessionId:guid}/reflect")]
    public async Task<IActionResult> SaveReflection(Guid sessionId, CgiReflectionSubmissionVm input)
    {
        var session = await _dbContext.CgiSessions
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == sessionId);
        if (session is null)
            return NotFound();

        if (session.StudentId != User.GetActorId())
            return Forbid();

        var questionLookup = session.Questions.ToDictionary(x => x.Id);
        var answersToReview = new List<ReflectionAnswerInput>();

        foreach (var answer in input.Answers)
        {
            if (!questionLookup.TryGetValue(answer.QuestionId, out var question))
                continue;

            question.StudentAnswer = NormalizeText(answer.Answer);
            question.AnsweredAtUtc = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(question.StudentAnswer))
            {
                answersToReview.Add(new ReflectionAnswerInput
                {
                    QuestionId = question.Id,
                    QuestionText = question.QuestionText,
                    Answer = question.StudentAnswer
                });
            }
        }

        var repositoryName = await ResolveRepositoryNameAsync(session.PushEventId);
        var reviews = await _aiReviewService.ReviewReflectionAnswersAsync(repositoryName, answersToReview, CancellationToken.None);
        var reviewLookup = reviews.ToDictionary(x => x.QuestionId);

        foreach (var question in session.Questions)
        {
            if (!reviewLookup.TryGetValue(question.Id, out var review))
                continue;

            question.AnswerAnalysis = review.TeacherInsight;
            question.IsAnswerSuspect = review.IsNonsense;
            question.AnswerConfidence = review.Confidence;
        }

        await _dbContext.SaveChangesAsync();
        await _auditService.LogAsync("Reflectie opgeslagen", nameof(CgiSession), sessionId.ToString(), $"Student heeft {answersToReview.Count} reflectie-antwoorden opgeslagen.");
        return RedirectToAction("Edit", new { sessionId });
    }

    private bool CanAccessSession(CgiSession session)
    {
        var actorId = User.GetActorId();
        if (!actorId.HasValue)
            return false;

        if (User.IsTeacher())
            return !session.TeacherId.HasValue || session.TeacherId == actorId;

        return User.IsStudent() && session.StudentId == actorId;
    }

    private async Task<string> ResolveRepositoryNameAsync(Guid pushEventId)
    {
        var push = await _dbContext.PushEvents.FirstOrDefaultAsync(x => x.Id == pushEventId);
        if (push?.RepoId is not Guid repoId)
            return "Onbekende repository";

        var repository = await _dbContext.Repositories.FirstOrDefaultAsync(x => x.Id == repoId);
        return repository is null ? "Onbekende repository" : $"{repository.Owner}/{repository.Name}";
    }

    private static string NormalizeText(string? value)
        => value?.Trim() ?? string.Empty;
}
