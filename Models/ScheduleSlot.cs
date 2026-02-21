namespace GradingSystem.Models
{
    public class ScheduleSlot
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty; // "Понеделник"
        public int PeriodNumber { get; set; }   // 1, 2, 3... (пореден час)
        public TimeOnly StartTime { get; set; } // 08:00
        public TimeOnly EndTime { get; set; }   // 08:40

        // Навигация
        public Class? Class { get; set; } = null!;
        public Subject? Subject { get; set; } = null!;
    }
}
