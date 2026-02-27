using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GradingSystem.Data;
using GradingSystem.Models;

namespace GradingSystem.Controllers
{
    [Authorize]
    public class StatisticsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StatisticsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Admin"))
                return await AdminStats();
            if (User.IsInRole("Teacher"))
                return await TeacherStats();
            return await StudentStats();
        }

        private async Task<IActionResult> AdminStats()
        {
            // Успех по клас
            var classesList = await _context.Classes.ToListAsync();
            var classAverages = new List<object>();
            foreach (var c in classesList)
            {
                var avg = await _context.Grades
                    .Where(g => g.Student!.ClassId == c.Id)
                    .AverageAsync(g => (double?)g.Value) ?? 0;
                classAverages.Add(new { name = c.Name, average = Math.Round(avg, 2) });
            }

            // Успех по предмет
            var subjectAverages = await _context.Grades
                .GroupBy(g => new { g.Subject!.Name, g.Subject.ShortName })
                .Select(g => new {
                    name = g.Key.Name,
                    shortName = g.Key.ShortName,
                    average = Math.Round(g.Average(x => (double)x.Value), 2),
                    count = g.Count()
                })
                .OrderByDescending(x => x.average)
                .ToListAsync();

            // Най-силни ученици
            var topStudents = await _context.Students
                .Include(s => s.Class)
                .Select(s => new {
                    id = s.Id,
                    name = s.FirstName + " " + s.LastName,
                    className = s.Class!.Name,
                    average = s.Grades!.Any() ? Math.Round(s.Grades.Average(g => (double)g.Value), 2) : 0.0
                })
                .Where(s => s.average > 0)
                .OrderByDescending(s => s.average)
                .Take(10)
                .ToListAsync();

            // Най-слаби ученици
            var weakStudents = await _context.Students
                .Include(s => s.Class)
                .Select(s => new {
                    id = s.Id,
                    name = s.FirstName + " " + s.LastName,
                    className = s.Class!.Name,
                    average = s.Grades!.Any() ? Math.Round(s.Grades.Average(g => (double)g.Value), 2) : 0.0
                })
                .Where(s => s.average > 0)
                .OrderBy(s => s.average)
                .Take(10)
                .ToListAsync();

            // Отсъствия по клас
            var absencesByClass = new List<object>();
            foreach (var c in classesList)
            {
                var count = await _context.Attendances
                    .Where(a => a.Student!.ClassId == c.Id && a.Status == "Отсъства")
                    .CountAsync();
                absencesByClass.Add(new { name = c.Name, count });
            }

            // Динамика на успеха по месец
            var gradesByMonth = await _context.Grades
                .GroupBy(g => new { g.GradedAt.Year, g.GradedAt.Month })
                .Select(g => new {
                    year = g.Key.Year,
                    month = g.Key.Month,
                    average = Math.Round(g.Average(x => (double)x.Value), 2)
                })
                .OrderBy(g => g.year).ThenBy(g => g.month)
                .Take(12)
                .ToListAsync();

            ViewBag.ClassAverages = classAverages;
            ViewBag.SubjectAverages = subjectAverages;
            ViewBag.TopStudents = topStudents;
            ViewBag.WeakStudents = weakStudents;
            ViewBag.AbsencesByClass = absencesByClass;
            ViewBag.GradesByMonth = gradesByMonth;

            return View("AdminStats");
        }

        private async Task<IActionResult> TeacherStats()
        {
            var user = await _userManager.GetUserAsync(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user!.Id);
            if (teacher == null) return View("AdminStats");

            var mySubjectIds = await _context.ClassSubjects
                .Where(cs => cs.TeacherId == teacher.Id)
                .Select(cs => cs.SubjectId)
                .Distinct()
                .ToListAsync();

            var myClassIds = await _context.ClassSubjects
                .Where(cs => cs.TeacherId == teacher.Id)
                .Select(cs => cs.ClassId)
                .Distinct()
                .ToListAsync();

            // Успех по предмет
            var subjectAverages = await _context.Grades
                .Where(g => mySubjectIds.Contains(g.SubjectId))
                .GroupBy(g => new { g.Subject!.Name, g.Subject.ShortName })
                .Select(g => new {
                    name = g.Key.Name,
                    shortName = g.Key.ShortName,
                    average = Math.Round(g.Average(x => (double)x.Value), 2),
                    count = g.Count()
                })
                .OrderByDescending(x => x.average)
                .ToListAsync();

            // Успех по клас
            var classes = await _context.Classes
                .Where(c => myClassIds.Contains(c.Id))
                .ToListAsync();

            var classAverages = new List<object>();
            foreach (var c in classes)
            {
                var avg = await _context.Grades
                    .Where(g => g.Student!.ClassId == c.Id && mySubjectIds.Contains(g.SubjectId))
                    .AverageAsync(g => (double?)g.Value) ?? 0;
                classAverages.Add(new { name = c.Name, average = Math.Round(avg, 2) });
            }

            // Топ и слаби ученици
            var topStudents = await _context.Students
                .Where(s => myClassIds.Contains(s.ClassId))
                .Include(s => s.Class)
                .Select(s => new {
                    id = s.Id,
                    name = s.FirstName + " " + s.LastName,
                    className = s.Class!.Name,
                    average = s.Grades!.Where(g => mySubjectIds.Contains(g.SubjectId)).Any()
                        ? Math.Round(s.Grades.Where(g => mySubjectIds.Contains(g.SubjectId)).Average(g => (double)g.Value), 2)
                        : 0.0
                })
                .Where(s => s.average > 0)
                .OrderByDescending(s => s.average)
                .Take(10)
                .ToListAsync();

            var weakStudents = await _context.Students
                .Where(s => myClassIds.Contains(s.ClassId))
                .Include(s => s.Class)
                .Select(s => new {
                    id = s.Id,
                    name = s.FirstName + " " + s.LastName,
                    className = s.Class!.Name,
                    average = s.Grades!.Where(g => mySubjectIds.Contains(g.SubjectId)).Any()
                        ? Math.Round(s.Grades.Where(g => mySubjectIds.Contains(g.SubjectId)).Average(g => (double)g.Value), 2)
                        : 0.0
                })
                .Where(s => s.average > 0)
                .OrderBy(s => s.average)
                .Take(10)
                .ToListAsync();

            // Отсъствия по клас
            var absencesByClass = new List<object>();
            foreach (var c in classes)
            {
                var count = await _context.Attendances
                    .Where(a => a.Student!.ClassId == c.Id &&
                                mySubjectIds.Contains(a.SubjectId) &&
                                a.Status == "Отсъства")
                    .CountAsync();
                absencesByClass.Add(new { name = c.Name, count });
            }

            // Динамика по месец
            var gradesByMonth = await _context.Grades
                .Where(g => mySubjectIds.Contains(g.SubjectId))
                .GroupBy(g => new { g.GradedAt.Year, g.GradedAt.Month })
                .Select(g => new {
                    year = g.Key.Year,
                    month = g.Key.Month,
                    average = Math.Round(g.Average(x => (double)x.Value), 2)
                })
                .OrderBy(g => g.year).ThenBy(g => g.month)
                .Take(12)
                .ToListAsync();

            ViewBag.SubjectAverages = subjectAverages;
            ViewBag.ClassAverages = classAverages;
            ViewBag.TopStudents = topStudents;
            ViewBag.WeakStudents = weakStudents;
            ViewBag.AbsencesByClass = absencesByClass;
            ViewBag.GradesByMonth = gradesByMonth;

            return View("AdminStats");
        }

        private async Task<IActionResult> StudentStats()
        {
            var user = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.Id == user!.StudentId);

            if (student == null) return View("StudentStats");

            // Успех по предмет
            var subjectAverages = await _context.Grades
                .Where(g => g.StudentId == student.Id)
                .GroupBy(g => new { g.Subject!.Name, g.Subject.ShortName })
                .Select(g => new {
                    name = g.Key.Name,
                    shortName = g.Key.ShortName,
                    average = Math.Round(g.Average(x => (double)x.Value), 2),
                    count = g.Count()
                })
                .OrderByDescending(x => x.average)
                .ToListAsync();

            // Динамика по месец
            var gradesByMonth = await _context.Grades
                .Where(g => g.StudentId == student.Id)
                .GroupBy(g => new { g.GradedAt.Year, g.GradedAt.Month })
                .Select(g => new {
                    year = g.Key.Year,
                    month = g.Key.Month,
                    average = Math.Round(g.Average(x => (double)x.Value), 2)
                })
                .OrderBy(g => g.year).ThenBy(g => g.month)
                .ToListAsync();

            // Позиция в клас
            var classStudents = await _context.Students
                .Where(s => s.ClassId == student.ClassId)
                .Select(s => new {
                    id = s.Id,
                    average = s.Grades!.Any()
                        ? Math.Round(s.Grades.Average(g => (double)g.Value), 2)
                        : 0.0
                })
                .Where(s => s.average > 0)
                .OrderByDescending(s => s.average)
                .ToListAsync();

            int position = classStudents.FindIndex(s => s.id == student.Id) + 1;

            // Отсъствия по предмет
            var absencesBySubject = await _context.Attendances
                .Where(a => a.StudentId == student.Id && a.Status == "Отсъства")
                .GroupBy(a => a.Subject!.Name)
                .Select(g => new { name = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToListAsync();

            ViewBag.Student = student;
            ViewBag.SubjectAverages = subjectAverages;
            ViewBag.GradesByMonth = gradesByMonth;
            ViewBag.ClassPosition = position;
            ViewBag.ClassTotal = classStudents.Count;
            ViewBag.AbsencesBySubject = absencesBySubject;

            return View("StudentStats");
        }
    }
}