namespace GradingSystem.Models
{
    public class ClassSubject
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public int? TeacherId { get; set; }

        // Навигация
        public Class? Class { get; set; }
        public Subject? Subject { get; set; }
        public Teacher? Teacher { get; set; } 
        public ICollection<Grade> Grades { get; set; } = new List<Grade>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<ScheduleSlot> ScheduleSlots { get; set; } = new List<ScheduleSlot>();
    }
}
