namespace Application.Interfaces;

public interface IWebhookSignatureValidator
{
    bool IsValid(string payload, string? signatureHeader, string secret);
}
