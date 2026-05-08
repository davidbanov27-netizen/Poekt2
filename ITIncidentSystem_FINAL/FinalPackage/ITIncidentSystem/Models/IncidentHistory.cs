using ITIncidentSystem.Enums;

namespace ITIncidentSystem.Models;

/// <summary>
/// Модел: Запис в историята на инцидент.
/// Всяка промяна на статус създава нов HistoryEntry — одиторска следа.
/// Съответства на таблица IncidentHistory в базата данни.
/// </summary>
public class IncidentHistory
{
    public int    Id          { get; set; }
    public int    IncidentId  { get; set; }
    public string ChangedBy   { get; set; } = string.Empty;   // Пълно имена или "SYSTEM"

    public IncidentStatus? OldStatus  { get; set; }   // null при първи запис (Open)
    public IncidentStatus  NewStatus  { get; set; }

    public string?   Notes      { get; set; }
    public DateTime  ChangedAt  { get; set; } = DateTime.Now;

    // ── Конструктор ───────────────────────────────────────────────────────

    public IncidentHistory() { }

    public IncidentHistory(int incidentId, string changedBy,
                           IncidentStatus? oldStatus, IncidentStatus newStatus,
                           string? notes = null)
    {
        IncidentId = incidentId;
        ChangedBy  = changedBy;
        OldStatus  = oldStatus;
        NewStatus  = newStatus;
        Notes      = notes;
    }

    public override string ToString()
    {
        string old = OldStatus.HasValue ? OldStatus.Value.ToString() : "—";
        return $"[{ChangedAt:dd.MM.yyyy HH:mm}] {ChangedBy}: {old} → {NewStatus}" +
               (Notes != null ? $" | {Notes}" : "");
    }
}
