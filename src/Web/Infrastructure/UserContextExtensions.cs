using System.Security.Claims;

namespace Web.Infrastructure;

public static class UserContextExtensions
{
    public static Guid? GetActorId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static string GetActorName(this ClaimsPrincipal principal)
        => principal.Identity?.Name ?? "Onbekend";

    public static string GetActorRole(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Role) ?? "Anoniem";

    public static bool IsTeacher(this ClaimsPrincipal principal)
        => principal.IsInRole("Teacher");

    public static bool IsStudent(this ClaimsPrincipal principal)
        => principal.IsInRole("Student");
}
