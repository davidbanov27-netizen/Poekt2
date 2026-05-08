using ITIncidentSystem.Delegates;
using ITIncidentSystem.Enums;
using ITIncidentSystem.Exceptions;
using ITIncidentSystem.Models;
using ITIncidentSystem.Services;

namespace ITIncidentSystem
{
    public class Program
    {
        public static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "IT Incident Management System — Week 2 Demo";

            PrintBanner();

            // ── 1. ИНИЦИАЛИЗАЦИЯ ──────────────────────────────────────────────────────
            Section("1. ИНИЦИАЛИЗАЦИЯ НА СИСТЕМАТА");

            var manager = new IncidentManager();
            var notifications = new NotificationService();
            var logger = new FileLogger("Logs");

            manager.SeedTestData();
            Console.WriteLine("  Тестовите данни са заредени (5 отдела, 5 техника, 5 потребители).");

            // ── 2. АБОНИРАНЕ НА СЛУШАТЕЛИТЕ ───────────────────────────────────────────
            Section("2. АБОНИРАНЕ НА СЛУШАТЕЛИ КЪМ СЪБИТИЯТА");

            // IncidentCreated → 2 слушателя (изискване: поне 2 слушателя към едно събитие)
            manager.IncidentCreated += notifications.OnIncidentCreated;
            manager.IncidentCreated += logger.OnIncidentCreated;

            // IncidentAssigned → 2 слушателя
            manager.IncidentAssigned += notifications.OnIncidentAssigned;
            manager.IncidentAssigned += logger.OnIncidentAssigned;

            // IncidentResolved → 2 слушателя
            manager.IncidentResolved += notifications.OnIncidentResolved;
            manager.IncidentResolved += logger.OnIncidentResolved;

            // CriticalIncidentDetected → 2 слушателя
            manager.CriticalIncidentDetected += notifications.OnCriticalDetected;
            manager.CriticalIncidentDetected += logger.OnCriticalDetected;

            Console.WriteLine("  ✓ NotificationService  абониран към: Created, Assigned, Resolved, Critical");
            Console.WriteLine("  ✓ FileLogger           абониран към: Created, Assigned, Resolved, Critical");
            Console.WriteLine("  ✓ Всяко събитие има минимум 2 слушателя.");

            Pause();

            // ── 3. РЕГИСТРИРАНЕ НА СТАНДАРТЕН ИНЦИДЕНТ ───────────────────────────────
            Section("3. РЕГИСТРИРАНЕ НА СТАНДАРТЕН ИНЦИДЕНТ (Medium приоритет)");
            Console.WriteLine("  Действие: CreateIncident → изстрелва IncidentCreated → 2 слушателя реагират\n");

            var inc1 = manager.CreateIncident(
                title: "Excel не стартира след ъпдейт",
                description: "След вчерашния Windows Update Excel показва грешка при стартиране.",
                category: IncidentCategory.Software,
                priority: IncidentPriority.Medium,
                userId: 1,
                departmentId: 1
            );

            Pause();

            // ── 4. РЕГИСТРИРАНЕ НА КРИТИЧЕН ИНЦИДЕНТ ─────────────────────────────────
            Section("4. РЕГИСТРИРАНЕ НА КРИТИЧЕН ИНЦИДЕНТ (Security)");
            Console.WriteLine("  Действие: CreateIncident с Critical → изстрелва IncidentCreated И CriticalIncidentDetected\n");

            var inc2 = manager.CreateIncident(
                title: "Подозрителни опити за вход — Финансов отдел",
                description: "Засечени множество неуспешни опити за вход към финансовата система от непознат IP.",
                category: IncidentCategory.Security,
                priority: IncidentPriority.Critical,
                userId: 1,
                departmentId: 1,
                affectedSystems: new List<string> { "Финансова система", "VPN сървър", "Active Directory" }
            );

            Pause();

            // ── 5. НАЗНАЧАВАНЕ НА ТЕХНИК ──────────────────────────────────────────────
            Section("5. НАЗНАЧАВАНЕ НА ТЕХНИК → изстрелва IncidentAssigned");

            Console.WriteLine($"  Назначаване на Даниел Маринов (Software) към {inc1.IncidentNumber}...\n");
            manager.AssignTechnician(inc1.Id, technicianId: 2, assignedBy: "Стефан Димитров");

            Pause();

            // ── 6. РЕШАВАНЕ НА ИНЦИДЕНТ ───────────────────────────────────────────────
            Section("6. РЕШАВАНЕ НА ИНЦИДЕНТ → изстрелва IncidentResolved");

            // Симулираме, че е изминало малко "работно" време
            // (в реална система датата се чете от DB)

            Console.WriteLine($"  Маркиране на {inc1.IncidentNumber} като решен...\n");
            manager.ResolveIncident(
                incidentId: inc1.Id,
                resolutionNotes: "Ъпдейтът е деинсталиран. Excel работи нормално. " +
                                  "Препоръчвам ексклудиране на Office от автоматичните обновявания.",
                resolvedBy: "Даниел Маринов"
            );

            Pause();

            // ── 7. ДЕМОНСТРАЦИЯ НА ДЕЛЕГАТИ ───────────────────────────────────────────
            Section("7. ДЕМОНСТРАЦИЯ НА ДЕЛЕГАТИ");

            // 7a. IncidentFilter делегат (готови филтри)
            Console.WriteLine("  7a. IncidentFilter делегат — IncidentFilters.OnlyCritical:");
            var critical = manager.GetFiltered(IncidentFilters.OnlyCritical);
            foreach (var i in critical)
                Console.WriteLine($"      → {i}");

            Console.WriteLine("\n  7b. Lambda като IncidentFilter — всички Software инциденти:");
            var software = manager.GetFiltered(i => i.Category == IncidentCategory.Software);
            foreach (var i in software)
                Console.WriteLine($"      → {i}");

            // 7c. IncidentReportFormatter делегат
            Console.WriteLine("\n  7c. IncidentReportFormatter делегат — персонализиран формат:");
            IncidentReportFormatter shortFormat =
                i => $"  [{i.IncidentNumber}] {i.Priority,-10} {i.Status,-12} — {i.Title}";

            foreach (var i in manager.GetAll())
                Console.WriteLine(shortFormat(i));

            // 7d. IncidentValidator делегат
            Console.WriteLine("\n  7d. IncidentValidator делегат — проверка на невалиден инцидент:");
            var validator = new Validators.InputValidator();
            var validateFn = validator.GetValidatorDelegate();

            var badIncident = new Incident(0, "", "Описание", IncidentCategory.Hardware,
                                            IncidentPriority.Low, 1, 1);
            bool isValid = validateFn(badIncident, out string error);
            Console.WriteLine($"      Валиден: {isValid} | Грешка: {error}");

            Pause();

            // ── 8. LINQ СТАТИСТИКА ────────────────────────────────────────────────────
            Section("8. LINQ СТАТИСТИКА");

            // Регистрираме още инциденти за по-интересна статистика
            RegisterSampleIncidents(manager);

            Console.WriteLine("  8a. Топ категории инциденти:");
            foreach (var (cat, cnt) in manager.GetTopCategories(3))
                Console.WriteLine($"      {cat,-15} → {cnt} инцидента");

            Console.WriteLine($"\n  8b. Средно времетраене: {manager.GetAverageResolutionMinutes():F1} мин.");

            Console.WriteLine("\n  8c. Инциденти по статус:");
            foreach (IncidentStatus s in Enum.GetValues<IncidentStatus>())
            {
                int cnt = manager.GetByStatus(s).Count;
                if (cnt > 0) Console.WriteLine($"      {s,-15} → {cnt}");
            }

            Console.WriteLine("\n  8d. Статистика по техник:");
            foreach (var (name, total, resolved) in manager.GetTechnicianStats())
                Console.WriteLine($"      {name,-25} → Общо: {total}  Решени: {resolved}");

            Console.WriteLine("\n  8e. Критични инциденти:");
            foreach (var i in manager.GetCriticalIncidents())
                Console.WriteLine($"      {i}");

            Pause();

            // ── 9. ОБРАБОТКА НА ИЗКЛЮЧЕНИЯ ────────────────────────────────────────────
            Section("9. ОБРАБОТКА НА ИЗКЛЮЧЕНИЯ");

            // 9a. Невалидни данни
            Console.WriteLine("  9a. Опит за инцидент с празно заглавие:");
            try
            {
                manager.CreateIncident("", "Описание", IncidentCategory.Hardware,
                                       IncidentPriority.Low, 1, 1);
            }
            catch (InvalidIncidentException ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"      Хванато: {ex.GetType().Name} — {ex.Message}");
                Console.ResetColor();
            }

            // 9b. Несъществуващ инцидент
            Console.WriteLine("\n  9b. Опит за намиране на несъществуващ инцидент (ID=9999):");
            try
            {
                manager.AssignTechnician(9999, 1);
            }
            catch (IncidentNotFoundException ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"      Хванато: {ex.GetType().Name} — {ex.Message}");
                Console.ResetColor();
            }

            // 9c. Зает техник
            Console.WriteLine("\n  9c. Опит за назначаване на зает техник:");
            try
            {
                // Мартин Тодоров (Security) — назначаваме 3 инцидента за да го заетим
                var techInc1 = manager.CreateIncident("Security тест 1", "Описание тест 1",
                    IncidentCategory.Security, IncidentPriority.High, 4, 5);
                var techInc2 = manager.CreateIncident("Security тест 2", "Описание тест 2",
                    IncidentCategory.Security, IncidentPriority.High, 5, 5);
                var techInc3 = manager.CreateIncident("Security тест 3", "Описание тест 3",
                    IncidentCategory.Security, IncidentPriority.High, 4, 5);

                manager.AssignTechnician(techInc1.Id, 4);
                manager.AssignTechnician(techInc2.Id, 4);
                manager.AssignTechnician(techInc3.Id, 4);

                // Сега техникът е зает — следващото назначаване хвърля изключение
                var techInc4 = manager.CreateIncident("Security тест 4", "Описание тест 4",
                    IncidentCategory.Security, IncidentPriority.Medium, 4, 5);
                manager.AssignTechnician(techInc4.Id, 4);   // ← трябва да хвърли
            }
            catch (TechnicianUnavailableException ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"      Хванато: {ex.GetType().Name} — {ex.Message}");
                Console.ResetColor();
            }

            Pause();

            // ── 10. ФАЙЛОВ ЕКСПОРТ ────────────────────────────────────────────────────
            Section("10. ФАЙЛОВ ЕКСПОРТ");

            Console.WriteLine("  Експортиране на всички инциденти в CSV и TXT...\n");
            logger.ExportToCsv(manager.GetAll(), "incidents_demo.csv");
            logger.ExportToTxt(manager.GetAll(), "incidents_demo.txt");

            Console.WriteLine("\n  Последни 5 реда от incident.log:");
            foreach (var line in logger.ReadLastLogLines(5))
                Console.WriteLine($"  {line}");

            // ── ФИНАЛ ─────────────────────────────────────────────────────────────────
            Section("ДЕМОНСТРАЦИЯТА ЗАВЪРШИ");
            notifications.PrintStats();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  Файлове записани в папка 'Logs/':");
            Console.WriteLine("    • incident.log        — пълен лог на всички действия");
            Console.WriteLine("    • critical.log        — само критичните инциденти");
            Console.WriteLine("    • incidents_demo.csv  — CSV експорт");
            Console.WriteLine("    • incidents_demo.txt  — текстов отчет");
            Console.ResetColor();
            Console.WriteLine("\n  Натисни ENTER за изход...");
            Console.ReadLine();
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // ПОМОЩНИ ФУНКЦИИ
        // ═══════════════════════════════════════════════════════════════════════════

        static void PrintBanner()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║       IT INCIDENT MANAGEMENT SYSTEM                     ║");
            Console.WriteLine("║       Седмица 2 — Класове, Делегати, Събития            ║");
            Console.WriteLine("║       ООП на C# • 11 клас • .NET 8.0                   ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        static void Section(string title)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"┌─── {title} ");
            Console.ResetColor();
        }

        static void Pause()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("\n  [Натисни ENTER за продължаване...] ");
            Console.ResetColor();
            Console.ReadLine();
        }

        static void RegisterSampleIncidents(IncidentManager mgr)
        {
            // Регистрираме допълнителни инциденти за LINQ статистика
            var data = new[]
            {
            ("Компютър не се включва",        IncidentCategory.Hardware,   IncidentPriority.High,   2, 1, 1),
            ("Без интернет — 3-ти етаж",      IncidentCategory.Network,    IncidentPriority.Critical,3, 2, 2),
            ("Принтерът не печата",           IncidentCategory.Peripheral, IncidentPriority.Low,     4, 4, 4),
            ("VPN не се свързва",             IncidentCategory.Network,    IncidentPriority.High,    2, 2, 2),
            ("Teams срива компютъра",         IncidentCategory.Software,   IncidentPriority.Medium,  3, 3, 3),
            ("Монитор с черни ивици",         IncidentCategory.Hardware,   IncidentPriority.Medium,  5, 5, 5),
            ("Изтекла парола",                IncidentCategory.Security,   IncidentPriority.Medium,  1, 1, 3),
            ("Мрежов суич — порт изгорял",   IncidentCategory.Network,    IncidentPriority.High,    3, 3, 3),
        };

            foreach (var (title, cat, pri, userId, deptId, techId) in data)
            {
                try
                {
                    var inc = mgr.CreateIncident(title, $"Описание: {title}",
                                                 cat, pri, userId, deptId);
                    mgr.AssignTechnician(inc.Id, techId);

                    // Решаваме High/Critical инциденти за статистика
                    if (pri >= IncidentPriority.High)
                        mgr.ResolveIncident(inc.Id, "Проблемът е отстранен.", "Техник");
                }
                catch (IncidentSystemException)
                {
                    // Пропускаме грешки при seed данни
                }
            }
        }
    }
}