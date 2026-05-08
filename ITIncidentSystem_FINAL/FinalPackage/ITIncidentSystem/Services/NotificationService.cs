using ITIncidentSystem.Events;

namespace ITIncidentSystem.Services;

/// <summary>
/// Услуга за известявания — Слушател №1 на всички събития.
///
/// Реагира на всяко събитие с конзолно съобщение (в реален проект
/// изпраща имейл, push нотификация, Teams/Slack съобщение и т.н.).
///
/// Абонира се към: IncidentCreated, IncidentAssigned,
///                 IncidentResolved, CriticalIncidentDetected
/// </summary>
public class NotificationService
{
    // ── Статистика на получените известявания ─────────────────────────────

    public int TotalNotificationsSent { get; private set; } = 0;
    public int CriticalAlertsSent     { get; private set; } = 0;

    // ── Слушатели (event handlers) ────────────────────────────────────────

    /// <summary>
    /// Реагира на IncidentCreated — известява техниците за нов инцидент.
    /// Абонира се: manager.IncidentCreated += notifications.OnIncidentCreated;
    /// </summary>
    public void OnIncidentCreated(object? sender, IncidentEventArgs e)
    {
        TotalNotificationsSent++;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n  [ИЗВЕСТИЕ] 🆕 Нов инцидент регистриран!");
        Console.ResetColor();
        Console.WriteLine($"  ├─ Номер   : {e.IncidentNumber}");
        Console.WriteLine($"  ├─ Заглавие: {e.Title}");
        Console.WriteLine($"  ├─ Приоритет: {e.Priority}");
        Console.WriteLine($"  ├─ Отдел   : {e.DepartmentName}");
        Console.WriteLine($"  ├─ Потребител: {e.UserName}");
        Console.WriteLine($"  └─ Час     : {e.Timestamp:HH:mm:ss}");
    }

    /// <summary>
    /// Реагира на IncidentAssigned — известява техника за новото задание.
    /// </summary>
    public void OnIncidentAssigned(object? sender, IncidentEventArgs e)
    {
        TotalNotificationsSent++;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n  [ИЗВЕСТИЕ] 👷 Инцидент назначен на техник!");
        Console.ResetColor();
        Console.WriteLine($"  ├─ Номер   : {e.IncidentNumber}");
        Console.WriteLine($"  ├─ Заглавие: {e.Title}");
        Console.WriteLine($"  ├─ Техник  : {e.TechnicianName ?? "—"}");
        Console.WriteLine($"  └─ Час     : {e.Timestamp:HH:mm:ss}");
    }

    /// <summary>
    /// Реагира на IncidentResolved — известява потребителя, че проблемът е решен.
    /// </summary>
    public void OnIncidentResolved(object? sender, IncidentEventArgs e)
    {
        TotalNotificationsSent++;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n  [ИЗВЕСТИЕ] ✅ Инцидент решен!");
        Console.ResetColor();
        Console.WriteLine($"  ├─ Номер   : {e.IncidentNumber}");
        Console.WriteLine($"  ├─ Заглавие: {e.Title}");
        Console.WriteLine($"  ├─ Решен от: {e.TechnicianName ?? "—"}");

        if (e.ResolutionMinutes.HasValue)
        {
            string duration = e.ResolutionMinutes.Value >= 60
                ? $"{e.ResolutionMinutes.Value / 60:F0} ч. {e.ResolutionMinutes.Value % 60:F0} мин."
                : $"{e.ResolutionMinutes.Value:F0} мин.";
            Console.WriteLine($"  ├─ Времетраене: {duration}");
        }

        if (!string.IsNullOrEmpty(e.Message))
            Console.WriteLine($"  ├─ Бележки: {e.Message}");

        Console.WriteLine($"  └─ Час     : {e.Timestamp:HH:mm:ss}");
    }

    /// <summary>
    /// Реагира на CriticalIncidentDetected — АЛАРМА с червен цвят!
    /// Ескалира до мениджмънта и показва краен срок за реакция.
    /// </summary>
    public void OnCriticalDetected(object? sender, CriticalAlertEventArgs e)
    {
        TotalNotificationsSent++;
        CriticalAlertsSent++;

        // Визуална аларма — мига в конзолата
        Console.Beep(800, 300);

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n  ╔══════════════════════════════════════════════════╗");
        Console.WriteLine($"  ║  🚨 КРИТИЧЕН ИНЦИДЕНТ — {e.AlertLevel,-24} ║");
        Console.WriteLine("  ╠══════════════════════════════════════════════════╣");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  ║  Номер   : {e.IncidentNumber,-40} ║");
        Console.WriteLine($"  ║  Заглавие: {e.Title.PadRight(40).Substring(0, 40)} ║");
        Console.WriteLine($"  ║  Отдел   : {e.DepartmentName,-40} ║");

        if (e.AffectedSystems.Count > 0)
        {
            string systems = string.Join(", ", e.AffectedSystems);
            Console.WriteLine($"  ║  Системи : {systems.PadRight(40).Substring(0, Math.Min(systems.Length, 40))} ║");
        }

        Console.WriteLine($"  ║  Краен срок: {e.ResponseDeadline:HH:mm:ss,-37} ║");

        if (e.EscalationRequired)
        {
            Console.WriteLine("  ║                                                  ║");
            Console.WriteLine("  ║  ⬆  ЕСКАЛИРАНО ДО ИТ МЕНИДЖЪР                  ║");
        }

        Console.WriteLine("  ╚══════════════════════════════════════════════════╝");
        Console.ResetColor();
    }

    // ── Справка ───────────────────────────────────────────────────────────

    public void PrintStats()
    {
        Console.WriteLine($"\n  [NotificationService] Изпратени известявания: " +
                          $"{TotalNotificationsSent} (критични аларми: {CriticalAlertsSent})");
    }
}
