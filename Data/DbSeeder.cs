using Microsoft.AspNetCore.Identity;
using GradingSystem.Models;

namespace GradingSystem.Data
{
    public static class DbSeeder
    {
        public static async Task Seed(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // --- РОЛИ ---
            foreach (var role in new[] { "Admin", "Teacher", "Student" })
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            if (context.Grades.Any()) return;

            // --- КЛАСОВЕ ---
            var classNames = new[]
            {
                "8А","8Б","8В","8Г",
                "9А","9Б","9В","9Г",
                "10А","10Б","10В","10Г",
                "11А","11Б","11В","11Г",
                "12А","12Б","12В","12Г"
            };
            var classes = classNames.Select(n => new Class { Name = n }).ToList();
            context.Classes.AddRange(classes);
            context.SaveChanges();

            // --- ПРЕДМЕТИ ---
            var subjects = new List<Subject>
            {
                new Subject { Name = "Български език" },
                new Subject { Name = "Математика" },
                new Subject { Name = "Физическо възпитание" },
                new Subject { Name = "Английски език" },
                new Subject { Name = "Изобразително изкуство" },
                new Subject { Name = "География и Икономика" },
                new Subject { Name = "История и Цивилизация" },
                new Subject { Name = "Немски език" },
                new Subject { Name = "Руски език" },
            };
            context.Subjects.AddRange(subjects);
            context.SaveChanges();

            // --- УЧИТЕЛИ ---
            var teacherInfo = new[]
            {
                ("Анна",     "Тодорова",  "teacher_bg"),
                ("Петър",    "Стоянов",   "teacher_math"),
                ("Георги",   "Маринов",   "teacher_pe"),
                ("Елена",    "Христова",  "teacher_en"),
                ("Мария",    "Колева",    "teacher_art"),
                ("Иван",     "Попов",     "teacher_geo"),
                ("Надежда",  "Атанасова", "teacher_hist"),
                ("Димитър",  "Йорданов",  "teacher_de"),
                ("Светлана", "Николова",  "teacher_ru"),
            };

            var teachers = teacherInfo.Select(t => new Teacher
            {
                FirstName = t.Item1,
                LastName = t.Item2
            }).ToList();
            context.Teachers.AddRange(teachers);
            context.SaveChanges();

            // --- ПОТРЕБИТЕЛИ ЗА УЧИТЕЛИ ---
            for (int i = 0; i < teacherInfo.Length; i++)
            {
                var u = new ApplicationUser
                {
                    UserName = teacherInfo[i].Item3,
                    Email = $"{teacherInfo[i].Item3}@pgds.bg",
                    FirstName = teacherInfo[i].Item1,
                    LastName = teacherInfo[i].Item2
                };
                await userManager.CreateAsync(u, "1234");
                await userManager.AddToRoleAsync(u, "Teacher");
                teachers[i].UserId = u.Id;
            }
            context.Teachers.UpdateRange(teachers);
            context.SaveChanges();

            // --- ADMIN ---
            var admin = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@pgds.bg",
                FirstName = "Администратор",
                LastName = "Системен"
            };
            await userManager.CreateAsync(admin, "1234");
            await userManager.AddToRoleAsync(admin, "Admin");

            // --- Предмет → Учител ---
            var subjectTeacher = new Dictionary<int, Teacher>
            {
                { subjects[0].Id, teachers[0] },
                { subjects[1].Id, teachers[1] },
                { subjects[2].Id, teachers[2] },
                { subjects[3].Id, teachers[3] },
                { subjects[4].Id, teachers[4] },
                { subjects[5].Id, teachers[5] },
                { subjects[6].Id, teachers[6] },
                { subjects[7].Id, teachers[7] },
                { subjects[8].Id, teachers[8] },
            };

            // --- CLASS SUBJECTS ---
            var classSubjects = new List<ClassSubject>();
            for (int ci = 0; ci < classes.Count; ci++)
            {
                int gradeLevel = ci / 4 + 8;
                var subjectsForClass = new List<Subject>(subjects.Take(7));
                if (gradeLevel == 9 || gradeLevel == 10) subjectsForClass.Add(subjects[7]);
                if (gradeLevel == 11 || gradeLevel == 12) subjectsForClass.Add(subjects[8]);

                foreach (var subj in subjectsForClass)
                {
                    classSubjects.Add(new ClassSubject
                    {
                        ClassId = classes[ci].Id,
                        SubjectId = subj.Id,
                        TeacherId = subjectTeacher[subj.Id].Id
                    });
                }
            }
            context.ClassSubjects.AddRange(classSubjects);
            context.SaveChanges();

            // --- РАЗПИСАНИЕ БЕЗ КОНФЛИКТИ ---
            var days = new[] { "Понеделник", "Вторник", "Сряда", "Четвъртък", "Петък" };
            var times = new (TimeOnly Start, TimeOnly End)[]
            {
                (new TimeOnly(8,   0), new TimeOnly(8,  45)),
                (new TimeOnly(8,  55), new TimeOnly(9,  40)),
                (new TimeOnly(10,  0), new TimeOnly(10, 45)),
                (new TimeOnly(10, 55), new TimeOnly(11, 40)),
                (new TimeOnly(11, 50), new TimeOnly(12, 35)),
                (new TimeOnly(12, 45), new TimeOnly(13, 30)),
                (new TimeOnly(13, 40), new TimeOnly(14, 25))
            };

            var teacherUsed = teachers.ToDictionary(t => t.Id, _ => new HashSet<(int, int)>());
            var classUsed = classes.ToDictionary(c => c.Id, _ => new HashSet<(int, int)>());
            var scheduleSlots = new List<ScheduleSlot>();

            foreach (var cs in classSubjects)
            {
                var teacher = subjectTeacher[cs.SubjectId];
                bool placed = false;

                for (int d = 0; d < 5 && !placed; d++)
                {
                    for (int p = 0; p < 7 && !placed; p++)
                    {
                        if (!teacherUsed[teacher.Id].Contains((d, p)) &&
                            !classUsed[cs.ClassId].Contains((d, p)))
                        {
                            scheduleSlots.Add(new ScheduleSlot
                            {
                                ClassId = cs.ClassId,
                                SubjectId = cs.SubjectId,
                                DayOfWeek = days[d],
                                PeriodNumber = p + 1,
                                StartTime = times[p].Start,
                                EndTime = times[p].End
                            });
                            teacherUsed[teacher.Id].Add((d, p));
                            classUsed[cs.ClassId].Add((d, p));
                            placed = true;
                        }
                    }
                }
            }
            context.ScheduleSlots.AddRange(scheduleSlots);
            context.SaveChanges();

            // --- УЧЕНИЦИ ---
            var maleNames = new[] { "Иван", "Георги", "Петър", "Димитър", "Стоян", "Николай", "Александър", "Мартин", "Кристиан", "Даниел" };
            var femaleNames = new[] { "Мария", "Елена", "Ивана", "Петя", "Надежда", "Кристина", "Виктория", "Александра", "Симона", "Даниела" };
            var maleLastNames = new[] { "Иванов", "Петров", "Георгиев", "Димитров", "Стоянов", "Николаев", "Тодоров", "Христов", "Попов", "Йорданов", "Колев", "Атанасов", "Маринов", "Илиев", "Станев", "Василев", "Добрев", "Ангелов", "Тончев", "Кирилов" };
            var femaleLastNames = new[] { "Иванова", "Петрова", "Георгиева", "Димитрова", "Стоянова", "Николаева", "Тодорова", "Христова", "Попова", "Йорданова", "Колева", "Атанасова", "Маринова", "Илиева", "Станева", "Василева", "Добрева", "Ангелова", "Тончева", "Кирилова" };

            var rng = new Random(42);
            var allStudents = new List<Student>();

            foreach (var cls in classes)
            {
                for (int i = 0; i < 20; i++)
                {
                    bool male = rng.Next(2) == 0;
                    var firstName = male ? maleNames[rng.Next(maleNames.Length)] : femaleNames[rng.Next(femaleNames.Length)];
                    var lastName = male ? maleLastNames[rng.Next(maleLastNames.Length)] : femaleLastNames[rng.Next(femaleLastNames.Length)];
                    allStudents.Add(new Student { FirstName = firstName, LastName = lastName, ClassId = cls.Id });
                }
            }
            context.Students.AddRange(allStudents);
            context.SaveChanges();

            // --- DEMO УЧЕНИЦИ ---
            var demoStudentData = new[]
            {
                (allStudents[0],   "student1"),  // 8А
                (allStudents[20],  "student2"),  // 8Б
                (allStudents[40],  "student3"),  // 9А
                (allStudents[80],  "student4"),  // 10А
                (allStudents[120], "student5"),  // 11А
                (allStudents[160], "student6"),  // 12А
            };

            foreach (var (s, username) in demoStudentData)
            {
                var u = new ApplicationUser
                {
                    UserName = username,
                    Email = $"{username}@pgds.bg",
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    StudentId = s.Id
                };
                await userManager.CreateAsync(u, "1234");
                await userManager.AddToRoleAsync(u, "Student");
            }

            // --- ОЦЕНКИ ---
            var gradeTypes = new[] { "Тест", "Устен", "Контролно" };
            var gradeValues = new[] { 2.00m, 3.00m, 3.50m, 4.00m, 4.50m, 5.00m, 5.50m, 6.00m };
            var allGrades = new List<Grade>();

            foreach (var student in allStudents.Take(160))
            {
                var csForClass = classSubjects.Where(cs => cs.ClassId == student.ClassId).ToList();
                foreach (var cs in csForClass)
                {
                    int count = rng.Next(2, 5);
                    for (int g = 0; g < count; g++)
                    {
                        allGrades.Add(new Grade
                        {
                            StudentId = student.Id,
                            SubjectId = cs.SubjectId,
                            Value = gradeValues[rng.Next(gradeValues.Length)],
                            Type = gradeTypes[rng.Next(gradeTypes.Length)],
                            GradedAt = DateTime.Now.AddDays(-rng.Next(1, 90))
                        });
                    }
                }
            }
            context.Grades.AddRange(allGrades);
            context.SaveChanges();

            // --- ОТСЪСТВИЯ ---
            var statuses = new[] { "Отсъства", "Закъснял", "Извинено" };
            var allAttendances = new List<Attendance>();

            foreach (var student in allStudents.Take(160))
            {
                var csForClass = classSubjects.Where(cs => cs.ClassId == student.ClassId).ToList();
                int count = rng.Next(0, 6);
                for (int a = 0; a < count; a++)
                {
                    var cs = csForClass[rng.Next(csForClass.Count)];
                    var date = DateOnly.FromDateTime(DateTime.Now.AddDays(-rng.Next(1, 60)));
                    if (!allAttendances.Any(x => x.StudentId == student.Id && x.SubjectId == cs.SubjectId && x.Date == date))
                    {
                        allAttendances.Add(new Attendance
                        {
                            StudentId = student.Id,
                            SubjectId = cs.SubjectId,
                            Date = date,
                            Status = statuses[rng.Next(statuses.Length)]
                        });
                    }
                }
            }
            context.Attendances.AddRange(allAttendances);
            context.SaveChanges();
        }
    }
}