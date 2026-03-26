using Application.Services;
using Domain.Entities;
using Xunit;

namespace Unit;

public class RelevancyServiceTests
{
    [Fact]
    public void IsRelevant_RespectsRuleProfileThresholds()
    {
        var service = new RelevancyService();
        var rule = new RuleProfile { BranchFilter = "main", MinCommitCount = 2, MinFilesChanged = 3, IsActive = true };

        var result = service.IsRelevant(rule, "feature/test", 1, 2);

        Assert.False(result);
    }
}
