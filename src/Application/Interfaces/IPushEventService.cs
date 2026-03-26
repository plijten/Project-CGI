using Application.DTOs;

namespace Application.Interfaces;

public interface IPushEventService
{
    Task<bool> IsDuplicateAsync(string deliveryId, CancellationToken cancellationToken);
    Task<Guid> HandlePushAsync(string deliveryId, string rawPayload, GitHubPushPayload payload, CancellationToken cancellationToken);
}
