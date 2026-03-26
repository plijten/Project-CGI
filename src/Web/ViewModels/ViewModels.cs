namespace Web.ViewModels;

public class DashboardItemVm
{
    public Guid CgiSessionId { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? StudentId { get; set; }
    public Guid? TeacherId { get; set; }
}

public class PushDetailVm
{
    public Guid PushEventId { get; set; }
    public string DeliveryId { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string PusherName { get; set; } = string.Empty;
    public int CommitCount { get; set; }
    public List<PushCommitVm> Commits { get; set; } = new();
}

public class PushCommitVm
{
    public string Sha { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<string> ChangedFiles { get; set; } = new();
}

public class CgiEditVm
{
    public Guid CgiSessionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Questions { get; set; } = new();
}

public class CgiOutcomeInputVm
{
    public string UnderstandingLevel { get; set; } = string.Empty;
    public string ArgumentationLevel { get; set; } = string.Empty;
    public string AiUseLevel { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string FollowUpAction { get; set; } = string.Empty;
}
