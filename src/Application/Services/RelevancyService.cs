using Domain.Entities;

namespace Application.Services;

public class RelevancyService
{
    public bool IsRelevant(RuleProfile? ruleProfile, string branch, int commitCount, int changedFileCount)
    {
        if (ruleProfile is null || !ruleProfile.IsActive)
            return true;

        if (!string.IsNullOrWhiteSpace(ruleProfile.BranchFilter) && !branch.EndsWith(ruleProfile.BranchFilter, StringComparison.OrdinalIgnoreCase))
            return false;

        if (commitCount < ruleProfile.MinCommitCount)
            return false;

        if (changedFileCount < ruleProfile.MinFilesChanged)
            return false;

        return true;
    }

    public string DeterminePriority(int commitCount, int changedFileCount)
    {
        if (changedFileCount >= 20 || commitCount >= 10)
            return "Hoog";
        if (changedFileCount >= 8 || commitCount >= 4)
            return "Midden";
        return "Laag";
    }
}
