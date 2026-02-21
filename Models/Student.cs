using System.Diagnostics;

namespace GradingSystem.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int ClassId { get; set; }  // към кой клас принадлежи

        // Навигация
        public Class? Class { get; set; } = null!;
        public ICollection<Grade> Grades { get; set; } = new List<Grade>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    }
}
