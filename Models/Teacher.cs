namespace GradingSystem.Models
{
    public class Teacher
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        // Връзка с Identity User
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        // Навигация
        public ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();
    }
}
