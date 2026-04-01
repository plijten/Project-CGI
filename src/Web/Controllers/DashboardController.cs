using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Infrastructure;
using Web.ViewModels;

namespace Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly AppDbContext _dbContext;

    public DashboardController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(string? status, string? className, string? branch)
    {
        var actorId = User.GetActorId();
        var isTeacher = User.IsTeacher();

        var query = from session in _dbContext.CgiSessions
                    join push in _dbContext.PushEvents on session.PushEventId equals push.Id
                    join repository in _dbContext.Repositories on push.RepoId equals repository.Id into repositoryJoin
                    from repository in repositoryJoin.DefaultIfEmpty()
                    join student in _dbContext.Students on session.StudentId equals student.Id into studentJoin
                    from student in studentJoin.DefaultIfEmpty()
                    join teacher in _dbContext.Teachers on session.TeacherId equals teacher.Id into teacherJoin
                    from teacher in teacherJoin.DefaultIfEmpty()
                    join outcome in _dbContext.CgiOutcomes on session.Id equals outcome.CgiSessionId into outcomeJoin
                    from outcome in outcomeJoin.DefaultIfEmpty()
                    select new { session, push, repository, student, teacher, outcome };

        if (!isTeacher && actorId.HasValue)
            query = query.Where(x => x.session.StudentId == actorId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.session.Status.ToString() == status);

        if (!string.IsNullOrWhiteSpace(className))
            query = query.Where(x => x.student != null && x.student.ClassName == className);

        if (!string.IsNullOrWhiteSpace(branch))
            query = query.Where(x => x.push.Branch == branch);

        var sessions = await query
            .OrderByDescending(x => x.session.Id)
            .Take(100)
            .Select(x => new DashboardItemVm
            {
                CgiSessionId = x.session.Id,
                Title = x.repository != null ? x.repository.Owner + "/" + x.repository.Name : "Onbekende repository",
                RepositoryName = x.repository != null ? x.repository.Owner + "/" + x.repository.Name : "Onbekende repository",
                Priority = x.session.Priority,
                Status = x.session.Status.ToString(),
                StudentName = x.student != null ? x.student.Name : "Nog niet gekoppeld",
                TeacherName = x.teacher != null ? x.teacher.Name : "Nog niet gekoppeld",
                Assessment = x.outcome != null && x.outcome.Rating.HasValue ? x.outcome.Rating.Value.ToString() : "Nog niet beoordeeld",
                ClassName = x.student != null ? x.student.ClassName : string.Empty
            })
            .ToListAsync();

        return View(sessions);
    }
}
