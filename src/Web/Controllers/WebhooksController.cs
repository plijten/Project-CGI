using System.Net.Http;
using System.Text;
using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[ApiController]
[Route("webhooks/github")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookSignatureValidator _signatureValidator;
    private readonly IPushEventService _pushEventService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IWebhookSignatureValidator signatureValidator,
        IPushEventService pushEventService,
        IConfiguration configuration,
        ILogger<WebhooksController> logger)
    {
        _signatureValidator = signatureValidator;
        _pushEventService = pushEventService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        try
        {
            Request.EnableBuffering();

            var (rawPayload, payload) = await ReadPayloadsAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(payload))
                return BadRequest("Empty payload.");

            var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
            var secret = _configuration["GitHub:WebhookSecret"] ?? string.Empty;

            if (!_signatureValidator.IsValid(rawPayload, signature, secret))
                return Unauthorized("Invalid signature.");

            var eventType = Request.Headers["X-GitHub-Event"].FirstOrDefault();
            var deliveryId = Request.Headers["X-GitHub-Delivery"].FirstOrDefault();

            if (!string.Equals(eventType, "push", StringComparison.OrdinalIgnoreCase))
                return Accepted("Event ignored.");

            if (string.IsNullOrWhiteSpace(deliveryId))
                return BadRequest("Missing X-GitHub-Delivery header.");

            var processingToken = CancellationToken.None;

            if (await _pushEventService.IsDuplicateAsync(deliveryId, processingToken))
                return Ok("Duplicate delivery ignored.");

            var model = JsonSerializer.Deserialize<GitHubPushPayload>(payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (model is null)
                return BadRequest("Invalid payload.");

            await _pushEventService.HandlePushAsync(deliveryId, payload, model, processingToken);
            return Accepted("Push verwerkt.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook processing failed.");
            return Problem("Webhook processing failed.");
        }
    }

    private async Task<(string RawPayload, string JsonPayload)> ReadPayloadsAsync(CancellationToken cancellationToken)
    {
        if (Request.Body.CanSeek)
            Request.Body.Position = 0;

        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawPayload = await reader.ReadToEndAsync(cancellationToken);

        if (Request.Body.CanSeek)
            Request.Body.Position = 0;

        if (!Request.HasFormContentType)
            return (rawPayload, rawPayload);

        var form = await Request.ReadFormAsync(cancellationToken);
        var jsonPayload = form["payload"].FirstOrDefault() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(rawPayload))
            return (rawPayload, jsonPayload);

        var rebuiltRawPayload = await RebuildFormPayloadAsync(jsonPayload);
        return (rebuiltRawPayload, jsonPayload);
    }

    private static async Task<string> RebuildFormPayloadAsync(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return string.Empty;

        using var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("payload", payload)
        });

        return await formContent.ReadAsStringAsync();
    }
}
