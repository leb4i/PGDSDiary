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
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Student"))
            {
                var user = await _userManager.GetUserAsync(User);
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.Id == user!.StudentId);

                if (student == null)
                    return View(new List<ScheduleSlot>());

                var slots = await _context.ScheduleSlots
                    .Include(s => s.Class)
                    .Include(s => s.Subject)
                    .Where(s => s.ClassId == student.ClassId)
                    .ToListAsync();

                return View(slots);
            }

            // Admin и Teacher виждат всичко
            var allSlots = await _context.ScheduleSlots
                .Include(s => s.Class)
                .Include(s => s.Subject)
                .ToListAsync();

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
