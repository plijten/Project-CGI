namespace Application.Interfaces;

public interface ICgiQuestionGenerator
{
    IReadOnlyCollection<string> GenerateQuestions(IEnumerable<string> filePaths);
}
