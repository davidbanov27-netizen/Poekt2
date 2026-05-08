using ITIncidentSystem.Enums;

namespace ITIncidentSystem.Models;

/// <summary>
/// Модел: Техник в ИТ отдела.
/// Техникът получава инциденти и ги решава. Един техник може да работи по много инциденти.
/// </summary>
public class Technician
{
    // ── Свойства ──────────────────────────────────────────────────────────

    public int              Id             { get; set; }
    public string           FullName       { get; set; } = string.Empty;
    public string           Email          { get; set; } = string.Empty;

    /// <summary>Специализация — определя по какви проблеми работи техникът.</summary>
    public IncidentCategory Specialization { get; set; }

    /// <summary>Свободен ли е техникът за нов инцидент?</summary>
    public bool             IsAvailable    { get; set; } = true;

    /// <summary>Брой активни инциденти (автоматично се обновява от IncidentManager).</summary>
    public int              ActiveCases    { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // ── Навигационни свойства ─────────────────────────────────────────────

    /// <summary>Инциденти, назначени на техника.</summary>
    public List<Incident> AssignedIncidents { get; set; } = new();

    // ── Конструктори ──────────────────────────────────────────────────────

    public Technician() { }

    public Technician(int id, string fullName, string email, IncidentCategory specialization)
    {
        Id             = id;
        FullName       = fullName;
        Email          = email;
        Specialization = specialization;
    }

    // ── Методи ────────────────────────────────────────────────────────────

    /// <summary>
    /// Техникът се счита за зает при повече от 3 активни инцидента.
    /// Правилото може да се настрои при нужда.
    /// </summary>
    public void UpdateAvailability()
        => IsAvailable = ActiveCases < 3;

    public override string ToString()
        => $"[Tech #{Id}] {FullName} | {Specialization} | " +
           $"{(IsAvailable ? "СВОБОДЕН" : "ЗАЕТ")} | Активни: {ActiveCases}";
}
