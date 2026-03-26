using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.ViewModels;

namespace Web.Controllers;

public class DashboardController : Controller
{
    private readonly AppDbContext _dbContext;

    public DashboardController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(string? status, string? className, string? branch)
    {
        var query = _dbContext.CgiSessions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status.ToString() == status);

        var sessions = await query
            .OrderByDescending(x => x.Id)
            .Take(100)
            .Select(x => new DashboardItemVm
            {
                CgiSessionId = x.Id,
                Priority = x.Priority,
                Status = x.Status.ToString(),
                StudentId = x.StudentId,
                TeacherId = x.TeacherId
            })
            .ToListAsync();

        return View(sessions);
    }
}
