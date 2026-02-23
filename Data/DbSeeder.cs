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
                "8А","8Б","8В","8Г","8Д",
                "9А","9Б","9В","9Г","9Д",
                "10А","10Б","10В","10Г","10Д",
                "11А","11Б","11В","11Г","11Д",
                "12А","12Б","12В","12Г","12Д"
            };
            var classes = classNames.Select(n => new Class { Name = n }).ToList();
            context.Classes.AddRange(classes);
            context.SaveChanges();

            // Индекси за лесен достъп
            var cls = classes.ToDictionary(c => c.Name);

            // --- ПРЕДМЕТИ ---
            var allSubjectNames = new HashSet<string>
            {
                // Общи
                "Български език", "Математика", "Физическо възпитание",
                "Английски език", "Изобразително изкуство",
                "География и Икономика", "История и Цивилизация",
                "Немски език", "Руски език",

                // 8-ми клас
                "Информационни технологии", "Устройство на компютърните системи",
                "Предприемачество", "Топиари (парково)",

                // 9А
                "Обектно-ориентирано програмиране",
                "Учебна практика по Обектно-ориентирано програмиране",
                "Учебна практика по програмиране",
                // 9Б
                "Учебна практика по строителни материали",
                "Учебна практика по Строителна графика",
                "Учебна практика по сградостроителство",
                // 9В
                "Учебна практика по мебелно производство",
                "Учебна практика по конструктивно знание и чертане",
                // 9Г
                "Учебна практика по рисунка типаж и анимации",
                "Материалознание", "Рисуване и композиция",
                // 9Д
                "Учебна практика по ловно и рибовъдно стопанство",
                "Учебна практика по топографско чертане",
                "Учебна практика по геодезия",

                // 10А
                "Увод в алгоритмите и структурите от данни",
                "Учебна практика по увод в алгоритмите и структурите от данни",
                "Управление на хардуер с Ардуино",
                // 10Б
                // (вече имаме сградостроителство и строителни материали)
                // 10В
                "Технологии и машини в мебелното производство",
                "Учебна практика по дървообработване",
                // 10Г
                "Графични техники",
                "Учебна практика по конструиране на мебели",
                // 10Д
                "Икономика и управление на търговията",
                "Счетоводство на предприятието",
                "Учебна практика по организация на брокерската дейност",

                // 11А
                "Компютърни мрежи", "Разработка на софтуер",
                "Учебна практика по Компютърни мрежи",
                "Учебна практика по Разработка на софтуер",
                // 11Б
                "Учебна практика по градоустройство и архитектурно проектиране",
                "Технология на строителните процеси",
                "Учебна практика по изпълнение на строително-монтажни работи",
                // 11В
                "Конструиране на мебели", "Проектиране на мебели и интериор",
                // 11Г
                "Парково строителство", "Лесовъдство", "Горско законознание",
                "Парково проектиране", "Декоративна дендрология",
                // 11Д
                "Учебна практика по Работа в учебно предприятие",
                "Организация на брокерската дейност",
                "Учебна практика по маркетинг",
                "Учебна практика по основи на сградостроенето",

                // 12А
                "Интернет програмиране",
                "Учебна практика по интернет програмиране",
                "Софтуерно инженерство",
                "Учебна практика по Софтуерно инженерство",
                // 12Б
                "Учебна практика по пътища и съоражения",
                "Учебна практика по строителна дейност и контрол",
                "Учебна практика по стоманобетонни и стоманени конструкции",
                // 12В
                "Настройка и поддържане на машини",
                // 12Г
                "Лесоустройство",
                "Предприемачество в горското стопанство",
                "Охрана на труда и борба с горските пожари",
                "Учебна практика по паркова архитектура",
                // 12Д
                "Учебна практика по инженерна геодезия",
                "Учебна практика по кадастър",
                "Инженерна геодезия",
                "Строителна дейност и контрол",
            };

            var subjects = allSubjectNames
                .Select(n => new Subject { Name = n }).ToList();
            context.Subjects.AddRange(subjects);
            context.SaveChanges();

            var subj = subjects.ToDictionary(s => s.Name);

            // --- УЧИТЕЛИ ---
            var teacherInfo = new[]
            {
                ("Анна",      "Тодорова",  "teacher_bg"),    // 0 Български
                ("Петър",     "Стоянов",   "teacher_math"),  // 1 Математика
                ("Георги",    "Маринов",   "teacher_pe"),    // 2 Физическо
                ("Елена",     "Христова",  "teacher_en"),    // 3 Английски
                ("Мария",     "Колева",    "teacher_art"),   // 4 Изобразително
                ("Иван",      "Попов",     "teacher_geo"),   // 5 География
                ("Надежда",   "Атанасова", "teacher_hist"),  // 6 История
                ("Димитър",   "Йорданов",  "teacher_de"),    // 7 Немски
                ("Светлана",  "Николова",  "teacher_ru"),    // 8 Руски
                ("Кирил",     "Василев",   "teacher_sp"),    // 9 СП специалност
                ("Радостина", "Петрова",   "teacher_stroit"),// 10 Строителство
                ("Стоян",     "Димитров",  "teacher_meb"),   // 11 Мебелно
                ("Виктория",  "Андреева",  "teacher_design"),// 12 Дизайн/Горско
                ("Александър","Тончев",    "teacher_geod"),  // 13 Геодезия/Икономика
                ("Камелия",   "Стоева",    "teacher_pred"),  // 14 Предприемачество
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

            var t_bg = teachers[0];
            var t_math = teachers[1];
            var t_pe = teachers[2];
            var t_en = teachers[3];
            var t_art = teachers[4];
            var t_geo = teachers[5];
            var t_hist = teachers[6];
            var t_de = teachers[7];
            var t_ru = teachers[8];
            var t_sp = teachers[9];
            var t_stroit = teachers[10];
            var t_meb = teachers[11];
            var t_design = teachers[12];
            var t_geod = teachers[13];
            var t_pred = teachers[14];

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

            // --- ПРЕДМЕТ → УЧИТЕЛ (общи) ---
            var subjectTeacher = new Dictionary<string, Teacher>
            {
                { "Български език",           t_bg     },
                { "Математика",               t_math   },
                { "Физическо възпитание",     t_pe     },
                { "Английски език",           t_en     },
                { "Изобразително изкуство",   t_art    },
                { "География и Икономика",    t_geo    },
                { "История и Цивилизация",    t_hist   },
                { "Немски език",              t_de     },
                { "Руски език",               t_ru     },
                // А track
                { "Информационни технологии",                               t_sp },
                { "Устройство на компютърните системи",                     t_sp },
                { "Обектно-ориентирано програмиране",                       t_sp },
                { "Учебна практика по Обектно-ориентирано програмиране",    t_sp },
                { "Учебна практика по програмиране",                        t_sp },
                { "Увод в алгоритмите и структурите от данни",              t_sp },
                { "Учебна практика по увод в алгоритмите и структурите от данни", t_sp },
                { "Управление на хардуер с Ардуино",                        t_sp },
                { "Компютърни мрежи",                                       t_sp },
                { "Разработка на софтуер",                                  t_sp },
                { "Учебна практика по Компютърни мрежи",                    t_sp },
                { "Учебна практика по Разработка на софтуер",               t_sp },
                { "Интернет програмиране",                                  t_sp },
                { "Учебна практика по интернет програмиране",               t_sp },
                { "Софтуерно инженерство",                                  t_sp },
                { "Учебна практика по Софтуерно инженерство",               t_sp },
                // Б track
                { "Учебна практика по строителни материали",                t_stroit },
                { "Учебна практика по Строителна графика",                  t_stroit },
                { "Учебна практика по сградостроителство",                  t_stroit },
                { "Учебна практика по градоустройство и архитектурно проектиране", t_stroit },
                { "Технология на строителните процеси",                     t_stroit },
                { "Учебна практика по изпълнение на строително-монтажни работи",   t_stroit },
                { "Учебна практика по пътища и съоражения",                 t_stroit },
                { "Учебна практика по строителна дейност и контрол",        t_stroit },
                { "Учебна практика по стоманобетонни и стоманени конструкции",     t_stroit },
                // В track
                { "Учебна практика по мебелно производство",               t_meb },
                { "Учебна практика по конструктивно знание и чертане",      t_meb },
                { "Технологии и машини в мебелното производство",           t_meb },
                { "Учебна практика по дървообработване",                    t_meb },
                { "Конструиране на мебели",                                 t_meb },
                { "Проектиране на мебели и интериор",                       t_meb },
                { "Настройка и поддържане на машини",                       t_meb },
                // Г track (Дизайн + Горско)
                { "Учебна практика по рисунка типаж и анимации",            t_design },
                { "Материалознание",                                         t_design },
                { "Рисуване и композиция",                                   t_design },
                { "Графични техники",                                        t_design },
                { "Учебна практика по конструиране на мебели",              t_design },
                { "Парково строителство",                                    t_design },
                { "Лесовъдство",                                             t_design },
                { "Горско законознание",                                     t_design },
                { "Парково проектиране",                                     t_design },
                { "Декоративна дендрология",                                 t_design },
                { "Лесоустройство",                                          t_design },
                { "Предприемачество в горското стопанство",                  t_design },
                { "Охрана на труда и борба с горските пожари",               t_design },
                { "Учебна практика по паркова архитектура",                  t_design },
                // Д track (Геодезия + Икономика)
                { "Топиари (парково)",                                       t_geod },
                { "Учебна практика по ловно и рибовъдно стопанство",        t_geod },
                { "Учебна практика по топографско чертане",                  t_geod },
                { "Учебна практика по геодезия",                             t_geod },
                { "Икономика и управление на търговията",                    t_geod },
                { "Счетоводство на предприятието",                           t_geod },
                { "Учебна практика по организация на брокерската дейност",   t_geod },
                { "Учебна практика по Работа в учебно предприятие",         t_geod },
                { "Организация на брокерската дейност",                      t_geod },
                { "Учебна практика по маркетинг",                            t_geod },
                { "Учебна практика по основи на сградостроенето",            t_geod },
                { "Учебна практика по инженерна геодезия",                   t_geod },
                { "Учебна практика по кадастър",                             t_geod },
                { "Инженерна геодезия",                                      t_geod },
                { "Строителна дейност и контрол",                            t_geod },
                // Предприемачество
                { "Предприемачество",                                        t_pred },
            };

            // --- ПРЕДМЕТИ ПО КЛАС ---
            var commonAll = new[] { "Български език", "Математика", "Физическо възпитание", "Английски език", "Изобразително изкуство", "География и Икономика", "История и Цивилизация" };
            var de_grades = new[] { "9А", "9Б", "9В", "9Г", "9Д", "10А", "10Б", "10В", "10Г", "10Д" };
            var ru_grades = new[] { "11А", "11Б", "11В", "11Г", "11Д", "12А", "12Б", "12В", "12Г", "12Д" };

            var specialByClass = new Dictionary<string, string[]>
            {
                // 8-ми
                { "8А", new[] { "Информационни технологии","Устройство на компютърните системи" } },
                { "8Б", new[] { "Информационни технологии","Предприемачество" } },
                { "8В", new[] { "Информационни технологии","Предприемачество" } },
                { "8Г", new[] { "Информационни технологии","Предприемачество" } },
                { "8Д", new[] { "Топиари (парково)","Информационни технологии","Предприемачество" } },
                // 9-ти
                { "9А", new[] { "Обектно-ориентирано програмиране","Учебна практика по Обектно-ориентирано програмиране","Учебна практика по програмиране" } },
                { "9Б", new[] { "Учебна практика по строителни материали","Учебна практика по Строителна графика","Учебна практика по сградостроителство" } },
                { "9В", new[] { "Учебна практика по мебелно производство","Учебна практика по конструктивно знание и чертане" } },
                { "9Г", new[] { "Учебна практика по рисунка типаж и анимации","Материалознание","Рисуване и композиция" } },
                { "9Д", new[] { "Учебна практика по ловно и рибовъдно стопанство","Учебна практика по топографско чертане","Учебна практика по геодезия" } },
                // 10-ти
                { "10А", new[] { "Увод в алгоритмите и структурите от данни","Учебна практика по увод в алгоритмите и структурите от данни","Управление на хардуер с Ардуино" } },
                { "10Б", new[] { "Учебна практика по сградостроителство","Учебна практика по строителни материали" } },
                { "10В", new[] { "Технологии и машини в мебелното производство","Учебна практика по дървообработване" } },
                { "10Г", new[] { "Графични техники","Учебна практика по рисунка типаж и анимации","Учебна практика по конструиране на мебели" } },
                { "10Д", new[] { "Икономика и управление на търговията","Счетоводство на предприятието","Учебна практика по организация на брокерската дейност" } },
                // 11-ти
                { "11А", new[] { "Компютърни мрежи","Разработка на софтуер","Учебна практика по Компютърни мрежи","Учебна практика по Разработка на софтуер" } },
                { "11Б", new[] { "Учебна практика по градоустройство и архитектурно проектиране","Технология на строителните процеси","Учебна практика по изпълнение на строително-монтажни работи" } },
                { "11В", new[] { "Учебна практика по мебелно производство","Конструиране на мебели","Проектиране на мебели и интериор" } },
                { "11Г", new[] { "Парково строителство","Лесовъдство","Горско законознание","Парково проектиране","Декоративна дендрология" } },
                { "11Д", new[] { "Учебна практика по Работа в учебно предприятие","Организация на брокерската дейност","Учебна практика по маркетинг","Учебна практика по основи на сградостроенето" } },
                // 12-ти
                { "12А", new[] { "Интернет програмиране","Учебна практика по интернет програмиране","Софтуерно инженерство","Учебна практика по Софтуерно инженерство" } },
                { "12Б", new[] { "Учебна практика по пътища и съоражения","Учебна практика по градоустройство и архитектурно проектиране","Учебна практика по строителна дейност и контрол","Учебна практика по стоманобетонни и стоманени конструкции" } },
                { "12В", new[] { "Учебна практика по мебелно производство","Технологии и машини в мебелното производство","Настройка и поддържане на машини" } },
                { "12Г", new[] { "Лесоустройство","Предприемачество в горското стопанство","Охрана на труда и борба с горските пожари","Учебна практика по паркова архитектура" } },
                { "12Д", new[] { "Учебна практика по инженерна геодезия","Учебна практика по кадастър","Инженерна геодезия","Строителна дейност и контрол" } },
            };

            // --- CLASS SUBJECTS ---
            var classSubjects = new List<ClassSubject>();
            foreach (var c in classes)
            {
                var subjNames = new List<string>(commonAll);
                if (de_grades.Contains(c.Name)) subjNames.Add("Немски език");
                if (ru_grades.Contains(c.Name)) subjNames.Add("Руски език");
                if (specialByClass.TryGetValue(c.Name, out var specials))
                    subjNames.AddRange(specials);

                foreach (var sn in subjNames)
                {
                    if (!subjectTeacher.ContainsKey(sn)) continue;
                    classSubjects.Add(new ClassSubject
                    {
                        ClassId = c.Id,
                        SubjectId = subj[sn].Id,
                        TeacherId = subjectTeacher[sn].Id
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
                var teacher = subjectTeacher[subjects.First(s => s.Id == cs.SubjectId).Name];
                bool placed = false;

                for (int d = 0; d < 5 && !placed; d++)
                    for (int p = 0; p < 7 && !placed; p++)
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
            context.ScheduleSlots.AddRange(scheduleSlots);
            context.SaveChanges();

            // --- УЧЕНИЦИ ---
            var maleNames = new[] { "Иван", "Георги", "Петър", "Димитър", "Стоян", "Николай", "Александър", "Мартин", "Кристиан", "Даниел" };
            var femaleNames = new[] { "Мария", "Елена", "Ивана", "Петя", "Надежда", "Кристина", "Виктория", "Александра", "Симона", "Даниела" };
            var maleLastNames = new[] { "Иванов", "Петров", "Георгиев", "Димитров", "Стоянов", "Николаев", "Тодоров", "Христов", "Попов", "Йорданов", "Колев", "Атанасов", "Маринов", "Илиев", "Станев", "Василев", "Добрев", "Ангелов", "Тончев", "Кирилов" };
            var femaleLastNames = new[] { "Иванова", "Петрова", "Георгиева", "Димитрова", "Стоянова", "Николаева", "Тодорова", "Христова", "Попова", "Йорданова", "Колева", "Атанасова", "Маринова", "Илиева", "Станева", "Василева", "Добрева", "Ангелова", "Тончева", "Кирилова" };

            var rng = new Random(42);
            var allStudents = new List<Student>();

            // А, Б, В = 20 ученика; Г, Д = 26 ученика
            foreach (var c in classes)
            {
                int count = (c.Name.EndsWith("Г") || c.Name.EndsWith("Д")) ? 26 : 20;
                for (int i = 0; i < count; i++)
                {
                    bool male = rng.Next(2) == 0;
                    var firstName = male ? maleNames[rng.Next(maleNames.Length)] : femaleNames[rng.Next(femaleNames.Length)];
                    var lastName = male ? maleLastNames[rng.Next(maleLastNames.Length)] : femaleLastNames[rng.Next(femaleLastNames.Length)];
                    allStudents.Add(new Student { FirstName = firstName, LastName = lastName, ClassId = c.Id });
                }
            }
            context.Students.AddRange(allStudents);
            context.SaveChanges();

            // --- DEMO УЧЕНИЦИ ---
            // Намираме първия ученик от всеки клас по индекс
            var classCounts = new Dictionary<string, int>
            {
                {"8А",0},{"8Б",20},{"8В",40},{"8Г",60},{"8Д",86},
                {"9А",112}
            };

            var demoData = new[]
            {
                (allStudents[0],   "student1"),   // 8А - СП
                (allStudents[20],  "student2"),   // 8Б - Строителство
                (allStudents[40],  "student3"),   // 8В - Мебелно
                (allStudents[60],  "student4"),   // 8Г - Дизайн
                (allStudents[86],  "student5"),   // 8Д - Геодезия
                (allStudents[112], "student6"),   // 9А - СП
            };

            foreach (var (s, username) in demoData)
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

            // Оценки за първите 10 класа
            var studentsForGrades = allStudents
                .Where(s => classes.Take(10).Select(c => c.Id).Contains(s.ClassId))
                .ToList();

            foreach (var student in studentsForGrades)
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
                            GradedAt = DateTime.Now.AddDays(-rng.Next(1, 90)),
                        });
                    }
                }
            }
            context.Grades.AddRange(allGrades);
            context.SaveChanges();

            // --- ОТСЪСТВИЯ ---
            var statuses = new[] { "Отсъства", "Закъснял", "Извинено" };
            var allAttendances = new List<Attendance>();

            foreach (var student in studentsForGrades)
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