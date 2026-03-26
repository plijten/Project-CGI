using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.ViewModels;

namespace Web.Controllers;

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

        var vm = new PushDetailVm
        {
            PushEventId = push.Id,
            DeliveryId = push.DeliveryId,
            Branch = push.Branch,
            PusherName = push.PusherName,
            CommitCount = push.CommitCount,
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
