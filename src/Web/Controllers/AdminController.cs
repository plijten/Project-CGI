using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Infrastructure;
using Web.ViewModels;

namespace Web.Controllers;

[Authorize(Roles = "Teacher")]
public class AdminController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly AuditService _auditService;
    private readonly PasswordHasher<string> _passwordHasher = new();

    public AdminController(AppDbContext dbContext, AuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public async Task<IActionResult> Students() => View(await _dbContext.Students.ToListAsync());
    public async Task<IActionResult> Teachers() => View(await _dbContext.Teachers.ToListAsync());
    public async Task<IActionResult> Repositories()
    {
        var enrollments = await (from enrollment in _dbContext.RepositoryEnrollments
                                 join repository in _dbContext.Repositories on enrollment.RepositoryId equals repository.Id
                                 join student in _dbContext.Students on enrollment.StudentId equals student.Id
                                 join teacher in _dbContext.Teachers on enrollment.TeacherId equals teacher.Id
                                 orderby repository.Owner, repository.Name
                                 select new RepositoryEnrollmentVm
                                 {
                                     RepositoryName = repository.Owner + "/" + repository.Name,
                                     StudentName = student.Name,
                                     TeacherName = teacher.Name,
                                     AssignmentName = enrollment.AssignmentName
                                 })
            .ToListAsync();

        return View(new RepositoriesPageVm
        {
            Repositories = await _dbContext.Repositories.OrderBy(x => x.Owner).ThenBy(x => x.Name).ToListAsync(),
            Students = await _dbContext.Students.OrderBy(x => x.Name).ToListAsync(),
            Teachers = await _dbContext.Teachers.OrderBy(x => x.Name).ToListAsync(),
            Enrollments = enrollments
        });
    }

    [HttpPost]
    public async Task<IActionResult> AddStudent(StudentInputVm input)
    {
        var student = new Student
        {
            Number = input.Number,
            Name = input.Name,
            Email = input.Email,
            ClassName = input.ClassName,
            PasswordHash = _passwordHasher.HashPassword(input.Email, input.Password)
        };

        _dbContext.Students.Add(student);
        await _dbContext.SaveChangesAsync();
        await _auditService.LogAsync("Student aangemaakt", nameof(Student), student.Id.ToString(), $"Student {student.Name} is toegevoegd.");
        return RedirectToAction(nameof(Students));
    }

    [HttpPost]
    public async Task<IActionResult> AddTeacher(TeacherInputVm input)
    {
        var teacher = new Teacher
        {
            Name = input.Name,
            Email = input.Email,
            PasswordHash = _passwordHasher.HashPassword(input.Email, input.Password)
        };

        _dbContext.Teachers.Add(teacher);
        await _dbContext.SaveChangesAsync();
        await _auditService.LogAsync("Docent aangemaakt", nameof(Teacher), teacher.Id.ToString(), $"Docent {teacher.Name} is toegevoegd.");
        return RedirectToAction(nameof(Teachers));
    }

    [HttpPost]
    public async Task<IActionResult> AddRepository(Repository repository)
    {
        _dbContext.Repositories.Add(repository);
        await _dbContext.SaveChangesAsync();
        await _auditService.LogAsync("Repository aangemaakt", nameof(Repository), repository.Id.ToString(), $"Repository {repository.Owner}/{repository.Name} is gekoppeld.");
        return RedirectToAction(nameof(Repositories));
    }

    [HttpPost]
    public async Task<IActionResult> AddEnrollment(RepositoryEnrollmentInputVm input)
    {
        var enrollment = new RepositoryEnrollment
        {
            RepositoryId = input.RepositoryId,
            StudentId = input.StudentId,
            TeacherId = input.TeacherId,
            AssignmentName = input.AssignmentName
        };

        _dbContext.RepositoryEnrollments.Add(enrollment);
        await _dbContext.SaveChangesAsync();
        await _auditService.LogAsync("Repository gekoppeld", nameof(RepositoryEnrollment), enrollment.Id.ToString(), $"Repository-inschrijving voor opdracht '{enrollment.AssignmentName}' is aangemaakt.");
        return RedirectToAction(nameof(Repositories));
    }
}
