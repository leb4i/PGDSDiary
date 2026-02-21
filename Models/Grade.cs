namespace GradingSystem.Models
{
    public class Grade
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int SubjectId { get; set; }
        public decimal Value { get; set; }        // 2, 3, 4, 5, 6
        public string Type { get; set; } = string.Empty;  // "Изпит", "Тест", "Устен"
        public DateTime GradedAt { get; set; } = DateTime.Now;
        public string? Comment { get; set; }      // незадължителна бележка

        // Навигация
        public Student? Student { get; set; } = null!;
        public Subject? Subject { get; set; } = null!;
    }
}
