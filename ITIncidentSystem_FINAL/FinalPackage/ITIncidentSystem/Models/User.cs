namespace ITIncidentSystem.Models;

/// <summary>
/// Модел: Краен потребител (служител в организацията).
/// Потребителят подава инциденти. Един потребител може да подаде много инциденти (1:*).
/// </summary>
public class User
{
    // ── Свойства ──────────────────────────────────────────────────────────

    public int    Id           { get; set; }
    public string FullName     { get; set; } = string.Empty;
    public string Email        { get; set; } = string.Empty;
    public string Phone        { get; set; } = string.Empty;
    public int    DepartmentId { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.Now;

    // ── Навигационни свойства ─────────────────────────────────────────────

    /// <summary>Отделът, в който работи потребителят.</summary>
    public Department? Department { get; set; }

    /// <summary>Всички инциденти, подадени от потребителя.</summary>
    public List<Incident> Incidents { get; set; } = new();

    // ── Конструктори ──────────────────────────────────────────────────────

    public User() { }

    public User(int id, string fullName, string email, string phone, int departmentId)
    {
        Id           = id;
        FullName     = fullName;
        Email        = email;
        Phone        = phone;
        DepartmentId = departmentId;
    }

    // ── Методи ────────────────────────────────────────────────────────────

    /// <summary>Броят на активните инциденти, подадени от потребителя.</summary>
    public int ActiveIncidentCount
        => Incidents.Count(i => i.Status is Enums.IncidentStatus.Open
                                          or Enums.IncidentStatus.InProgress);

    public override string ToString()
        => $"[User #{Id}] {FullName} | {Email} | Dept: {DepartmentId}";
}
