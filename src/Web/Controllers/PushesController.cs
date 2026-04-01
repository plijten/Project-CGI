using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.ViewModels;

namespace Web.Controllers;

[Authorize]
public class PushesController : Controller
{
    private readonly AppDbContext _dbContext;

    public PushesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("pushes/{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var push = await _dbContext.PushEvents
            .Include(x => x.Commits)
                .ThenInclude(x => x.ChangedFiles)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (push is null)
            return NotFound();

        var session = await _dbContext.CgiSessions.FirstOrDefaultAsync(x => x.PushEventId == push.Id);
        var repository = push.RepoId.HasValue ? await _dbContext.Repositories.FirstOrDefaultAsync(x => x.Id == push.RepoId.Value) : null;
        var student = session?.StudentId.HasValue == true ? await _dbContext.Students.FirstOrDefaultAsync(x => x.Id == session.StudentId.Value) : null;
        var teacher = session?.TeacherId.HasValue == true ? await _dbContext.Teachers.FirstOrDefaultAsync(x => x.Id == session.TeacherId.Value) : null;

        var vm = new PushDetailVm
        {
            PushEventId = push.Id,
            RepositoryName = repository is null ? "Onbekende repository" : $"{repository.Owner}/{repository.Name}",
            DeliveryId = push.DeliveryId,
            Branch = push.Branch,
            PusherName = push.PusherName,
            CommitCount = push.CommitCount,
            StudentName = student?.Name ?? "Nog niet gekoppeld",
            TeacherName = teacher?.Name ?? "Nog niet gekoppeld",
            Commits = push.Commits.Select(c => new PushCommitVm
            {
                Sha = c.Sha,
                Message = c.Message,
                ChangedFiles = c.ChangedFiles.Select(f => $"{f.ChangeType}: {f.Path}").ToList()
            }).ToList()
        };

        return View(vm);
    }
}
