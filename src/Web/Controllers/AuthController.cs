using System.Security.Claims;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Infrastructure;
using Web.ViewModels;

namespace Web.Controllers;

public class AuthController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly AuditService _auditService;
    private readonly PasswordHasher<string> _passwordHasher = new();

    public AuthController(AppDbContext dbContext, AuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["CanBootstrap"] = !_dbContext.Teachers.Any(x => !string.IsNullOrWhiteSpace(x.PasswordHash));
        return View(new LoginVm());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginVm input, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        var teacher = await _dbContext.Teachers.FirstOrDefaultAsync(x => x.Email == input.Email);
        if (teacher is not null && IsValidPassword(teacher.PasswordHash, input.Password, teacher.Email))
        {
            await SignInAsync(teacher.Id, teacher.Name, teacher.Email, "Teacher");
            await _auditService.LogAsync("Login", nameof(Teacher), teacher.Id.ToString(), $"{teacher.Email} heeft ingelogd.");
            return RedirectToLocal(returnUrl);
        }

        var student = await _dbContext.Students.FirstOrDefaultAsync(x => x.Email == input.Email);
        if (student is not null && IsValidPassword(student.PasswordHash, input.Password, student.Email))
        {
            await SignInAsync(student.Id, student.Name, student.Email, "Student");
            await _auditService.LogAsync("Login", nameof(Student), student.Id.ToString(), $"{student.Email} heeft ingelogd.");
            return RedirectToLocal(returnUrl);
        }

        input.ErrorMessage = "Ongeldige inloggegevens.";
        return View(input);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _auditService.LogAsync("Logout", "Sessie", details: "Gebruiker heeft uitgelogd.");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult BootstrapTeacher()
    {
        if (_dbContext.Teachers.Any(x => !string.IsNullOrWhiteSpace(x.PasswordHash)))
            return RedirectToAction(nameof(Login));

        return View(new TeacherInputVm());
    }

    [HttpPost]
    public async Task<IActionResult> BootstrapTeacher(TeacherInputVm input)
    {
        if (_dbContext.Teachers.Any(x => !string.IsNullOrWhiteSpace(x.PasswordHash)))
            return RedirectToAction(nameof(Login));

        var teacher = await _dbContext.Teachers.FirstOrDefaultAsync(x => x.Email == input.Email)
            ?? new Teacher
            {
                Name = input.Name,
                Email = input.Email
            };

        teacher.Name = input.Name;
        teacher.Email = input.Email;
        teacher.PasswordHash = _passwordHasher.HashPassword(input.Email, input.Password);

        if (_dbContext.Entry(teacher).State == EntityState.Detached)
            _dbContext.Teachers.Add(teacher);

        await _dbContext.SaveChangesAsync();
        await SignInAsync(teacher.Id, teacher.Name, teacher.Email, "Teacher");
        await _auditService.LogAsync("Bootstrap docent", nameof(Teacher), teacher.Id.ToString(), "Eerste docentaccount aangemaakt.");
        return RedirectToAction("Index", "Dashboard");
    }

    private async Task SignInAsync(Guid id, string name, string email, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, id.ToString()),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    private bool IsValidPassword(string passwordHash, string password, string salt)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrWhiteSpace(password))
            return false;

        var result = _passwordHasher.VerifyHashedPassword(salt, passwordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    private IActionResult RedirectToLocal(string? returnUrl)
        => Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction("Index", "Dashboard");
}
