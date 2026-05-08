using ITIncidentSystem.Enums;

namespace ITIncidentSystem.Models;

/// <summary>
/// Модел: Технически инцидент — централният обект на системата.
/// Един инцидент има пълен жизнен цикъл: Open → InProgress → Resolved → Closed.
/// Всяка промяна на статус се записва в IncidentHistory.
/// </summary>
public class Incident
{
    // ── Свойства ──────────────────────────────────────────────────────────

    public int              Id              { get; set; }

    /// <summary>Форматиран номер: INC-00001</summary>
    public string           IncidentNumber  => $"INC-{Id:D5}";

    public string           Title           { get; set; } = string.Empty;
    public string           Description     { get; set; } = string.Empty;

    public IncidentCategory Category        { get; set; }
    public IncidentPriority Priority        { get; set; }
    public IncidentStatus   Status          { get; set; } = IncidentStatus.Open;

    // ── FK към свързани обекти ────────────────────────────────────────────

    public int  UserId        { get; set; }
    public int? TechnicianId  { get; set; }    // null = не е назначен техник
    public int  DepartmentId  { get; set; }

    // ── Дати ─────────────────────────────────────────────────────────────

    public DateTime  CreatedAt      { get; set; } = DateTime.Now;
    public DateTime? AssignedAt     { get; set; }   // null = не е назначен
    public DateTime? ResolvedAt     { get; set; }   // null = не е решен

    public string? ResolutionNotes { get; set; }

    // ── Навигационни свойства ─────────────────────────────────────────────

    public User?       User       { get; set; }
    public Technician? Technician { get; set; }
    public Department? Department { get; set; }

    /// <summary>Пълна история на промените по инцидента.</summary>
    public List<IncidentHistory> History { get; set; } = new();

    // ── Конструктори ──────────────────────────────────────────────────────

    public Incident() { }

    public Incident(int id, string title, string description,
                    IncidentCategory category, IncidentPriority priority,
                    int userId, int departmentId)
    {
        Id           = id;
        Title        = title;
        Description  = description;
        Category     = category;
        Priority     = priority;
        UserId       = userId;
        DepartmentId = departmentId;
    }

    // ── Изчисляеми свойства ───────────────────────────────────────────────

    /// <summary>Времетраене до решаване (в минути). Null ако не е решен.</summary>
    public double? ResolutionTimeMinutes
        => ResolvedAt.HasValue
            ? (ResolvedAt.Value - CreatedAt).TotalMinutes
            : null;

    /// <summary>Дали инцидентът е активен (не е затворен/отменен)?</summary>
    public bool IsActive
        => Status is IncidentStatus.Open or IncidentStatus.InProgress;

    /// <summary>Дали инцидентът е критичен?</summary>
    public bool IsCritical
        => Priority == IncidentPriority.Critical;

    // ── ToString ──────────────────────────────────────────────────────────

    public override string ToString()
        => $"[{IncidentNumber}] {Title} | {Category} | {Priority} | {Status}";
}
