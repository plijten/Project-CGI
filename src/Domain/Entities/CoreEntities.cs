using Domain.Enums;

namespace Domain.Entities;

public class Student
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
}

public class Teacher
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class Repository
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long GitHubRepoId { get; set; }
    public string Owner { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DefaultBranch { get; set; } = "main";
    public bool IsActive { get; set; } = true;
}

public class RepositoryEnrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RepositoryId { get; set; }
    public Guid StudentId { get; set; }
    public Guid TeacherId { get; set; }
    public string AssignmentName { get; set; } = string.Empty;
}

public class PushEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DeliveryId { get; set; } = string.Empty;
    public Guid? RepoId { get; set; }
    public string Branch { get; set; } = string.Empty;
    public DateTime PushedAtUtc { get; set; }
    public string PusherName { get; set; } = string.Empty;
    public int CommitCount { get; set; }
    public PushProcessingStatus Status { get; set; }
    public string RawPayloadJson { get; set; } = string.Empty;
    public List<PushCommit> Commits { get; set; } = new();
}

public class PushCommit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PushEventId { get; set; }
    public string Sha { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public List<ChangedFile> ChangedFiles { get; set; } = new();
}

public class ChangedFile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PushCommitId { get; set; }
    public string Path { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
}

public class CgiSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PushEventId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? TeacherId { get; set; }
    public CgiSessionStatus Status { get; set; } = CgiSessionStatus.Draft;
    public string Priority { get; set; } = "Normal";
    public DateTime? ScheduledAtUtc { get; set; }
    public List<CgiQuestion> Questions { get; set; } = new();
}

public class CgiQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CgiSessionId { get; set; }
    public int OrderNr { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string SourceType { get; set; } = "Template";
}

public class CgiOutcome
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CgiSessionId { get; set; }
    public string UnderstandingLevel { get; set; } = string.Empty;
    public string ArgumentationLevel { get; set; } = string.Empty;
    public string AiUseLevel { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string FollowUpAction { get; set; } = string.Empty;
}

public class NotificationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Recipient { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }
    public bool Success { get; set; }
    public string Reference { get; set; } = string.Empty;
}

public class RuleProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string BranchFilter { get; set; } = string.Empty;
    public int MinFilesChanged { get; set; }
    public int MinCommitCount { get; set; }
    public bool IsActive { get; set; } = true;
}
