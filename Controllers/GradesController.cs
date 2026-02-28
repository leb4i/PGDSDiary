using GradingSystem.Data;
using GradingSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GradingSystem.Controllers
{
    public class GradesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GradesController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? classId, int page = 1)
        {
            const int pageSize = 50;

            if (User.IsInRole("Student"))
            {
                var user = await _userManager.GetUserAsync(User);
                var grades = await _context.Grades
                    .Include(g => g.Student)
                    .Include(g => g.Subject)
                    .Where(g => g.StudentId == user!.StudentId)
                    .OrderByDescending(g => g.GradedAt)
                    .ToListAsync();
                return View(grades);
            }

            if (User.IsInRole("Teacher"))
            {
                var user = await _userManager.GetUserAsync(User);
                var teacher = await _context.Teachers
                    .FirstOrDefaultAsync(t => t.UserId == user!.Id);

                if (teacher == null) return View(new List<Grade>());

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

                var query = _context.Grades
                    .Include(g => g.Student).ThenInclude(s => s.Class)
                    .Include(g => g.Subject)
                    .Where(g => mySubjectIds.Contains(g.SubjectId));

                if (classId.HasValue)
                    query = query.Where(g => g.Student.ClassId == classId.Value);

                var grades = await query
                    .OrderByDescending(g => g.GradedAt)
                    .ToListAsync();

                var myClasses = await _context.Classes
                    .Where(c => myClassIds.Contains(c.Id))
                    .ToListAsync();

                ViewBag.MyClasses = myClasses
                    .OrderBy(c => int.Parse(string.Concat(c.Name.TakeWhile(char.IsDigit))))
                    .ThenBy(c => c.Name)
                    .ToList();

                ViewBag.SelectedClassId = classId;
                return View(grades);
            }

            // Admin
            ViewBag.AllClasses = (await _context.Classes.ToListAsync())
                .OrderBy(c => int.Parse(string.Concat(c.Name.TakeWhile(char.IsDigit))))
                .ThenBy(c => c.Name)
                .ToList();

            ViewBag.SelectedClassId = classId;
            ViewBag.TotalCount = 0;

            if (!classId.HasValue)
                return View(new List<Grade>());

            var totalCount = await _context.Grades
                .Where(g => g.Student.ClassId == classId.Value)
                .CountAsync();

            var adminGrades = await _context.Grades
                .Include(g => g.Student).ThenInclude(s => s.Class)
                .Include(g => g.Subject)
                .Where(g => g.Student.ClassId == classId.Value)
                .OrderByDescending(g => g.GradedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalCount = totalCount;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return View(adminGrades);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var grade = await _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (grade == null) return NotFound();

            return View(grade);
        }

        public IActionResult Create()
        {
            ViewBag.StudentId = new SelectList(
                _context.Students.Select(s => new { s.Id, FullName = s.FirstName + " " + s.LastName }),
                "Id", "FullName");

            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,StudentId,SubjectId,Value,Type,GradedAt,Comment")] Grade grade)
        {
            if (ModelState.IsValid)
            {
                _context.Add(grade);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.StudentId = new SelectList(
                _context.Students.Select(s => new { s.Id, FullName = s.FirstName + " " + s.LastName }),
                "Id", "FullName", grade.StudentId);

            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name", grade.SubjectId);

            return View(grade);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var grade = await _context.Grades.FindAsync(id);
            if (grade == null) return NotFound();

            ViewBag.StudentId = new SelectList(
                _context.Students.Select(s => new { s.Id, FullName = s.FirstName + " " + s.LastName }),
                "Id", "FullName", grade.StudentId);

            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name", grade.SubjectId);

            return View(grade);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StudentId,SubjectId,Value,Type,GradedAt,Comment")] Grade grade)
        {
            if (id != grade.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(grade);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.StudentId = new SelectList(
                _context.Students.Select(s => new { s.Id, FullName = s.FirstName + " " + s.LastName }),
                "Id", "FullName", grade.StudentId);

            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name", grade.SubjectId);

            return View(grade);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var grade = await _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (grade == null) return NotFound();

            return View(grade);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var grade = await _context.Grades.FindAsync(id);
            if (grade != null)
                _context.Grades.Remove(grade);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GradeExists(int id)
        {
            return _context.Grades.Any(e => e.Id == id);
        }
    }
}