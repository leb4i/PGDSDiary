namespace GradingSystem.Models
{
    public class Class
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;  // "8А", "9А", "10А" и т.н.

        // Навигация
        public ICollection<Student> Students { get; set; } = new List<Student>();
        public ICollection<ScheduleSlot> ScheduleSlots { get; set; } = new List<ScheduleSlot>();
        public ICollection<ClassSubject> ClassSubject { get; set; } = new List<ClassSubject>();
    }
}
