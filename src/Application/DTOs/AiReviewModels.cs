namespace Application.DTOs;

public class AutomatedCodeReviewResult
{
    public string Summary { get; set; } = string.Empty;
    public string TeacherInsight { get; set; } = string.Empty;
    public string SuggestedAssessment { get; set; } = string.Empty;
    public string RiskFlags { get; set; } = string.Empty;
    public List<string> ReflectionQuestions { get; set; } = new();
}

public class ReflectionAnswerInput
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

public class ReflectionAnswerReviewResult
{
    public Guid QuestionId { get; set; }
    public string TeacherInsight { get; set; } = string.Empty;
    public bool IsNonsense { get; set; }
    public string Confidence { get; set; } = string.Empty;
}
