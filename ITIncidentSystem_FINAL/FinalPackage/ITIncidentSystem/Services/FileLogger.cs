using ITIncidentSystem.Events;
using ITIncidentSystem.Exceptions;
using ITIncidentSystem.Models;

namespace ITIncidentSystem.Services;

/// <summary>
/// Клас за файлово логиране и експортиране на данни.
///
/// Отговорности:
///   1. Записва всяко действие в incident.log
///   2. Записва критичните инциденти в critical.log
///   3. Експортира списък с инциденти в .csv и .txt
///
/// Слуша събитията от IncidentManager (Слушател №2).
/// </summary>
public class FileLogger
{
    // ── Пътища до файловете ───────────────────────────────────────────────

    private readonly string _logDir;
    private readonly string _incidentLogPath;
    private readonly string _criticalLogPath;
    private readonly object _lock = new();   // за thread-safe запис

    // ── Конструктор ───────────────────────────────────────────────────────

    public FileLogger(string logDirectory = "Logs")
    {
        _logDir          = logDirectory;
        _incidentLogPath = Path.Combine(logDirectory, "incident.log");
        _criticalLogPath = Path.Combine(logDirectory, "critical.log");

        EnsureDirectoryExists();
    }

    // ── Слушатели на събитията ────────────────────────────────────────────

    /// <summary>
    /// Слушател за IncidentCreated — логва регистрирането на нов инцидент.
    /// Абонира се: manager.IncidentCreated += logger.OnIncidentCreated;
    /// </summary>
    public void OnIncidentCreated(object? sender, IncidentEventArgs e)
    {
        string line = $"[CREATED]  {e.ToLogLine()}";
        WriteToLog(_incidentLogPath, line);
    }

    /// <summary>Слушател за IncidentAssigned — логва назначаването на техник.</summary>
    public void OnIncidentAssigned(object? sender, IncidentEventArgs e)
    {
        string line = $"[ASSIGNED] {e.ToLogLine()}";
        WriteToLog(_incidentLogPath, line);
    }

    /// <summary>Слушател за IncidentResolved — логва решаването на инцидент.</summary>
    public void OnIncidentResolved(object? sender, IncidentEventArgs e)
    {
        string line = $"[RESOLVED] {e.ToLogLine()}";
        WriteToLog(_incidentLogPath, line);
    }

    /// <summary>
    /// Слушател за CriticalIncidentDetected — пише в ОТДЕЛЕН критичен лог.
    /// Критичните инциденти изискват специално внимание и отделен файл.
    /// </summary>
    public void OnCriticalDetected(object? sender, CriticalAlertEventArgs e)
    {
        // Записва в основния лог
        WriteToLog(_incidentLogPath, $"[CRITICAL] {e.ToLogLine()}");

        // Записва и в специалния критичен лог
        string separator = new string('!', 80);
        WriteToLog(_criticalLogPath, separator);
        WriteToLog(_criticalLogPath, e.ToLogLine());
        if (e.AffectedSystems.Count > 0)
            WriteToLog(_criticalLogPath,
                $"           Засегнати системи: {string.Join(", ", e.AffectedSystems)}");
        WriteToLog(_criticalLogPath,
            $"           Краен срок за реакция: {e.ResponseDeadline:dd.MM.yyyy HH:mm:ss}");
        WriteToLog(_criticalLogPath, separator);
    }

    // ── Запис в лог ───────────────────────────────────────────────────────

    /// <summary>
    /// Универсален метод за запис на ред в лог файл.
    /// lock гарантира, че при многонишково изпълнение файлът не се разваля.
    /// </summary>
    public void WriteToLog(string filePath, string message)
    {
        lock (_lock)
        {
            try
            {
                File.AppendAllText(filePath, message + Environment.NewLine);
            }
            catch (IOException ex)
            {
                throw new FileLogException(filePath, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new FileLogException(filePath,
                    $"Няма права за запис: {ex.Message}");
            }
        }
    }

    /// <summary>Логва произволно съобщение с timestamp.</summary>
    public void Log(string message, string level = "INFO")
    {
        string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
        WriteToLog(_incidentLogPath, line);
    }

    // ── Експортиране ──────────────────────────────────────────────────────

    /// <summary>
    /// Експортира списък инциденти в CSV файл.
    /// Заглавният ред съдържа имената на колоните.
    /// </summary>
    public void ExportToCsv(IEnumerable<Incident> incidents, string? fileName = null)
    {
        fileName ??= $"export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string filePath = Path.Combine(_logDir, fileName);

        try
        {
            var lines = new List<string>
            {
                // Заглавен ред
                "ИнцидентID,Номер,Заглавие,Категория,Приоритет,Статус," +
                "ПотребителID,ТехникID,ОтделID,Създаден,Назначен,Решен,ВремеМинути"
            };

            foreach (var i in incidents)
            {
                string resolved = i.ResolvedAt.HasValue
                    ? i.ResolvedAt.Value.ToString("dd.MM.yyyy HH:mm")
                    : "";
                string assigned = i.AssignedAt.HasValue
                    ? i.AssignedAt.Value.ToString("dd.MM.yyyy HH:mm")
                    : "";
                string resTime = i.ResolutionTimeMinutes.HasValue
                    ? i.ResolutionTimeMinutes.Value.ToString("F0")
                    : "";

                // Ескейпваме запетаи в заглавието
                string safeTitle = $"\"{i.Title.Replace("\"", "\"\"")}\"";

                lines.Add($"{i.Id},{i.IncidentNumber},{safeTitle}," +
                          $"{i.Category},{i.Priority},{i.Status}," +
                          $"{i.UserId},{i.TechnicianId?.ToString() ?? ""}," +
                          $"{i.DepartmentId}," +
                          $"{i.CreatedAt:dd.MM.yyyy HH:mm}," +
                          $"{assigned},{resolved},{resTime}");
            }

            File.WriteAllLines(filePath, lines);
            Log($"CSV експорт: {filePath} ({lines.Count - 1} записа)");
            Console.WriteLine($"  [FileLogger] CSV записан: {filePath}");
        }
        catch (IOException ex)
        {
            throw new FileLogException(filePath, ex);
        }
    }

    /// <summary>
    /// Експортира инцидентите в четим текстов формат.
    /// </summary>
    public void ExportToTxt(IEnumerable<Incident> incidents, string? fileName = null)
    {
        fileName ??= $"report_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        string filePath = Path.Combine(_logDir, fileName);

        try
        {
            var lines = new List<string>();
            string sep = new string('─', 70);

            lines.Add("═══════════════════════════════════════════════════════════════════════");
            lines.Add("       ОТЧЕТ — IT INCIDENT MANAGEMENT SYSTEM");
            lines.Add($"       Генериран: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            lines.Add("═══════════════════════════════════════════════════════════════════════");
            lines.Add("");

            int count = 0;
            foreach (var i in incidents)
            {
                count++;
                lines.Add(sep);
                lines.Add($"  {i.IncidentNumber}  |  {i.Title}");
                lines.Add($"  Категория : {i.Category,-15}  Приоритет: {i.Priority}");
                lines.Add($"  Статус    : {i.Status,-15}  Отдел ID : {i.DepartmentId}");
                lines.Add($"  Създаден  : {i.CreatedAt:dd.MM.yyyy HH:mm}");
                if (i.TechnicianId.HasValue)
                    lines.Add($"  Техник ID : {i.TechnicianId}  Назначен: {i.AssignedAt:dd.MM.yyyy HH:mm}");
                if (i.ResolvedAt.HasValue)
                    lines.Add($"  Решен     : {i.ResolvedAt:dd.MM.yyyy HH:mm}" +
                              $"  (за {i.ResolutionTimeMinutes:F0} мин.)");
                if (!string.IsNullOrEmpty(i.ResolutionNotes))
                    lines.Add($"  Бележки   : {i.ResolutionNotes}");
            }

            lines.Add(sep);
            lines.Add($"  Общо инциденти: {count}");

            File.WriteAllLines(filePath, lines);
            Log($"TXT отчет: {filePath} ({count} записа)");
            Console.WriteLine($"  [FileLogger] TXT записан: {filePath}");
        }
        catch (IOException ex)
        {
            throw new FileLogException(filePath, ex);
        }
    }

    // ── Помощни ───────────────────────────────────────────────────────────

    private void EnsureDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_logDir))
                Directory.CreateDirectory(_logDir);
        }
        catch (IOException ex)
        {
            throw new FileLogException(_logDir,
                $"Не може да се създаде директория: {ex.Message}");
        }
    }

    /// <summary>Чете последните N реда от лог файла.</summary>
    public IEnumerable<string> ReadLastLogLines(int count = 20)
    {
        if (!File.Exists(_incidentLogPath))
            return Enumerable.Empty<string>();

        try
        {
            var lines = File.ReadAllLines(_incidentLogPath);
            return lines.TakeLast(count);
        }
        catch (IOException ex)
        {
            throw new FileLogException(_incidentLogPath, ex);
        }
    }
}
