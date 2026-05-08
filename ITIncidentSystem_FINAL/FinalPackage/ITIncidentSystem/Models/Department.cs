namespace ITIncidentSystem.Models;

/// <summary>
/// Модел: Отдел в организацията.
/// Един отдел има много потребители (1:*) и много инциденти (1:*).
/// </summary>
public class Department
{
    // ── Свойства ──────────────────────────────────────────────────────────

    public int    Id             { get; set; }
    public string Name           { get; set; } = string.Empty;
    public string ManagerName    { get; set; } = string.Empty;
    public int    EmployeeCount  { get; set; }
    public DateTime CreatedAt   { get; set; } = DateTime.Now;

    // ── Навигационни свойства (в паметта — за Week 2) ────────────────────

    /// <summary>Списък на служителите в отдела (зарежда се при нужда).</summary>
    public List<User> Users { get; set; } = new();

    // ── Конструктори ──────────────────────────────────────────────────────

    public Department() { }

    public Department(int id, string name, string managerName, int employeeCount)
    {
        Id            = id;
        Name          = name;
        ManagerName   = managerName;
        EmployeeCount = employeeCount;
    }

    // ── Методи ────────────────────────────────────────────────────────────

    /// <summary>Кратко представяне за конзолен изход / логове.</summary>
    public override string ToString()
        => $"[Dept #{Id}] {Name} | Мениджър: {ManagerName} | Служители: {EmployeeCount}";
}
