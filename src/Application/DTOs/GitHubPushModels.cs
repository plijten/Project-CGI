namespace Application.DTOs;

public record GitHubPushPayload(
    string Ref,
    RepositoryPayload Repository,
    PusherPayload Pusher,
    IReadOnlyCollection<CommitPayload> Commits);

public record RepositoryPayload(long Id, string Name, OwnerPayload Owner, string Default_Branch);
public record OwnerPayload(string Name);
public record PusherPayload(string Name);
public record CommitPayload(
    string Id,
    string Message,
    DateTime Timestamp,
    AuthorPayload Author,
    IReadOnlyCollection<string> Added,
    IReadOnlyCollection<string> Modified,
    IReadOnlyCollection<string> Removed);
public record AuthorPayload(string Name);
