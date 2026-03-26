using Application.Services;
using Xunit;

namespace Unit;

public class CgiQuestionGeneratorTests
{
    [Fact]
    public void GenerateQuestions_AddsContextQuestions_ForKnownPaths()
    {
        var generator = new CgiQuestionGenerator();
        var questions = generator.GenerateQuestions(new[] { "Controllers/HomeController.cs", "Migrations/001_init.sql" });

        Assert.Contains(questions, q => q.Contains("controller", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(questions, q => q.Contains("data-opslag", StringComparison.OrdinalIgnoreCase));
    }
}
