namespace Application.Interfaces;

public interface INotificationService
{
    Task NotifyTeacherAsync(string email, string subject, string message, CancellationToken cancellationToken);
}
