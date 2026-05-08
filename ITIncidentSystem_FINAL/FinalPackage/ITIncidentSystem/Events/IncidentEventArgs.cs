using ITIncidentSystem.Enums;

namespace ITIncidentSystem.Events;

/// <summary>
/// Аргументи за стандартните събития на инцидент:
///   IncidentCreated, IncidentAssigned, IncidentResolved.
///
/// Наследява EventArgs — това е изискването на .NET event pattern.
/// Съдържа цялата информация, нужна на ВСЕКИ слушател да реагира,
/// без да трябва да прави допълнителни заявки.
/// </summary>
public class IncidentEventArgs : EventArgs
{
    // ── Основна информация ────────────────────────────────────────────────

    /// <summary>Уникален ID на инцидента.</summary>
    public int IncidentId { get; init; }

    /// <summary>Форматиран номер: INC-00001</summary>
    public string IncidentNumber => $"INC-{IncidentId:D5}";

    /// <summary>Кратко заглавие на инцидента.</summary>
    public string Title { get; init; } = string.Empty;

    // ── Статус и приоритет ────────────────────────────────────────────────

    public IncidentPriority Priority  { get; init; }
    public IncidentStatus?  OldStatus { get; init; }   // null при събитие Created
    public IncidentStatus   NewStatus { get; init; }

    // ── Участници ─────────────────────────────────────────────────────────

    /// <summary>Пълно имена на потребителя, подал инцидента.</summary>
    public string UserName       { get; init; } = string.Empty;

    /// <summary>Пълно имена на назначения техник (null ако не е назначен).</summary>
    public string? TechnicianName { get; init; }

    /// <summary>Наименование на засегнатия отдел.</summary>
    public string DepartmentName { get; init; } = string.Empty;

    // ── Допълнителни данни ────────────────────────────────────────────────

    /// <summary>Момент на изстрелване на събитието.</summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;

    /// <summary>Свободен текст — бележки, резолюция и т.н.</summary>
    public string? Message { get; init; }

    /// <summary>Времетраене до решаване (попълва се само при Resolved).</summary>
    public double? ResolutionMinutes { get; init; }

    // ── Конструктор ───────────────────────────────────────────────────────

    public IncidentEventArgs(
        int incidentId, string title,
        IncidentPriority priority, IncidentStatus newStatus,
        string userName, string departmentName,
        IncidentStatus? oldStatus = null,
        string? technicianName = null,
        string? message = null,
        double? resolutionMinutes = null)
    {
        IncidentId       = incidentId;
        Title            = title;
        Priority         = priority;
        NewStatus        = newStatus;
        OldStatus        = oldStatus;
        UserName         = userName;
        DepartmentName   = departmentName;
        TechnicianName   = technicianName;
        Message          = message;
        ResolutionMinutes = resolutionMinutes;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>Форматиран лог ред — използва се от FileLogger.</summary>
    public string ToLogLine()
    {
        string old = OldStatus.HasValue ? $"{OldStatus} → " : "";
        string tech = TechnicianName != null ? $" | Техник: {TechnicianName}" : "";
        string res = ResolutionMinutes.HasValue
            ? $" | Решено за: {ResolutionMinutes:F0} мин."
            : "";
        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {IncidentNumber} | {Title} | " +
               $"{Priority} | {old}{NewStatus}{tech}{res}";
    }
}
