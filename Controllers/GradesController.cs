using GradingSystem.Data;
using GradingSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        // GET: Grades
        public async Task<IActionResult> Index()
        {
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

                // Ако е избран клас — филтрираме
                int? classId = null;
                if (int.TryParse(Request.Query["classId"], out int parsedClassId))
                    classId = parsedClassId;

                var query = _context.Grades
                    .Include(g => g.Student).ThenInclude(s => s.Class)
                    .Include(g => g.Subject)
                    .Where(g => mySubjectIds.Contains(g.SubjectId));

                if (classId.HasValue)
                    query = query.Where(g => g.Student.ClassId == classId.Value);

                var grades = await query
                    .OrderByDescending(g => g.GradedAt)
                    .ToListAsync();

                // Класовете на учителя за dropdown
                var myClassIds = await _context.ClassSubjects
                    .Where(cs => cs.TeacherId == teacher.Id)
                    .Select(cs => cs.ClassId)
                    .Distinct()
                    .ToListAsync();

                ViewBag.MyClasses = await _context.Classes
                    .Where(c => myClassIds.Contains(c.Id))
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                ViewBag.SelectedClassId = classId;

                return View(grades);
            }

            // Admin вижда всичко
            var allGrades = await _context.Grades
                .Include(g => g.Student).ThenInclude(s => s.Class)
                .Include(g => g.Subject)
                .OrderByDescending(g => g.GradedAt)
                .ToListAsync();

            return View(allGrades);
        }

        // GET: Grades/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grade = await _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (grade == null)
            {
                return NotFound();
            }

            return View(grade);
        }

        // GET: Grades/Create
        public IActionResult Create()
        {
            ViewBag.StudentId = new SelectList(
                _context.Students.Select(s => new { s.Id, FullName = s.FirstName + " " + s.LastName }),
                "Id", "FullName");

            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name");

            return View();
        }


        // POST: Grades/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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


        // GET: Grades/Edit/5
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

        // POST: Grades/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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

        // GET: Grades/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grade = await _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (grade == null)
            {
                return NotFound();
            }

            return View(grade);
        }

        // POST: Grades/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var grade = await _context.Grades.FindAsync(id);
            if (grade != null)
            {
                _context.Grades.Remove(grade);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GradeExists(int id)
        {
            return _context.Grades.Any(e => e.Id == id);
        }
    }
}
