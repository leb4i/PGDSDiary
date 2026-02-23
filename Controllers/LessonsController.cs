using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GradingSystem.Data;
using GradingSystem.Models;

namespace GradingSystem.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class LessonsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LessonsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user!.Id);
            if (teacher == null) return View(new List<ScheduleSlot>());

            var today = DateTime.Now.DayOfWeek;
            var dayName = today switch
            {
                DayOfWeek.Monday => "Понеделник",
                DayOfWeek.Tuesday => "Вторник",
                DayOfWeek.Wednesday => "Сряда",
                DayOfWeek.Thursday => "Четвъртък",
                DayOfWeek.Friday => "Петък",
                _ => ""
            };

            var myClassSubjectIds = await _context.ClassSubjects
                .Where(cs => cs.TeacherId == teacher.Id)
                .Select(cs => new { cs.ClassId, cs.SubjectId })
                .ToListAsync();

            var classIds = myClassSubjectIds.Select(x => x.ClassId).ToList();
            var subjectIds = myClassSubjectIds.Select(x => x.SubjectId).ToList();

            var todaySlots = await _context.ScheduleSlots
                .Include(s => s.Class)
                .Include(s => s.Subject)
                .Where(s => s.DayOfWeek == dayName &&
                            classIds.Contains(s.ClassId) &&
                            subjectIds.Contains(s.SubjectId))
                .OrderBy(s => s.PeriodNumber)
                .ToListAsync();

            var todayLessons = await _context.Lessons
                .Where(l => l.TeacherId == teacher.Id && l.Date.Date == DateTime.Today)
                .ToListAsync();

            ViewBag.Teacher = teacher;
            ViewBag.TodayLessons = todayLessons;

            return View(todaySlots);
        }

        public async Task<IActionResult> Start(int classId, int subjectId)
        {
            var user = await _userManager.GetUserAsync(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user!.Id);

            var cls = await _context.Classes.FindAsync(classId);
            var subject = await _context.Subjects.FindAsync(subjectId);
            var students = await _context.Students
                .Where(s => s.ClassId == classId)
                .OrderBy(s => s.FirstName)
                .ToListAsync();

            var existingLesson = await _context.Lessons
                .FirstOrDefaultAsync(l => l.TeacherId == teacher!.Id &&
                                          l.ClassId == classId &&
                                          l.SubjectId == subjectId &&
                                          l.Date.Date == DateTime.Today);

            var today = DateOnly.FromDateTime(DateTime.Today);
            var existingAttendances = await _context.Attendances
                .Where(a => a.SubjectId == subjectId &&
                            a.Date == today &&
                            students.Select(s => s.Id).Contains(a.StudentId))
                .ToListAsync();

            var existingGrades = await _context.Grades
                .Where(g => g.SubjectId == subjectId &&
                            g.GradedAt.Date == DateTime.Today &&
                            students.Select(s => s.Id).Contains(g.StudentId))
                .ToListAsync();

            ViewBag.Class = cls;
            ViewBag.Subject = subject;
            ViewBag.Teacher = teacher;
            ViewBag.Students = students;
            ViewBag.Lesson = existingLesson;
            ViewBag.ExistingAttendances = existingAttendances;
            ViewBag.ExistingGrades = existingGrades;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Save(int classId, int subjectId, string topic,
            List<int> absentStudents, List<int> lateStudents,
            [FromForm] Dictionary<int, string> grades,
            [FromForm] Dictionary<int, string> gradeTypes)
        {
            var user = await _userManager.GetUserAsync(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user!.Id);

            // Запази или обнови урока
            var lesson = await _context.Lessons
                .FirstOrDefaultAsync(l => l.TeacherId == teacher!.Id &&
                                          l.ClassId == classId &&
                                          l.SubjectId == subjectId &&
                                          l.Date.Date == DateTime.Today);

            if (lesson == null)
            {
                lesson = new Lesson
                {
                    ClassId = classId,
                    SubjectId = subjectId,
                    TeacherId = teacher!.Id,
                    Date = DateTime.Now,
                    Topic = topic
                };
                _context.Lessons.Add(lesson);
            }
            else
            {
                lesson.Topic = topic;
                _context.Lessons.Update(lesson);
            }

            // Запази отсъствия
            var today = DateOnly.FromDateTime(DateTime.Today);
            foreach (var studentId in absentStudents)
            {
                if (!await _context.Attendances.AnyAsync(a =>
                    a.StudentId == studentId && a.SubjectId == subjectId && a.Date == today))
                {
                    _context.Attendances.Add(new Attendance
                    {
                        StudentId = studentId,
                        SubjectId = subjectId,
                        Date = today,
                        Status = "Отсъства"
                    });
                }
            }

            foreach (var studentId in lateStudents)
            {
                if (!await _context.Attendances.AnyAsync(a =>
                    a.StudentId == studentId && a.SubjectId == subjectId && a.Date == today))
                {
                    _context.Attendances.Add(new Attendance
                    {
                        StudentId = studentId,
                        SubjectId = subjectId,
                        Date = today,
                        Status = "Закъснял"
                    });
                }
            }

            // Запази оценки
            foreach (var (studentId, valueStr) in grades)
            {
                if (decimal.TryParse(valueStr,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal value) && value > 0)
                {
                    _context.Grades.Add(new Grade
                    {
                        StudentId = studentId,
                        SubjectId = subjectId,
                        Value = value,
                        Type = gradeTypes.ContainsKey(studentId) ? gradeTypes[studentId] : "Устен",
                        GradedAt = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Часът е запазен успешно!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> History()
        {
            var user = await _userManager.GetUserAsync(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user!.Id);

            var lessons = await _context.Lessons
                .Include(l => l.Class)
                .Include(l => l.Subject)
                .Where(l => l.TeacherId == teacher!.Id)
                .OrderByDescending(l => l.Date)
                .ToListAsync();

            var gradesCounts = new Dictionary<int, int>();
            var attendanceCounts = new Dictionary<int, int>();

            foreach (var l in lessons)
            {
                var date = DateOnly.FromDateTime(l.Date.Date);
                gradesCounts[l.Id] = await _context.Grades
                    .CountAsync(g => g.SubjectId == l.SubjectId &&
                                     g.GradedAt.Date == l.Date.Date);
                attendanceCounts[l.Id] = await _context.Attendances
                    .CountAsync(a => a.SubjectId == l.SubjectId &&
                                     a.Date == date);
            }

            ViewBag.GradesCounts = gradesCounts;
            ViewBag.AttendanceCounts = attendanceCounts;

            return View(lessons);
        }
    }
}