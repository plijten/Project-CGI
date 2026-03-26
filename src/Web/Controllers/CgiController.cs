using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.ViewModels;

namespace Web.Controllers;

public class CgiController : Controller
{
    private readonly AppDbContext _dbContext;

    public CgiController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("cgi/{sessionId:guid}")]
    public async Task<IActionResult> Edit(Guid sessionId)
    {
        var session = await _dbContext.CgiSessions
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == sessionId);

        if (session is null)
            return NotFound();

        return View(new CgiEditVm
        {
            CgiSessionId = session.Id,
            Status = session.Status.ToString(),
            Questions = session.Questions.OrderBy(q => q.OrderNr).Select(q => q.QuestionText).ToList()
        });
    }

    [HttpPost("cgi/{sessionId:guid}")]
    public async Task<IActionResult> Save(Guid sessionId, CgiOutcomeInputVm input)
    {
        var session = await _dbContext.CgiSessions.FirstOrDefaultAsync(x => x.Id == sessionId);
        if (session is null)
            return NotFound();

        _dbContext.CgiOutcomes.Add(new CgiOutcome
        {
            CgiSessionId = sessionId,
            UnderstandingLevel = input.UnderstandingLevel,
            ArgumentationLevel = input.ArgumentationLevel,
            AiUseLevel = input.AiUseLevel,
            Notes = input.Notes,
            FollowUpAction = input.FollowUpAction
        });

        session.Status = CgiSessionStatus.Completed;
        await _dbContext.SaveChangesAsync();

        return RedirectToAction("Index", "Dashboard");
    }
}
