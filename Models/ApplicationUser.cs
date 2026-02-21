using Microsoft.AspNetCore.Identity;
using NuGet.DependencyResolver;

namespace GradingSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        // Навигация — ако е учител
        public Teacher? Teacher { get; set; }

        // Навигация — ако е ученик
        public int? StudentId { get; set; }
        public Student? Student { get; set; }

    }
}
