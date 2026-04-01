using Application.DTOs;

namespace Application.Interfaces;

public interface IAiReviewService
{
    Task<AutomatedCodeReviewResult?> ReviewCodeAsync(
        string repositoryName,
        string branch,
        IReadOnlyCollection<string> commitMessages,
        IReadOnlyCollection<string> changedFiles,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ReflectionAnswerReviewResult>> ReviewReflectionAnswersAsync(
        string repositoryName,
        IReadOnlyCollection<ReflectionAnswerInput> answers,
        CancellationToken cancellationToken);
}
