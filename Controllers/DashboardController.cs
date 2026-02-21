using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GradingSystem.Data;
using GradingSystem.Models;

namespace GradingSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (User.IsInRole("Admin"))
                return await AdminDashboard();
            else if (User.IsInRole("Teacher"))
                return await TeacherDashboard(user);
            else
                return await StudentDashboard(user);
        }

        // ── ADMIN ──
        private async Task<IActionResult> AdminDashboard()
        {
            ViewData["Title"] = "Начало — Администратор";
            ViewData["Layout"] = "Admin";

            ViewBag.TotalStudents = await _context.Students.CountAsync();
            ViewBag.TotalGrades = await _context.Grades.CountAsync();
            ViewBag.TotalAbsences = await _context.Attendances.CountAsync(a => a.Status == "Отсъства");
            ViewBag.TotalLate = await _context.Attendances.CountAsync(a => a.Status == "Закъснял");

            var avg = await _context.Grades.AverageAsync(g => (double?)g.Value);
            ViewBag.AverageGrade = avg.HasValue ? Math.Round(avg.Value, 2) : 0;

            // Топ 10 ученика по успех
            var top10 = await _context.Grades
                .Include(g => g.Student).ThenInclude(s => s.Class)
                .GroupBy(g => new { g.StudentId, g.Student.FirstName, g.Student.LastName, ClassName = g.Student.Class!.Name })
                .Select(g => new
                {
                    g.Key.StudentId,
                    g.Key.FirstName,
                    g.Key.LastName,
                    g.Key.ClassName,
                    Average = Math.Round(g.Average(x => (double)x.Value), 2)
                })
                .OrderByDescending(x => x.Average)
                .Take(10)
                .ToListAsync();
            ViewBag.Top10 = top10;

            // Топ предмети
            var topSubjects = await _context.Grades
                .Include(g => g.Subject)
                .GroupBy(g => g.Subject!.Name)
                .Select(g => new
                {
                    Subject = g.Key,
                    Average = Math.Round(g.Average(x => (double)x.Value), 2)
                })
                .OrderByDescending(x => x.Average)
                .Take(5)
                .ToListAsync();
            ViewBag.TopSubjects = topSubjects;

            // Последни оценки
            ViewBag.RecentGrades = await _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Subject)
                .OrderByDescending(g => g.GradedAt)
                .Take(5)
                .ToListAsync();

            return View("AdminDashboard");
        }

        // ── TEACHER ──
        private async Task<IActionResult> TeacherDashboard(ApplicationUser user)
        {
            ViewData["Title"] = "Начало — Учител";

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (teacher == null)
            {
                ViewBag.Error = "Не сте свързани с учителски профил.";
                return View("TeacherDashboard");
            }

            // Класовете на учителя
            var myClassSubjects = await _context.ClassSubjects
                .Include(cs => cs.Class)
                .Include(cs => cs.Subject)
                .Where(cs => cs.TeacherId == teacher.Id)
                .ToListAsync();

            ViewBag.Teacher = teacher;
            ViewBag.MyClassSubjects = myClassSubjects;

            // Брой ученици в неговите класове
            var myClassIds = myClassSubjects.Select(cs => cs.ClassId).Distinct().ToList();
            ViewBag.TotalStudents = await _context.Students
                .CountAsync(s => myClassIds.Contains(s.ClassId));

            // Брой оценки поставени от него
            var mySubjectIds = myClassSubjects.Select(cs => cs.SubjectId).Distinct().ToList();
            ViewBag.TotalGrades = await _context.Grades
                .CountAsync(g => mySubjectIds.Contains(g.SubjectId));

            // Последни оценки
            ViewBag.RecentGrades = await _context.Grades
                .Include(g => g.Student).ThenInclude(s => s.Class)
                .Include(g => g.Subject)
                .Where(g => mySubjectIds.Contains(g.SubjectId))
                .OrderByDescending(g => g.GradedAt)
                .Take(8)
                .ToListAsync();

            // Разписание на учителя
            ViewBag.Schedule = await _context.ScheduleSlots
                .Include(ss => ss.Class)
                .Include(ss => ss.Subject)
                .Where(ss => mySubjectIds.Contains(ss.SubjectId) && myClassIds.Contains(ss.ClassId))
                .OrderBy(ss => ss.DayOfWeek)
                .ThenBy(ss => ss.PeriodNumber)
                .ToListAsync();

            return View("TeacherDashboard");
        }

        // ── STUDENT ──
        private async Task<IActionResult> StudentDashboard(ApplicationUser user)
        {
            ViewData["Title"] = "Начало — Ученик";

            var student = await _context.Students
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.Id == user.StudentId);

            if (student == null)
            {
                ViewBag.Error = "Не сте свързани с ученически профил.";
                return View("StudentDashboard");
            }

            ViewBag.Student = student;

            // Оценките на ученика
            var myGrades = await _context.Grades
                .Include(g => g.Subject)
                .Where(g => g.StudentId == student.Id)
                .OrderBy(g => g.GradedAt)
                .ToListAsync();

            ViewBag.MyGrades = myGrades;

            // Среден успех
            ViewBag.MyAverage = myGrades.Any()
                ? Math.Round(myGrades.Average(g => (double)g.Value), 2)
                : 0;

            // Позиция в клас
            var classAverages = await _context.Grades
                .Where(g => _context.Students
                    .Where(s => s.ClassId == student.ClassId)
                    .Select(s => s.Id)
                    .Contains(g.StudentId))
                .GroupBy(g => g.StudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    Average = g.Average(x => (double)x.Value)
                })
                .OrderByDescending(x => x.Average)
                .ToListAsync();

            var position = classAverages.FindIndex(x => x.StudentId == student.Id) + 1;
            ViewBag.ClassPosition = position > 0 ? position : classAverages.Count + 1;
            ViewBag.ClassTotal = await _context.Students.CountAsync(s => s.ClassId == student.ClassId);

            // Топ 10 в училище
            var top10 = await _context.Grades
                .Include(g => g.Student).ThenInclude(s => s.Class)
                .GroupBy(g => new { g.StudentId, g.Student.FirstName, g.Student.LastName, ClassName = g.Student.Class!.Name })
                .Select(g => new
                {
                    g.Key.StudentId,
                    g.Key.FirstName,
                    g.Key.LastName,
                    g.Key.ClassName,
                    Average = Math.Round(g.Average(x => (double)x.Value), 2)
                })
                .OrderByDescending(x => x.Average)
                .Take(10)
                .ToListAsync();
            ViewBag.Top10 = top10;

            // Оценки по дата за Line Graph
            var gradesByDate = myGrades
                .GroupBy(g => g.GradedAt.ToString("dd.MM"))
                .Select(g => new
                {
                    Date = g.Key,
                    Average = Math.Round(g.Average(x => (double)x.Value), 2)
                })
                .ToList();
            ViewBag.GradeLabels = gradesByDate.Select(x => x.Date).ToList();
            ViewBag.GradeValues = gradesByDate.Select(x => x.Average).ToList();

            // Отсъствия
            ViewBag.TotalAbsences = await _context.Attendances
                .CountAsync(a => a.StudentId == student.Id && a.Status == "Отсъства");

            return View("StudentDashboard");
        }
    }
}