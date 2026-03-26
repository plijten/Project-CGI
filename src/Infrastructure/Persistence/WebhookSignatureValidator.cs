using System.Security.Cryptography;
using System.Text;
using Application.Interfaces;

namespace Infrastructure.Persistence;

public class WebhookSignatureValidator : IWebhookSignatureValidator
{
    public bool IsValid(string payload, string? signatureHeader, string secret)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) || !signatureHeader.StartsWith("sha256="))
            return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var calculated = "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(calculated),
            Encoding.UTF8.GetBytes(signatureHeader));
    }
}
