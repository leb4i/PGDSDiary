namespace GradingSystem.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int SubjectId { get; set; }
        public DateOnly Date { get; set; }
        public string Status { get; set; } = string.Empty; // "Отсъства", "Закъснял", "Извинено"

        // Навигация
        public Student? Student { get; set; } = null!;
        public Subject? Subject { get; set; } = null!;
    }
}
