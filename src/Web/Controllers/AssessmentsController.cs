using System.Text.Json;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.ViewModels;

namespace Web.Controllers;

public class AssessmentsController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public AssessmentsController(AppDbContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    public async Task<IActionResult> Groups()
    {
        var groups = await _dbContext.StudentGroups
            .Select(g => new GroupListItemVm
            {
                Id = g.Id,
                Name = g.Name,
                SprintName = g.SprintName,
                TeacherName = _dbContext.Teachers.Where(t => t.Id == g.TeacherId).Select(t => t.Name).FirstOrDefault() ?? "Onbekend",
                StudentCount = _dbContext.StudentGroupMemberships.Count(m => m.StudentGroupId == g.Id)
            })
            .ToListAsync();

        var vm = new GroupManagementVm
        {
            Groups = groups,
            Students = await _dbContext.Students
                .Select(s => new StudentLookupVm { Id = s.Id, DisplayName = $"{s.Name} ({s.Number})" })
                .ToListAsync(),
            Teachers = await _dbContext.Teachers
                .Select(t => new TeacherLookupVm { Id = t.Id, DisplayName = t.Name })
                .ToListAsync()
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> AddGroup(string name, string sprintName, Guid teacherId, List<Guid> studentIds)
    {
        if (string.IsNullOrWhiteSpace(name) || teacherId == Guid.Empty)
        {
            return RedirectToAction(nameof(Groups));
        }

        var group = new StudentGroup
        {
            Name = name,
            SprintName = sprintName,
            TeacherId = teacherId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.StudentGroups.Add(group);

        foreach (var studentId in studentIds.Distinct())
        {
            _dbContext.StudentGroupMemberships.Add(new StudentGroupMembership
            {
                StudentGroupId = group.Id,
                StudentId = studentId
            });
        }

        await _dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(GroupDetail), new { id = group.Id });
    }

    public async Task<IActionResult> GroupDetail(Guid id)
    {
        var group = await _dbContext.StudentGroups.FirstOrDefaultAsync(x => x.Id == id);
        if (group is null)
        {
            return NotFound();
        }

        var framework = LoadFramework();
        var workProcesses = framework.Kerntaken
            .SelectMany(k => k.WorkProcesses)
            .Select(w => new FrameworkWorkProcessVm { Id = w.Id, Code = w.Code, Title = w.Title })
            .ToList();

        var studentNames = await _dbContext.StudentGroupMemberships
            .Where(m => m.StudentGroupId == id)
            .Join(_dbContext.Students, m => m.StudentId, s => s.Id, (_, s) => $"{s.Name} ({s.Number})")
            .ToListAsync();

        var assessments = await _dbContext.WorkProcessAssessments
            .Where(a => a.StudentGroupId == id)
            .OrderByDescending(a => a.LastUpdatedAtUtc)
            .Select(a => new AssessmentListItemVm
            {
                Id = a.Id,
                WorkProcessCode = a.WorkProcessCode,
                WorkProcessTitle = a.WorkProcessTitle,
                AssessmentType = a.AssessmentType,
                Grade = a.Grade,
                Passed = a.Passed,
                LastUpdatedAtUtc = a.LastUpdatedAtUtc
            })
            .ToListAsync();

        var teacher = await _dbContext.Teachers.FirstOrDefaultAsync(t => t.Id == group.TeacherId);
        var vm = new GroupDetailVm
        {
            GroupId = group.Id,
            GroupName = group.Name,
            SprintName = group.SprintName,
            TeacherName = teacher?.Name ?? "Onbekend",
            Students = studentNames,
            WorkProcesses = workProcesses,
            Assessments = assessments
        };

        return View(vm);
    }

    public async Task<IActionResult> Edit(Guid groupId, string workProcessId, string type = "Tussen", Guid? id = null)
    {
        var group = await _dbContext.StudentGroups.FirstOrDefaultAsync(g => g.Id == groupId);
        if (group is null)
        {
            return NotFound();
        }

        var framework = LoadFramework();
        var workProcess = framework.Kerntaken.SelectMany(k => k.WorkProcesses).FirstOrDefault(w => w.Id == workProcessId);
        if (workProcess is null)
        {
            return NotFound();
        }

        var vm = new AssessmentFormVm
        {
            AssessmentId = id,
            GroupId = groupId,
            GroupName = group.Name,
            WorkProcessId = workProcess.Id,
            WorkProcessCode = workProcess.Code,
            WorkProcessTitle = workProcess.Title,
            AssessmentType = type,
            Date = DateTime.UtcNow.Date,
            Criteria = workProcess.Criteria.Select(c => new CriterionInputVm
            {
                CriterionId = c.Id,
                CriterionCode = c.Code,
                Title = c.Title,
                IsCritical = c.IsCritical,
                Score = 0
            }).ToList()
        };

        if (id.HasValue)
        {
            var assessment = await _dbContext.WorkProcessAssessments
                .Include(a => a.Scores)
                .FirstOrDefaultAsync(a => a.Id == id.Value && a.StudentGroupId == groupId);

            if (assessment is not null)
            {
                vm.AssessmentType = assessment.AssessmentType;
                vm.Date = assessment.Date;
                vm.CandidateName = assessment.CandidateName;
                vm.StudentNumber = assessment.StudentNumber;
                vm.ClassName = assessment.ClassName;
                vm.Assessor1 = assessment.Assessor1;
                vm.Assessor2 = assessment.Assessor2;
                vm.Motivation = assessment.Motivation;
                vm.AuthenticityIsOwnWork = assessment.AuthenticityIsOwnWork ?? false;
                vm.AuthenticityNotes = assessment.AuthenticityNotes;

                foreach (var crit in vm.Criteria)
                {
                    var score = assessment.Scores.FirstOrDefault(s => s.CriterionId == crit.CriterionId);
                    if (score is not null)
                    {
                        crit.Score = score.Score;
                    }
                }
            }
        }

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Save(AssessmentFormVm vm)
    {
        var framework = LoadFramework();
        var workProcess = framework.Kerntaken.SelectMany(k => k.WorkProcesses).First(w => w.Id == vm.WorkProcessId);
        var totalPoints = vm.Criteria.Sum(c => c.Score);
        var gradeRow = workProcess.GradeTable.FirstOrDefault(g => g.Points == totalPoints);
        var grade = gradeRow?.Grade ?? 1.0m;
        var passed = gradeRow?.Passed ?? false;
        var actor = string.IsNullOrWhiteSpace(vm.PerformedBy) ? "Onbekend" : vm.PerformedBy;

        WorkProcessAssessment entity;
        var action = "Aangemaakt";

        if (vm.AssessmentId.HasValue)
        {
            entity = await _dbContext.WorkProcessAssessments
                .Include(a => a.Scores)
                .FirstAsync(a => a.Id == vm.AssessmentId.Value);
            action = "Aangepast";
        }
        else
        {
            entity = new WorkProcessAssessment
            {
                StudentGroupId = vm.GroupId,
                WorkProcessId = vm.WorkProcessId,
                WorkProcessCode = vm.WorkProcessCode,
                WorkProcessTitle = vm.WorkProcessTitle,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = actor
            };
            _dbContext.WorkProcessAssessments.Add(entity);
        }

        entity.AssessmentType = vm.AssessmentType;
        entity.Date = vm.Date;
        entity.CandidateName = vm.CandidateName;
        entity.StudentNumber = vm.StudentNumber;
        entity.ClassName = vm.ClassName;
        entity.Assessor1 = vm.Assessor1;
        entity.Assessor2 = vm.Assessor2;
        entity.Motivation = vm.Motivation;
        entity.AuthenticityIsOwnWork = vm.AuthenticityIsOwnWork;
        entity.AuthenticityNotes = vm.AuthenticityNotes;
        entity.TotalPoints = totalPoints;
        entity.Grade = grade;
        entity.Passed = passed;
        entity.LastUpdatedAtUtc = DateTime.UtcNow;
        entity.LastUpdatedBy = actor;

        _dbContext.WorkProcessAssessmentScores.RemoveRange(entity.Scores);
        entity.Scores = vm.Criteria.Select(c => new WorkProcessAssessmentScore
        {
            WorkProcessAssessmentId = entity.Id,
            CriterionId = c.CriterionId,
            CriterionCode = c.CriterionCode,
            CriterionTitle = c.Title,
            Score = c.Score
        }).ToList();

        _dbContext.AssessmentAuditTrails.Add(new AssessmentAuditTrail
        {
            WorkProcessAssessmentId = entity.Id,
            PerformedBy = actor,
            Action = action,
            Details = $"{entity.AssessmentType}-beoordeling {entity.WorkProcessCode} met totaal {totalPoints} punten en cijfer {grade}."
        });

        await _dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Detail), new { id = entity.Id });
    }

    public async Task<IActionResult> Detail(Guid id)
    {
        var assessment = await _dbContext.WorkProcessAssessments
            .Include(a => a.Scores)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assessment is null)
        {
            return NotFound();
        }

        var group = await _dbContext.StudentGroups.FirstOrDefaultAsync(g => g.Id == assessment.StudentGroupId);

        var vm = new AssessmentDetailVm
        {
            Id = assessment.Id,
            GroupName = group?.Name ?? "Onbekend",
            SprintName = group?.SprintName ?? "-",
            WorkProcessCode = assessment.WorkProcessCode,
            WorkProcessTitle = assessment.WorkProcessTitle,
            AssessmentType = assessment.AssessmentType,
            Grade = assessment.Grade,
            Passed = assessment.Passed,
            Motivation = assessment.Motivation,
            Assessor1 = assessment.Assessor1,
            Assessor2 = assessment.Assessor2,
            Date = assessment.Date,
            Scores = assessment.Scores.Select(s => new CriterionInputVm
            {
                CriterionId = s.CriterionId,
                CriterionCode = s.CriterionCode,
                Title = s.CriterionTitle,
                Score = s.Score
            }).ToList(),
            AuditTrail = await _dbContext.AssessmentAuditTrails
                .Where(a => a.WorkProcessAssessmentId == assessment.Id)
                .OrderByDescending(a => a.PerformedAtUtc)
                .Select(a => new AuditItemVm
                {
                    PerformedAtUtc = a.PerformedAtUtc,
                    PerformedBy = a.PerformedBy,
                    Action = a.Action,
                    Details = a.Details
                }).ToListAsync()
        };

        return View(vm);
    }

    public async Task<IActionResult> Student(Guid? studentId)
    {
        var student = studentId.HasValue
            ? await _dbContext.Students.FirstOrDefaultAsync(s => s.Id == studentId.Value)
            : await _dbContext.Students.OrderBy(s => s.Name).FirstOrDefaultAsync();

        if (student is null)
        {
            return View(new List<AssessmentListItemVm>());
        }

        var groupIds = await _dbContext.StudentGroupMemberships
            .Where(m => m.StudentId == student.Id)
            .Select(m => m.StudentGroupId)
            .ToListAsync();

        ViewBag.StudentName = student.Name;
        ViewBag.StudentId = student.Id;

        var list = await _dbContext.WorkProcessAssessments
            .Where(a => groupIds.Contains(a.StudentGroupId))
            .OrderByDescending(a => a.LastUpdatedAtUtc)
            .Select(a => new AssessmentListItemVm
            {
                Id = a.Id,
                WorkProcessCode = a.WorkProcessCode,
                WorkProcessTitle = a.WorkProcessTitle,
                AssessmentType = a.AssessmentType,
                Grade = a.Grade,
                Passed = a.Passed,
                LastUpdatedAtUtc = a.LastUpdatedAtUtc
            })
            .ToListAsync();

        return View(list);
    }

    private AssessmentFrameworkVm LoadFramework()
    {
        var path = Path.Combine(_environment.ContentRootPath, "Data", "software-development-assessment-framework.json");
        var json = System.IO.File.ReadAllText(path);
        return JsonSerializer.Deserialize<AssessmentFrameworkVm>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new AssessmentFrameworkVm();
    }
}

public class AssessmentFrameworkVm
{
    public List<KerntaakVm> Kerntaken { get; set; } = new();
}

public class KerntaakVm
{
    public List<FrameworkWorkProcessDefinitionVm> WorkProcesses { get; set; } = new();
}

public class FrameworkWorkProcessDefinitionVm
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<GradeRowVm> GradeTable { get; set; } = new();
    public List<CriterionDefinitionVm> Criteria { get; set; } = new();
}

public class GradeRowVm
{
    public int Points { get; set; }
    public decimal Grade { get; set; }
    public bool Passed { get; set; }
}

public class CriterionDefinitionVm
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsCritical { get; set; }
}
