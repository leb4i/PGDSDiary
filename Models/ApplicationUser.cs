using Microsoft.AspNetCore.Identity;
using NuGet.DependencyResolver;
namespace GradingSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }

        public Teacher? Teacher { get; set; }
        public int? StudentId { get; set; }
        public Student? Student { get; set; }
    }
}