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

    public WebhooksController(
        IWebhookSignatureValidator signatureValidator,
        IPushEventService pushEventService,
        IConfiguration configuration)
    {
        _signatureValidator = signatureValidator;
        _pushEventService = pushEventService;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
        var secret = _configuration["GitHub:WebhookSecret"] ?? string.Empty;

        if (!_signatureValidator.IsValid(payload, signature, secret))
            return Unauthorized("Invalid signature.");

        var eventType = Request.Headers["X-GitHub-Event"].FirstOrDefault();
        var deliveryId = Request.Headers["X-GitHub-Delivery"].FirstOrDefault();

        if (!string.Equals(eventType, "push", StringComparison.OrdinalIgnoreCase))
            return Accepted("Event ignored.");

        if (string.IsNullOrWhiteSpace(deliveryId))
            return BadRequest("Missing X-GitHub-Delivery header.");

        if (await _pushEventService.IsDuplicateAsync(deliveryId, cancellationToken))
            return Ok("Duplicate delivery ignored.");

        var model = JsonSerializer.Deserialize<GitHubPushPayload>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (model is null)
            return BadRequest("Invalid payload.");

        await _pushEventService.HandlePushAsync(deliveryId, payload, model, cancellationToken);
        return Accepted("Push verwerkt.");
    }
}
