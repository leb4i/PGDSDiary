using System.Diagnostics;

namespace GradingSystem.Models
{
    public class Subject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;  // "Математика"
        public string? ShortName { get; set; }

        // Навигация
        public ICollection<Grade> Grades { get; set; } = new List<Grade>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<ScheduleSlot> ScheduleSlots { get; set; } = new List<ScheduleSlot>();
        public ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();
    }
}
