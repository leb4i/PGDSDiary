using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GradingSystem.Data;
using GradingSystem.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GradingSystem.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? classId, DateTime? from, DateTime? to)
        {
            var fromDate = from ?? DateTime.Now.AddMonths(-6);
            var toDate = to ?? DateTime.Now;

            List<Class> classes;
            if (User.IsInRole("Teacher"))
            {
                var user = await _userManager.GetUserAsync(User);
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user!.Id);
                var myClassIds = await _context.ClassSubjects
                    .Where(cs => cs.TeacherId == teacher!.Id)
                    .Select(cs => cs.ClassId).Distinct().ToListAsync();
                classes = await _context.Classes
                    .Where(c => myClassIds.Contains(c.Id))
                    .ToListAsync();
            }
            else
            {
                classes = await _context.Classes.ToListAsync();
            }

            classes = classes
                .OrderBy(c => int.Parse(string.Concat(c.Name.TakeWhile(char.IsDigit))))
                .ThenBy(c => c.Name)
                .ToList();

            ViewBag.Classes = classes;
            ViewBag.Selected = classId;
            ViewBag.From = fromDate.ToString("yyyy-MM-dd");
            ViewBag.To = toDate.ToString("yyyy-MM-dd");

            if (!classId.HasValue)
                return View();

            // Данни за preview
            var selectedClass = classes.FirstOrDefault(c => c.Id == classId);

            var students = await _context.Students
                .Include(s => s.Class)
                .Where(s => s.ClassId == classId)
                .ToListAsync();

            var grades = await _context.Grades
                .Include(g => g.Subject)
                .Include(g => g.Student)
                .Where(g => g.Student!.ClassId == classId &&
                            g.GradedAt >= fromDate && g.GradedAt <= toDate)
                .ToListAsync();

            var absences = await _context.Attendances
                .Where(a => a.Student!.ClassId == classId &&
                            a.Status == "Отсъства" &&
                            a.Date >= DateOnly.FromDateTime(fromDate) &&
                            a.Date <= DateOnly.FromDateTime(toDate))
                .ToListAsync();

            // Среден успех по клас
            var classAvg = grades.Any() ? Math.Round(grades.Average(g => (double)g.Value), 2) : 0;

            // Топ 5 ученици
            var topStudents = students
                .Select(s => new {
                    Name = s.FirstName + " " + s.LastName,
                    Average = grades.Where(g => g.StudentId == s.Id).Any()
                        ? Math.Round(grades.Where(g => g.StudentId == s.Id).Average(g => (double)g.Value), 2)
                        : 0.0
                })
                .Where(s => s.Average > 0)
                .OrderByDescending(s => s.Average)
                .Take(5)
                .ToList();

            // Оценки по предмет
            var subjectStats = grades
                .GroupBy(g => g.Subject!.Name)
                .Select(g => new {
                    Subject = g.Key,
                    Average = Math.Round(g.Average(x => (double)x.Value), 2),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Average)
                .ToList();

            // Отсъствия по ученик
            var absenceStats = students
                .Select(s => new {
                    Name = s.FirstName + " " + s.LastName,
                    Count = absences.Count(a => a.StudentId == s.Id)
                })
                .Where(s => s.Count > 0)
                .OrderByDescending(s => s.Count)
                .ToList();

            ViewBag.SelectedClass = selectedClass;
            ViewBag.ClassAvg = classAvg;
            ViewBag.TopStudents = topStudents;
            ViewBag.SubjectStats = subjectStats;
            ViewBag.AbsenceStats = absenceStats;
            ViewBag.TotalStudents = students.Count;
            ViewBag.TotalAbsences = absences.Count;

            return View();
        }

        public async Task<IActionResult> GeneratePdf(int classId, DateTime from, DateTime to)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var selectedClass = await _context.Classes.FindAsync(classId);
            var students = await _context.Students
                .Where(s => s.ClassId == classId).ToListAsync();

            var grades = await _context.Grades
                .Include(g => g.Subject)
                .Include(g => g.Student)
                .Where(g => g.Student!.ClassId == classId &&
                            g.GradedAt >= from && g.GradedAt <= to)
                .ToListAsync();

            var absences = await _context.Attendances
                .Where(a => a.Student!.ClassId == classId &&
                            a.Status == "Отсъства" &&
                            a.Date >= DateOnly.FromDateTime(from) &&
                            a.Date <= DateOnly.FromDateTime(to))
                .ToListAsync();

            var classAvg = grades.Any() ? Math.Round(grades.Average(g => (double)g.Value), 2) : 0;

            var topStudents = students
                .Select(s => new {
                    Name = s.FirstName + " " + s.LastName,
                    Average = grades.Where(g => g.StudentId == s.Id).Any()
                        ? Math.Round(grades.Where(g => g.StudentId == s.Id).Average(g => (double)g.Value), 2)
                        : 0.0
                })
                .Where(s => s.Average > 0)
                .OrderByDescending(s => s.Average)
                .Take(5)
                .ToList();

            var subjectStats = grades
                .GroupBy(g => g.Subject!.Name)
                .Select(g => new {
                    Subject = g.Key,
                    Average = Math.Round(g.Average(x => (double)x.Value), 2),
                    Count = g.Count(),
                    Grades = g.Select(x => x.Value).OrderBy(x => x).ToList()
                })
                .OrderByDescending(x => x.Average)
                .ToList();

            var absenceStats = students
                .Select(s => new {
                    Name = s.FirstName + " " + s.LastName,
                    Count = absences.Count(a => a.StudentId == s.Id)
                })
                .Where(s => s.Count > 0)
                .OrderByDescending(s => s.Count)
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"ПГДС — Справка за клас {selectedClass?.Name}")
                                .FontSize(16).Bold().FontColor(Color.FromHex("#1e2530"));
                            row.ConstantItem(150).AlignRight()
                                .Text($"{from:dd.MM.yyyy} — {to:dd.MM.yyyy}")
                                .FontSize(9).FontColor(Color.FromHex("#64748b"));
                        });
                        col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Color.FromHex("#2e6be6"));
                        col.Item().PaddingBottom(8);
                    });

                    page.Content().Column(col =>
                    {
                        // Обобщение
                        col.Item().Background(Color.FromHex("#f1f5f9")).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Общ преглед").Bold().FontSize(11);
                                c.Item().Text($"Ученици: {students.Count}");
                                c.Item().Text($"Среден успех: {classAvg}").Bold();
                                c.Item().Text($"Общо отсъствия: {absences.Count}");
                            });
                        });

                        col.Item().PaddingTop(14).Text("Успех по предмет")
                            .FontSize(12).Bold().FontColor(Color.FromHex("#1e2530"));
                        col.Item().PaddingTop(4);

                        // Таблица предмети
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(3);
                            });

                            // Header
                            table.Header(h =>
                            {
                                h.Cell().Background(Color.FromHex("#2e6be6")).Padding(5)
                                    .Text("Предмет").Bold().FontColor(Colors.White);
                                h.Cell().Background(Color.FromHex("#2e6be6")).Padding(5)
                                    .Text("Среден").Bold().FontColor(Colors.White).AlignCenter();
                                h.Cell().Background(Color.FromHex("#2e6be6")).Padding(5)
                                    .Text("Оценки").Bold().FontColor(Colors.White).AlignCenter();
                                h.Cell().Background(Color.FromHex("#2e6be6")).Padding(5)
                                    .Text("Всички оценки").Bold().FontColor(Colors.White);
                            });

                            bool alt = false;
                            foreach (var s in subjectStats)
                            {
                                var bg = alt ? Color.FromHex("#f8fafc") : Colors.White;
                                table.Cell().Background(bg).Padding(5).Text(s.Subject);
                                table.Cell().Background(bg).Padding(5).AlignCenter()
                                    .Text(s.Average.ToString("F2")).Bold();
                                table.Cell().Background(bg).Padding(5).AlignCenter()
                                    .Text(s.Count.ToString());
                                table.Cell().Background(bg).Padding(5)
                                    .Text(string.Join(", ", s.Grades));
                                alt = !alt;
                            }
                        });

                        col.Item().PaddingTop(14).Text("Топ 5 ученици")
                            .FontSize(12).Bold().FontColor(Color.FromHex("#1e2530"));
                        col.Item().PaddingTop(4);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(25);
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(1);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Background(Color.FromHex("#2e7d50")).Padding(5)
                                    .Text("#").Bold().FontColor(Colors.White);
                                h.Cell().Background(Color.FromHex("#2e7d50")).Padding(5)
                                    .Text("Ученик").Bold().FontColor(Colors.White);
                                h.Cell().Background(Color.FromHex("#2e7d50")).Padding(5)
                                    .Text("Среден успех").Bold().FontColor(Colors.White);
                            });

                            int rank = 1;
                            foreach (var s in topStudents)
                            {
                                var bg = rank % 2 == 0 ? Color.FromHex("#f8fafc") : Colors.White;
                                table.Cell().Background(bg).Padding(5).Text(rank.ToString()).Bold();
                                table.Cell().Background(bg).Padding(5).Text(s.Name);
                                table.Cell().Background(bg).Padding(5).Text(s.Average.ToString("F2")).Bold();
                                rank++;
                            }
                        });

                        if (absenceStats.Any())
                        {
                            col.Item().PaddingTop(14).Text("Отсъствия по ученик")
                                .FontSize(12).Bold().FontColor(Color.FromHex("#1e2530"));
                            col.Item().PaddingTop(4);

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(3);
                                    cols.RelativeColumn(1);
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Background(Color.FromHex("#e53e3e")).Padding(5)
                                        .Text("Ученик").Bold().FontColor(Colors.White);
                                    h.Cell().Background(Color.FromHex("#e53e3e")).Padding(5)
                                        .Text("Отсъствия").Bold().FontColor(Colors.White);
                                });

                                bool alt = false;
                                foreach (var a in absenceStats)
                                {
                                    var bg = alt ? Color.FromHex("#fff5f5") : Colors.White;
                                    table.Cell().Background(bg).Padding(5).Text(a.Name);
                                    table.Cell().Background(bg).Padding(5).Text(a.Count.ToString()).Bold();
                                    alt = !alt;
                                }
                            });
                        }

                        col.Item().PaddingTop(20).LineHorizontal(1).LineColor(Color.FromHex("#e2e8f0"));
                        col.Item().PaddingTop(6).AlignCenter()
                            .Text($"Генерирано на {DateTime.Now:dd.MM.yyyy HH:mm} — PGDSDiary")
                            .FontSize(8).FontColor(Color.FromHex("#94a3b8"));
                    });
                });
            });

            var pdf = document.GeneratePdf();
            return File(pdf, "application/pdf", $"Справка_{selectedClass?.Name}_{from:ddMMyyyy}-{to:ddMMyyyy}.pdf");
        }
    }
}