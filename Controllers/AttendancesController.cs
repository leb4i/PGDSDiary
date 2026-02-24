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
    public class AttendancesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AttendancesController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Attendances
        public async Task<IActionResult> Index(int? classId)
        {
            if (User.IsInRole("Student"))
            {
                var user = await _userManager.GetUserAsync(User);
                var attendances = await _context.Attendances
                    .Include(a => a.Student)
                    .Include(a => a.Subject)
                    .Where(a => a.StudentId == user!.StudentId)
                    .OrderBy(a => a.Subject!.Name)
                    .ToListAsync();
                return View(attendances);
            }

            if (User.IsInRole("Teacher"))
            {
                var user = await _userManager.GetUserAsync(User);
                var teacher = await _context.Teachers
                    .FirstOrDefaultAsync(t => t.UserId == user!.Id);

                if (teacher == null) return View(new List<Attendance>());

                var mySubjectIds = await _context.ClassSubjects
                    .Where(cs => cs.TeacherId == teacher.Id)
                    .Select(cs => cs.SubjectId)
                    .Distinct()
                    .ToListAsync();

                var attendances = await _context.Attendances
                    .Include(a => a.Student).ThenInclude(s => s.Class)
                    .Include(a => a.Subject)
                    .Where(a => mySubjectIds.Contains(a.SubjectId))
                    .OrderByDescending(a => a.Date)
                    .ToListAsync();

                return View(attendances);
            }

            // Admin
            var allClasses = await _context.Classes.ToListAsync();
            ViewBag.AllClasses = allClasses
                .OrderBy(c => int.Parse(string.Concat(c.Name.TakeWhile(char.IsDigit))))
                .ThenBy(c => c.Name)
                .ToList();

            ViewBag.SelectedClassId = classId;

            if (!classId.HasValue)
                return View(new List<Attendance>());

            var adminAttendances = await _context.Attendances
                .Include(a => a.Student).ThenInclude(s => s.Class)
                .Include(a => a.Subject)
                .Where(a => a.Student.ClassId == classId.Value)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            return View(adminAttendances);
        }

        // GET: Attendances/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendance = await _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (attendance == null)
            {
                return NotFound();
            }

            return View(attendance);
        }

        // GET: Attendances/Create
        public IActionResult Create()
        {
            ViewBag.StudentId = new SelectList(
                _context.Students.Select(s => new { s.Id, FullName = s.FirstName + " " + s.LastName }),
                "Id", "FullName");

            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name");

            return View();
        }

        // POST: Attendances/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,StudentId,SubjectId,Date,Status")] Attendance attendance)
        {
            if (ModelState.IsValid)
            {
                _context.Add(attendance);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.StudentId = new SelectList(
                _context.Students.Select(s => new { s.Id, FullName = s.FirstName + " " + s.LastName }),
                "Id", "FullName", attendance.StudentId);

            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name", attendance.SubjectId);

            return View(attendance);
        }

        // GET: Attendances/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null) return NotFound();

            ViewBag.StudentId = new SelectList(
                _context.Students.Select(s => new { s.Id, FullName = s.FirstName + " " + s.LastName }),
                "Id", "FullName", attendance.StudentId);

            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name", attendance.SubjectId);

            return View(attendance);
        }

        // POST: Attendances/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StudentId,SubjectId,Date,Status")] Attendance attendance)
        {
            if (id != attendance.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(attendance);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.StudentId = new SelectList(
                _context.Students.Select(s => new { s.Id, FullName = s.FirstName + " " + s.LastName }),
                "Id", "FullName", attendance.StudentId);

            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name", attendance.SubjectId);

            return View(attendance);
        }

        // GET: Attendances/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendance = await _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (attendance == null)
            {
                return NotFound();
            }

            return View(attendance);
        }

        // POST: Attendances/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance != null)
            {
                _context.Attendances.Remove(attendance);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AttendanceExists(int id)
        {
            return _context.Attendances.Any(e => e.Id == id);
        }
    }
}
