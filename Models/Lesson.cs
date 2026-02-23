namespace GradingSystem.Models
{
    public class Lesson
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public int TeacherId { get; set; }
        public DateTime Date { get; set; }
        public string Topic { get; set; } = "";

        public Class? Class { get; set; }
        public Subject? Subject { get; set; }
        public Teacher? Teacher { get; set; }
    }
}