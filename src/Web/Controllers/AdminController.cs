using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers;

public class AdminController : Controller
{
    private readonly AppDbContext _dbContext;

    public AdminController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Students() => View(await _dbContext.Students.ToListAsync());
    public async Task<IActionResult> Teachers() => View(await _dbContext.Teachers.ToListAsync());
    public async Task<IActionResult> Repositories() => View(await _dbContext.Repositories.ToListAsync());

    [HttpPost]
    public async Task<IActionResult> AddStudent(Student student)
    {
        _dbContext.Students.Add(student);
        await _dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Students));
    }

    [HttpPost]
    public async Task<IActionResult> AddTeacher(Teacher teacher)
    {
        _dbContext.Teachers.Add(teacher);
        await _dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Teachers));
    }

    [HttpPost]
    public async Task<IActionResult> AddRepository(Repository repository)
    {
        _dbContext.Repositories.Add(repository);
        await _dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Repositories));
    }
}
