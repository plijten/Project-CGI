using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;

namespace Infrastructure.Notifications;

public class ConsoleEmailNotificationService : INotificationService
{
    private readonly AppDbContext _dbContext;

    public ConsoleEmailNotificationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task NotifyTeacherAsync(string email, string subject, string message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"E-mail naar {email}: {subject}\n{message}");

        _dbContext.NotificationLogs.Add(new NotificationLog
        {
            Recipient = email,
            Channel = "Email",
            SentAtUtc = DateTime.UtcNow,
            Success = true,
            Reference = subject
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
