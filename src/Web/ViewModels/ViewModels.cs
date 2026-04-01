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

public class GroupManagementVm
{
    public List<GroupListItemVm> Groups { get; set; } = new();
    public List<StudentLookupVm> Students { get; set; } = new();
    public List<TeacherLookupVm> Teachers { get; set; } = new();
}

public class GroupListItemVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SprintName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public int StudentCount { get; set; }
}

public class StudentLookupVm
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public class TeacherLookupVm
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public class GroupDetailVm
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string SprintName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public List<string> Students { get; set; } = new();
    public List<FrameworkWorkProcessVm> WorkProcesses { get; set; } = new();
    public List<AssessmentListItemVm> Assessments { get; set; } = new();
}

public class FrameworkWorkProcessVm
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

public class AssessmentListItemVm
{
    public Guid Id { get; set; }
    public string WorkProcessCode { get; set; } = string.Empty;
    public string WorkProcessTitle { get; set; } = string.Empty;
    public string AssessmentType { get; set; } = string.Empty;
    public decimal Grade { get; set; }
    public bool Passed { get; set; }
    public DateTime LastUpdatedAtUtc { get; set; }
}

public class AssessmentFormVm
{
    public Guid? AssessmentId { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string WorkProcessId { get; set; } = string.Empty;
    public string WorkProcessCode { get; set; } = string.Empty;
    public string WorkProcessTitle { get; set; } = string.Empty;
    public string AssessmentType { get; set; } = "Tussen";
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string Assessor1 { get; set; } = string.Empty;
    public string Assessor2 { get; set; } = string.Empty;
    public string Motivation { get; set; } = string.Empty;
    public bool AuthenticityIsOwnWork { get; set; }
    public string AuthenticityNotes { get; set; } = string.Empty;
    public List<CriterionInputVm> Criteria { get; set; } = new();
}

public class CriterionInputVm
{
    public string CriterionId { get; set; } = string.Empty;
    public string CriterionCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsCritical { get; set; }
    public int Score { get; set; }
}

public class AssessmentDetailVm
{
    public Guid Id { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string SprintName { get; set; } = string.Empty;
    public string WorkProcessCode { get; set; } = string.Empty;
    public string WorkProcessTitle { get; set; } = string.Empty;
    public string AssessmentType { get; set; } = string.Empty;
    public decimal Grade { get; set; }
    public bool Passed { get; set; }
    public string Motivation { get; set; } = string.Empty;
    public string Assessor1 { get; set; } = string.Empty;
    public string Assessor2 { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public List<CriterionInputVm> Scores { get; set; } = new();
    public List<AuditItemVm> AuditTrail { get; set; } = new();
}

public class AuditItemVm
{
    public DateTime PerformedAtUtc { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
