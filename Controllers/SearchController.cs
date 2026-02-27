using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GradingSystem.Data;

namespace GradingSystem.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly AppDbContext _context;

        public SearchController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return View(null);

            q = q.Trim().ToLower();

            var students = await _context.Students
                .Include(s => s.Class)
                .Where(s => (s.FirstName + " " + s.LastName).ToLower().Contains(q) ||
                             s.Class!.Name.ToLower().Contains(q))
                .Take(10)
                .ToListAsync();

            var teachers = await _context.Teachers
                .Where(t => (t.FirstName + " " + t.LastName).ToLower().Contains(q))
                .Take(10)
                .ToListAsync();

            var subjects = await _context.Subjects
                .Where(s => s.Name.ToLower().Contains(q) ||
                             s.ShortName!.ToLower().Contains(q))
                .Take(10)
                .ToListAsync();

            ViewBag.Query = q;
            ViewBag.Students = students;
            ViewBag.Teachers = teachers;
            ViewBag.Subjects = subjects;

            return View();
        }

        public async Task<IActionResult> Live(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Json(new { total = 0 });

            q = q.Trim().ToLower();

            var students = await _context.Students
                .Include(s => s.Class)
                .Where(s => (s.FirstName + " " + s.LastName).ToLower().Contains(q) ||
                             s.Class!.Name.ToLower().Contains(q))
                .Take(5)
                .Select(s => new { id = s.Id, name = s.FirstName + " " + s.LastName, className = s.Class!.Name })
                .ToListAsync();

            var teachers = await _context.Teachers
                .Where(t => (t.FirstName + " " + t.LastName).ToLower().Contains(q))
                .Take(5)
                .Select(t => new { id = t.Id, name = t.FirstName + " " + t.LastName })
                .ToListAsync();

            var subjects = await _context.Subjects
                .Where(s => s.Name.ToLower().Contains(q) || s.ShortName!.ToLower().Contains(q))
                .Take(5)
                .Select(s => new { id = s.Id, name = s.Name, shortName = s.ShortName })
                .ToListAsync();

            return Json(new
            {
                total = students.Count + teachers.Count + subjects.Count,
                students,
                teachers,
                subjects
            });
        }
    }
}