using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public class OpenAiReviewService : IAiReviewService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiReviewService> _logger;

    public OpenAiReviewService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiReviewService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AutomatedCodeReviewResult?> ReviewCodeAsync(
        string repositoryName,
        string branch,
        IReadOnlyCollection<string> commitMessages,
        IReadOnlyCollection<string> changedFiles,
        CancellationToken cancellationToken)
    {
        if (!TryConfigureClient())
            return BuildFallbackReview(repositoryName, branch, commitMessages, changedFiles);

        var prompt = $$"""
            Analyseer deze GitHub push voor een CGI met studentreflectie.
            Repository: {{repositoryName}}
            Branch: {{branch}}
            Commit messages:
            {{string.Join(Environment.NewLine, commitMessages.Select(x => $"- {x}"))}}
            Gewijzigde bestanden:
            {{string.Join(Environment.NewLine, changedFiles.Select(x => $"- {x}"))}}

            Geef strikte JSON terug met deze velden:
            {
              "summary": "korte samenvatting van de wijziging",
              "teacherInsight": "analyse voor docent over niveau en risico",
              "suggestedAssessment": "Onvoldoende, Matig, Voldoende of Goed",
              "riskFlags": "signalen waarop docent moet letten",
              "reflectionQuestions": ["vraag1", "vraag2", "vraag3"]
            }
            Zorg dat reflectionQuestions precies 3 concrete reflectievragen bevat.
            """;

        var content = await SendPromptAsync(prompt, cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
            return BuildFallbackReview(repositoryName, branch, commitMessages, changedFiles);

        try
        {
            var result = JsonSerializer.Deserialize<AutomatedCodeReviewResult>(content, SerializerOptions);
            if (result is null)
                return BuildFallbackReview(repositoryName, branch, commitMessages, changedFiles);

            result.ReflectionQuestions = result.ReflectionQuestions.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Take(3).ToList();
            if (result.ReflectionQuestions.Count == 0)
                result.ReflectionQuestions = BuildFallbackQuestions(changedFiles).ToList();

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "OpenAI review response kon niet worden gelezen.");
            return BuildFallbackReview(repositoryName, branch, commitMessages, changedFiles);
        }
    }

    public async Task<IReadOnlyCollection<ReflectionAnswerReviewResult>> ReviewReflectionAnswersAsync(
        string repositoryName,
        IReadOnlyCollection<ReflectionAnswerInput> answers,
        CancellationToken cancellationToken)
    {
        var relevantAnswers = answers.Where(x => !string.IsNullOrWhiteSpace(x.Answer)).ToList();
        if (relevantAnswers.Count == 0)
            return [];

        if (!TryConfigureClient())
            return BuildFallbackAnswerReview(relevantAnswers);

        var prompt = $$"""
            Analyseer deze reflectie-antwoorden van een student voor repository {{repositoryName}}.
            Beoordeel per antwoord:
            - of het inhoudelijk is
            - of het mogelijk onzin of ontwijkend gedrag is
            - welke korte docentobservatie relevant is

            Antwoorden:
            {{JsonSerializer.Serialize(relevantAnswers, SerializerOptions)}}

            Geef strikte JSON terug als array met objecten:
            [
              {
                "questionId": "guid",
                "teacherInsight": "korte analyse",
                "isNonsense": true,
                "confidence": "Laag, Middel of Hoog"
              }
            ]
            """;

        var content = await SendPromptAsync(prompt, cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
            return BuildFallbackAnswerReview(relevantAnswers);

        try
        {
            var result = JsonSerializer.Deserialize<List<ReflectionAnswerReviewResult>>(content, SerializerOptions);
            return result is { Count: > 0 }
                ? result
                : BuildFallbackAnswerReview(relevantAnswers);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "OpenAI reflectie-analyse response kon niet worden gelezen.");
            return BuildFallbackAnswerReview(relevantAnswers);
        }
    }

    private bool TryConfigureClient()
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        var baseUrl = _configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/";

        if (string.IsNullOrWhiteSpace(apiKey))
            return false;

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return true;
    }

    private async Task<string> SendPromptAsync(string prompt, CancellationToken cancellationToken)
    {
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        var request = new
        {
            model,
            temperature = 0.2,
            messages = new object[]
            {
                new { role = "system", content = "Je bent een code review assistent voor docenten. Geef uitsluitend valide JSON terug." },
                new { role = "user", content = prompt }
            }
        };

        using var httpContent = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync("chat/completions", httpContent, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenAI call failed with status {StatusCode}", response.StatusCode);
            return string.Empty;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    private static AutomatedCodeReviewResult BuildFallbackReview(
        string repositoryName,
        string branch,
        IReadOnlyCollection<string> commitMessages,
        IReadOnlyCollection<string> changedFiles)
    {
        return new AutomatedCodeReviewResult
        {
            Summary = $"Push op {repositoryName} ({branch}) met {commitMessages.Count} commit(s) en {changedFiles.Count} gewijzigde bestanden.",
            TeacherInsight = "Automatische AI-analyse was niet beschikbaar. Gebruik de reflectievragen om het begrip van de student te toetsen.",
            SuggestedAssessment = changedFiles.Count > 8 ? "Matig" : "Voldoende",
            RiskFlags = changedFiles.Count > 10 ? "Veel bestanden gewijzigd; controleer eigenaarschap en consistentie." : "Geen directe rode vlaggen op basis van metadata.",
            ReflectionQuestions = BuildFallbackQuestions(changedFiles).ToList()
        };
    }

    private static IReadOnlyCollection<string> BuildFallbackQuestions(IReadOnlyCollection<string> changedFiles)
    {
        var fileList = changedFiles.Take(3).ToList();
        var firstFile = fileList.FirstOrDefault() ?? "deze wijziging";
        return new[]
        {
            $"Wat was het doel van je wijziging in {firstFile}?",
            "Welke afweging heb je gemaakt tussen verschillende oplossingsrichtingen?",
            "Hoe heb je gecontroleerd dat je wijziging correct werkt?"
        };
    }

    private static IReadOnlyCollection<ReflectionAnswerReviewResult> BuildFallbackAnswerReview(IReadOnlyCollection<ReflectionAnswerInput> answers)
        => answers.Select(x => new ReflectionAnswerReviewResult
        {
            QuestionId = x.QuestionId,
            TeacherInsight = BuildFallbackTeacherInsight(x.Answer),
            IsNonsense = LooksSuspicious(x.Answer),
            Confidence = LooksSuspicious(x.Answer) ? "Middel" : "Laag"
        }).ToList();

    private static string BuildFallbackTeacherInsight(string answer)
        => LooksSuspicious(answer)
            ? "Antwoord is erg summier of ontwijkend; vraag door op concrete keuzes en uitgevoerde controles."
            : "Antwoord bevat waarschijnlijk voldoende inhoud voor een vervolggesprek, maar controleer op concrete voorbeelden.";

    private static bool LooksSuspicious(string answer)
    {
        var normalized = answer.Trim().ToLowerInvariant();
        if (normalized.Length < 20)
            return true;

        string[] suspiciousTerms = ["geen idee", "weet ik niet", "idk", "gewoon", "nvt", "test", "asdf"];
        return suspiciousTerms.Any(normalized.Contains);
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
