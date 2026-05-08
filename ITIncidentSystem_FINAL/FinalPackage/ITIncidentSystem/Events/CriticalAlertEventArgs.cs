using ITIncidentSystem.Enums;

namespace ITIncidentSystem.Events;

/// <summary>
/// Аргументи за събитието CriticalIncidentDetected.
///
/// Наследява IncidentEventArgs — всичко от базовия клас плюс
/// допълнителни полета за ескалация и засегнати системи.
///
/// Демонстрира наследяване на EventArgs класове.
/// </summary>
public class CriticalAlertEventArgs : IncidentEventArgs
{
    // ── Специфични полета за критични инциденти ───────────────────────────

    /// <summary>
    /// Ниво на алертата: "P1-CRITICAL", "P2-HIGH".
    /// При Security инциденти автоматично се задава "P1-SECURITY".
    /// </summary>
    public string AlertLevel { get; init; } = "P1-CRITICAL";

    /// <summary>
    /// Засегнати системи/ресурси (напр. "Сървър DB01", "Мрежа 3-ти етаж").
    /// Попълва се ръчно при регистриране на инцидента.
    /// </summary>
    public List<string> AffectedSystems { get; init; } = new();

    /// <summary>
    /// Изисква ли се ескалация до ИТ мениджър?
    /// Автоматично true при Critical, може да се override.
    /// </summary>
    public bool EscalationRequired { get; init; } = true;

    /// <summary>Краен срок за реакция (SLA).</summary>
    public DateTime ResponseDeadline { get; init; }

    // ── Конструктор ───────────────────────────────────────────────────────

    public CriticalAlertEventArgs(
        int incidentId, string title,
        IncidentCategory category,
        string userName, string departmentName,
        List<string>? affectedSystems = null,
        bool escalationRequired = true)
        : base(incidentId, title,
               IncidentPriority.Critical,
               IncidentStatus.Open,
               userName, departmentName)
    {
        // Ако категорията е Security — специален alert level
        AlertLevel = category == IncidentCategory.Security
            ? "P1-SECURITY"
            : "P1-CRITICAL";

        AffectedSystems    = affectedSystems ?? new List<string>();
        EscalationRequired = escalationRequired;

        // SLA: критичен инцидент трябва да се реши в рамките на 2 часа
        ResponseDeadline = DateTime.Now.AddHours(2);
    }

    // ── Override на log формата ───────────────────────────────────────────

    public new string ToLogLine()
    {
        string systems = AffectedSystems.Count > 0
            ? $" | Засегнати: {string.Join(", ", AffectedSystems)}"
            : "";
        string deadline = $" | Краен срок: {ResponseDeadline:HH:mm:ss}";
        string esc = EscalationRequired ? " | [ЕСКАЛАЦИЯ]" : "";

        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] *** {AlertLevel} *** " +
               $"{IncidentNumber} | {Title}" +
               $" | Отдел: {DepartmentName}" +
               systems + deadline + esc;
    }
}
