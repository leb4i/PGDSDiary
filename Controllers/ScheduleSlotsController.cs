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
    public class ScheduleSlotsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ScheduleSlotsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ScheduleSlots
        public async Task<IActionResult> Index(int? classId, int? teacherId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (User.IsInRole("Student"))
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.Id == user!.StudentId);

                if (student == null) return View(new List<ScheduleSlot>());

                var slots = await _context.ScheduleSlots
                    .Include(s => s.Class)
                    .Include(s => s.Subject)
                    .Where(s => s.ClassId == student.ClassId)
                    .ToListAsync();

                return View(slots);
            }

            if (User.IsInRole("Teacher"))
            {
                var teacher = await _context.Teachers
                    .FirstOrDefaultAsync(t => t.UserId == user!.Id);

                if (teacher == null) return View(new List<ScheduleSlot>());

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

                var slots = await _context.ScheduleSlots
                    .Include(s => s.Class)
                    .Include(s => s.Subject)
                    .Where(s => mySubjectIds.Contains(s.SubjectId) && myClassIds.Contains(s.ClassId))
                    .ToListAsync();

                // Филтър по клас ако е избран
                if (classId.HasValue)
                    slots = slots.Where(s => s.ClassId == classId.Value).ToList();

                ViewBag.Classes = await _context.Classes
                    .Where(c => myClassIds.Contains(c.Id))
                    .OrderBy(c => c.Name).ToListAsync();
                ViewBag.SelectedClass = classId;

                return View(slots);
            }

            // Admin
            var query = _context.ScheduleSlots
                .Include(s => s.Class)
                .Include(s => s.Subject)
                .AsQueryable();

            if (classId.HasValue)
                query = query.Where(s => s.ClassId == classId.Value);

            var allSlots = await query.ToListAsync();

            ViewBag.Classes = await _context.Classes
                .OrderBy(c => c.Name)
                .ToListAsync();
            ViewBag.SelectedClass = classId;

            return View(allSlots);
        }

        // GET: ScheduleSlots/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var scheduleSlot = await _context.ScheduleSlots
                .Include(s => s.Class)
                .Include(s => s.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (scheduleSlot == null)
            {
                return NotFound();
            }

            return View(scheduleSlot);
        }

        // GET: ScheduleSlots/Create
        public IActionResult Create()
        {
            ViewData["ClassId"] = new SelectList(_context.Classes, "Id", "Name");
            ViewData["SubjectId"] = new SelectList(_context.Subjects, "Id", "Name");
            return View();
        }

        // POST: ScheduleSlots/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ClassId,SubjectId,DayOfWeek,PeriodNumber,StartTime,EndTime")] ScheduleSlot scheduleSlot)
        {
            if (ModelState.IsValid)
            {
                _context.Add(scheduleSlot);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClassId"] = new SelectList(_context.Classes, "Id", "Name", scheduleSlot.ClassId);
            ViewData["SubjectId"] = new SelectList(_context.Subjects, "Id", "Name", scheduleSlot.SubjectId);
            return View(scheduleSlot);
        }

        // GET: ScheduleSlots/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var scheduleSlot = await _context.ScheduleSlots.FindAsync(id);
            if (scheduleSlot == null)
            {
                return NotFound();
            }
            ViewData["ClassId"] = new SelectList(_context.Classes, "Id", "Name", scheduleSlot.ClassId);
            ViewData["SubjectId"] = new SelectList(_context.Subjects, "Id", "Name", scheduleSlot.SubjectId);
            return View(scheduleSlot);
        }

        // POST: ScheduleSlots/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClassId,SubjectId,DayOfWeek,PeriodNumber,StartTime,EndTime")] ScheduleSlot scheduleSlot)
        {
            if (id != scheduleSlot.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(scheduleSlot);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ScheduleSlotExists(scheduleSlot.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClassId"] = new SelectList(_context.Classes, "Id", "Name", scheduleSlot.ClassId);
            ViewData["SubjectId"] = new SelectList(_context.Subjects, "Id", "Name", scheduleSlot.SubjectId);
            return View(scheduleSlot);
        }

        // GET: ScheduleSlots/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var scheduleSlot = await _context.ScheduleSlots
                .Include(s => s.Class)
                .Include(s => s.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (scheduleSlot == null)
            {
                return NotFound();
            }

            return View(scheduleSlot);
        }

        // POST: ScheduleSlots/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var scheduleSlot = await _context.ScheduleSlots.FindAsync(id);
            if (scheduleSlot != null)
            {
                _context.ScheduleSlots.Remove(scheduleSlot);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ScheduleSlotExists(int id)
        {
            return _context.ScheduleSlots.Any(e => e.Id == id);
        }
    }
}
