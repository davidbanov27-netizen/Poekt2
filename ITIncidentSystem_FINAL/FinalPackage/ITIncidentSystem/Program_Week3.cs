using ITIncidentSystem.Config;
using ITIncidentSystem.Data;
using ITIncidentSystem.Enums;
using ITIncidentSystem.Exceptions;
using ITIncidentSystem.Models;
using ITIncidentSystem.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.Title = "IT Incident Management System — Week 3 Demo";

PrintBanner();

var manager    = new IncidentManager();
var stats      = new StatisticsService();
var logger     = new FileLogger("Logs");
var loader     = new DataLoader();
var dbService  = new DatabaseService();
var notifications = new NotificationService();

manager.IncidentCreated          += notifications.OnIncidentCreated;
manager.IncidentCreated          += logger.OnIncidentCreated;
manager.CriticalIncidentDetected += notifications.OnCriticalDetected;
manager.CriticalIncidentDetected += logger.OnCriticalDetected;
manager.IncidentAssigned         += notifications.OnIncidentAssigned;
manager.IncidentAssigned         += logger.OnIncidentAssigned;
manager.IncidentResolved         += notifications.OnIncidentResolved;
manager.IncidentResolved         += logger.OnIncidentResolved;

manager.SeedTestData();

Section("1. ЗАРЕЖДАНЕ НА 30 ТЕСТОВИ ЗАПИСА ОТ JSON ФАЙЛ");

List<Incident> allIncidents = new();
try
{
    var dtos = loader.LoadIncidentsFromJson("Data/seed_data.json");
    Console.WriteLine($"  Заредени {dtos.Count} инцидента от seed_data.json\n");
    foreach (var dto in dtos)
    {
        allIncidents.Add(new Incident
        {
            Id           = dto.Id,
            Title        = dto.Title,
            Description  = dto.Description,
            Category     = Enum.Parse<IncidentCategory>(dto.Category),
            Priority     = Enum.Parse<IncidentPriority>(dto.Priority),
            Status       = Enum.Parse<IncidentStatus>(dto.Status),
            UserId       = dto.UserId,
            TechnicianId = dto.TechnicianId,
            DepartmentId = dto.DepartmentId,
            CreatedAt    = DateTime.Parse(dto.CreatedAt),
            AssignedAt   = dto.AssignedAt  != null ? DateTime.Parse(dto.AssignedAt)  : null,
            ResolvedAt   = dto.ResolvedAt  != null ? DateTime.Parse(dto.ResolvedAt)  : null,
            ResolutionNotes = dto.ResolutionNotes,
        });
    }
    Console.WriteLine($"    Resolved  : {allIncidents.Count(i => i.Status == IncidentStatus.Resolved)}");
    Console.WriteLine($"    InProgress: {allIncidents.Count(i => i.Status == IncidentStatus.InProgress)}");
    Console.WriteLine($"    Open      : {allIncidents.Count(i => i.Status == IncidentStatus.Open)}");
    Console.WriteLine($"    Critical  : {allIncidents.Count(i => i.Priority == IncidentPriority.Critical)}");
}
catch (FileLogException ex)
{
    Console.WriteLine($"  ГРЕШКА: {ex.Message}");
    allIncidents = manager.GetAll().ToList();
}

Pause();

Section("2. LINQ СПРАВКИ");

Console.WriteLine("  2a. Топ 3 най-чести типа проблеми:");
Console.WriteLine($"      {"Категория",-15} {"Брой",6} {"Процент",8}");
foreach (var (cat, cnt, pct) in stats.GetTopCategories(allIncidents, 3))
    Console.WriteLine($"      {cat,-15} {cnt,6}   {pct,6:F1}%");

Console.WriteLine("\n  2b. Инциденти по статус:");
foreach (var (status, cnt) in stats.GroupByStatus(allIncidents))
    Console.WriteLine($"      {status,-15} → {cnt}");

double avgMin = stats.GetAverageResolutionMinutes(allIncidents);
Console.WriteLine($"\n  2c. Средно времетраене: {(avgMin >= 60 ? $"{avgMin/60:F1} ч." : $"{avgMin:F0} мин.")}");

Console.WriteLine("\n  2d. Критични инциденти:");
foreach (var i in stats.GetCriticalIncidents(allIncidents))
    Console.WriteLine($"      {(i.Status==IncidentStatus.Resolved?"✓":"⚠")} {i.IncidentNumber} | {i.Category,-12} | {i.Title}");

Console.WriteLine("\n  2e. Security инциденти (Where + OrderBy):");
allIncidents.Where(i => i.Category == IncidentCategory.Security)
            .OrderBy(i => i.CreatedAt)
            .ToList()
            .ForEach(i => Console.WriteLine($"      {i.IncidentNumber} | {i.Priority,-8} | {i.Status,-12} | {i.Title}"));

Console.WriteLine("\n  2f. SLA нарушения (активни > 2 дни):");
var breaches = stats.GetSlaBreaches(allIncidents, 2);
if (breaches.Count == 0) Console.WriteLine("      Няма нарушения.");
else breaches.ForEach(i =>
    Console.WriteLine($"      {i.IncidentNumber} | {(DateTime.Now-i.CreatedAt).TotalDays:F0} дни | {i.Title}"));

Pause();

Section("3. ОБОБЩЕНА СТАТИСТИКА");

var techList = new List<Technician>
{
    new(1,"Александър Георгиев","a@co.bg",IncidentCategory.Hardware),
    new(2,"Даниел Маринов","d@co.bg",IncidentCategory.Software),
    new(3,"Кристина Василева","k@co.bg",IncidentCategory.Network),
    new(4,"Мартин Тодоров","m@co.bg",IncidentCategory.Security),
    new(5,"Симона Христова","s@co.bg",IncidentCategory.Hardware),
};

var summary = stats.GenerateSummary(allIncidents);
stats.PrintSummary(summary);

Console.WriteLine("\n  По техник:");
Console.WriteLine($"  {"Имена",-28} {"Общо",6} {"Решени",8} {"%",8} {"Средно",10}");
foreach (var ts in stats.GetTechnicianStats(allIncidents, techList))
{
    string avg = ts.AvgResolutionMin >= 60 ? $"{ts.AvgResolutionMin/60:F1}ч" : $"{ts.AvgResolutionMin:F0}мин";
    Console.WriteLine($"  {ts.FullName,-28} {ts.TotalAssigned,6} {ts.ResolvedCount,8} {ts.ResolutionRate,7:F0}% {avg,10}");
}

Pause();

Section("4. БАЗА ДАННИ");
bool dbOk = dbService.TestConnection();
if (dbOk)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("  MySQL е достъпен!\n");
    Console.ResetColor();
    try
    {
        var dbIncs = dbService.GetAllIncidents();
        Console.WriteLine($"  Заредени {dbIncs.Count} инцидента от MySQL.");
        Console.WriteLine($"  Средно (SQL AVG): {dbService.GetAvgResolutionMinutes():F0} мин.");
        Console.WriteLine("  Топ категории (SQL GROUP BY):");
        foreach (var (cat,cnt) in dbService.GetTopCategories(3))
            Console.WriteLine($"    {cat,-15} → {cnt}");

        var newInc = new Incident { Title="Тестов Week3 INSERT", Description="Демо на транзакция.",
            Category=IncidentCategory.Software, Priority=IncidentPriority.Low, UserId=1, DepartmentId=1 };
        int newId = dbService.InsertIncident(newInc);
        Console.WriteLine($"\n  INSERT успешен: INC-{newId:D5}");
        foreach (var h in dbService.GetHistory(newId))
            Console.WriteLine($"    История: {h}");
    }
    catch (DatabaseException ex) { Console.WriteLine($"  DB грешка: {ex.Message}"); }
}
else
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  MySQL не е достъпен. Стартирай schema.sql за реален тест.");
    Console.ResetColor();
    try { dbService.GetAllIncidents(); }
    catch (DatabaseException ex)
    { Console.WriteLine($"  DatabaseException: {ex.Operation} — {ex.Message}"); }
}

Pause();

Section("5. ФАЙЛОВА ДЕЙНОСТ");
Directory.CreateDirectory("Exports");
logger.ExportToCsv(allIncidents, "../Exports/incidents_week3.csv");
logger.ExportToTxt(allIncidents.Where(i => i.Priority == IncidentPriority.Critical), "../Exports/critical_report.txt");
loader.ExportToJson(allIncidents, "Exports/backup_incidents.json");
loader.ExportStatsToJson(new { Total=summary.Total, Resolved=summary.Resolved, Critical=summary.Critical,
    AvgMin=Math.Round(summary.AvgResolutionMinutes,1), Top=summary.TopCategory }, "Exports/statistics.json");
Console.WriteLine("\n  Последни редове от лога:");
foreach (var line in logger.ReadLastLogLines(5)) Console.WriteLine($"  {line}");

Pause();

Section("6. ОБРАБОТКА НА ИЗКЛЮЧЕНИЯ");

Console.WriteLine("  6a. FileLogException — липсващ файл:");
try { loader.LoadIncidentsFromJson("Data/missing.json"); }
catch (FileLogException ex) { PrintEx("FileLogException", ex.Message, ex.FilePath); }

Console.WriteLine("\n  6b. InvalidIncidentException — невалидно заглавие:");
try { manager.CreateIncident("ab","desc",IncidentCategory.Hardware,IncidentPriority.Low,1,1); }
catch (InvalidIncidentException ex) { PrintEx("InvalidIncidentException", ex.Message, ex.FieldName); }

Console.WriteLine("\n  6c. IncidentNotFoundException:");
try { manager.AssignTechnician(9999, 1); }
catch (IncidentNotFoundException ex) { PrintEx("IncidentNotFoundException", ex.Message, $"ID={ex.IncidentId}"); }

Console.WriteLine("\n  6d. TechnicianUnavailableException:");
try
{
    for (int j=1;j<=3;j++) { var t=manager.CreateIncident($"Тест{j}",$"Описание{j}",IncidentCategory.Network,IncidentPriority.Medium,3,3); manager.AssignTechnician(t.Id,3); }
    var x=manager.CreateIncident("Четвърти","Описание",IncidentCategory.Network,IncidentPriority.Low,3,3);
    manager.AssignTechnician(x.Id,3);
}
catch (TechnicianUnavailableException ex) { PrintEx("TechnicianUnavailableException",ex.Message,$"TechID={ex.TechnicianId}"); }

Console.WriteLine("\n  6e. DatabaseException:");
try { new DatabaseService("Server=invalid;Uid=x;Pwd=x;").GetAllIncidents(); }
catch (DatabaseException ex) { PrintEx("DatabaseException",ex.Message,ex.Operation); }

Console.WriteLine("\n  6f. Базово хващане IncidentSystemException:");
try { loader.LoadIncidentsFromJson("Data/none.json"); }
catch (IncidentSystemException ex)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"      {ex.GetType().Name} @ {ex.OccurredAt:HH:mm:ss}: {ex.Message}");
    Console.ResetColor();
}

Section("ДЕМОНСТРАЦИЯТА ЗАВЪРШИ");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("  Exports/incidents_week3.csv | critical_report.txt | backup_incidents.json | statistics.json");
Console.ResetColor();
Console.WriteLine("\n  ENTER за изход...");
Console.ReadLine();

static void PrintBanner()
{
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine("╔══════════════════════════════════════════════════════╗");
    Console.WriteLine("║  IT Incident Management System — Week 3 Demo         ║");
    Console.WriteLine("║  LINQ • Файлове • Изключения • MySQL                 ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════╝");
    Console.ResetColor();
}
static void Section(string t) { Console.WriteLine(); Console.ForegroundColor=ConsoleColor.DarkCyan; Console.WriteLine($"┌─── {t}"); Console.ResetColor(); }
static void Pause() { Console.ForegroundColor=ConsoleColor.DarkGray; Console.Write("\n  [ENTER...] "); Console.ResetColor(); Console.ReadLine(); }
static void PrintEx(string type, string msg, string detail) { Console.ForegroundColor=ConsoleColor.Yellow; Console.WriteLine($"      {type}: {detail}"); Console.WriteLine($"      {msg}"); Console.ResetColor(); }
