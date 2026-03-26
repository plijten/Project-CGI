using Application.Interfaces;

namespace Application.Services;

public class CgiQuestionGenerator : ICgiQuestionGenerator
{
    private static readonly IReadOnlyCollection<string> BaseQuestions =
    [
        "Kun je de kern van deze wijziging stap voor stap uitleggen?",
        "Welke alternatieven heb je overwogen en waarom heb je deze keuze gemaakt?",
        "Welke tests of controles heb je uitgevoerd om de wijziging te valideren?"
    ];

    public IReadOnlyCollection<string> GenerateQuestions(IEnumerable<string> filePaths)
    {
        var questions = new List<string>(BaseQuestions);

        foreach (var path in filePaths.Select(p => p.ToLowerInvariant()).Distinct())
        {
            if (path.Contains("controllers/"))
                questions.Add("Waarom hoort deze logica in een controller en niet in een view of service?");
            if (path.Contains("models/") || path.Contains("entities/"))
                questions.Add("Welke verantwoordelijkheid heeft dit model binnen jouw oplossing?");
            if (path.Contains("views/") || path.EndsWith(".cshtml"))
                questions.Add("Hoe heb je de verdeling gemaakt tussen presentatie en logica?");
            if (path.EndsWith(".js") || path.EndsWith(".ts"))
                questions.Add("Welke interactie voeg je hier toe en waarom is dit client-side opgelost?");
            if (path.Contains("migrations/") || path.Contains("sql/"))
                questions.Add("Welke wijziging maak je in de data-opslag en waarom?");
        }

        return questions.Distinct().ToList();
    }
}
