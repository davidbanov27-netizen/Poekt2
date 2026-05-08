using ITIncidentSystem.Delegates;
using ITIncidentSystem.Enums;
using ITIncidentSystem.Events;
using ITIncidentSystem.Exceptions;
using ITIncidentSystem.Models;
using ITIncidentSystem.Validators;

namespace ITIncidentSystem.Services;

/// <summary>
/// Централен клас за управление на инциденти — ядрото на системата.
///
/// Отговорности:
///   • Регистриране, назначаване и решаване на инциденти
///   • Изстрелване на 4-те събития
///   • Координиране на останалите услуги
///   • LINQ заявки за филтриране и статистика
///
/// Всички слушатели се абонират към събитията на ТОЗИ клас.
/// </summary>
public class IncidentManager
{
    // ═══════════════════════════════════════════════════════════════════════
    // СЪБИТИЯ — дефиниция
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Изстрелва се при успешно регистриране на нов инцидент.</summary>
    public event EventHandler<IncidentEventArgs>?      IncidentCreated;

    /// <summary>Изстрелва се при назначаване на техник към инцидент.</summary>
    public event EventHandler<IncidentEventArgs>?      IncidentAssigned;

    /// <summary>Изстрелва се при маркиране на инцидент като решен.</summary>
    public event EventHandler<IncidentEventArgs>?      IncidentResolved;

    /// <summary>Изстрелва се при регистриране на КРИТИЧЕН инцидент (допълнително).</summary>
    public event EventHandler<CriticalAlertEventArgs>? CriticalIncidentDetected;

    // ═══════════════════════════════════════════════════════════════════════
    // ДАННИ (Week 2 — в паметта; Week 3 — заменят се с DatabaseService)
    // ═══════════════════════════════════════════════════════════════════════

    private readonly List<Incident>   _incidents    = new();
    private readonly List<Technician> _technicians  = new();
    private readonly List<User>       _users        = new();
    private readonly List<Department> _departments  = new();

    private readonly InputValidator   _validator    = new();
    private int _nextId = 1;

    // ═══════════════════════════════════════════════════════════════════════
    // ИНИЦИАЛИЗАЦИЯ — зареждане на тестови данни
    // ═══════════════════════════════════════════════════════════════════════

    public void SeedTestData()
    {
        // Отдели
        _departments.AddRange(new[]
        {
            new Department(1, "Финансов отдел",  "Иван Петров",       24),
            new Department(2, "Отдел Продажби",  "Мария Колева",      31),
            new Department(3, "ИТ отдел",        "Стефан Димитров",   12),
            new Department(4, "HR отдел",        "Елена Иванова",      8),
            new Department(5, "Маркетинг",       "Николай Стоянов",   15),
        });

        // Техници
        _technicians.AddRange(new[]
        {
            new Technician(1, "Александър Георгиев", "a.georgiev@co.bg",  IncidentCategory.Hardware),
            new Technician(2, "Даниел Маринов",      "d.marinov@co.bg",   IncidentCategory.Software),
            new Technician(3, "Кристина Василева",   "k.vasileva@co.bg",  IncidentCategory.Network),
            new Technician(4, "Мартин Тодоров",      "m.todorov@co.bg",   IncidentCategory.Security),
            new Technician(5, "Симона Христова",     "s.hristova@co.bg",  IncidentCategory.Hardware),
        });

        // Потребители
        _users.AddRange(new[]
        {
            new User(1, "Петя Атанасова",   "p.atanasova@co.bg",  "0888-111-001", 1),
            new User(2, "Борис Недялков",   "b.nedyalkov@co.bg",  "0888-111-002", 2),
            new User(3, "Галина Стоева",    "g.stoeva@co.bg",     "0888-111-003", 3),
            new User(4, "Радослав Иванов",  "r.ivanov@co.bg",     "0888-111-004", 4),
            new User(5, "Теодора Любенова", "t.lyubenova@co.bg",  "0888-111-005", 5),
        });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ОСНОВНИ ОПЕРАЦИИ — Create, Assign, Resolve
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Регистрира нов инцидент.
    /// 1. Валидира входните данни (хвърля при грешка)
    /// 2. Записва инцидента
    /// 3. Изстрелва IncidentCreated (и CriticalIncidentDetected ако е Critical)
    /// </summary>
    public Incident CreateIncident(
        string title, string description,
        IncidentCategory category, IncidentPriority priority,
        int userId, int departmentId,
        List<string>? affectedSystems = null)
    {
        // 1. Намери свързаните обекти
        var user = GetUserOrThrow(userId);
        var dept = GetDepartmentOrThrow(departmentId);

        // 2. Създай обекта
        var incident = new Incident(_nextId++, title, description,
                                    category, priority, userId, departmentId)
        {
            User       = user,
            Department = dept
        };

        // 3. Валидирай
        _validator.ValidateIncident(incident);

        // 4. Запиши
        _incidents.Add(incident);
        incident.History.Add(new IncidentHistory(
            incident.Id, user.FullName, null, IncidentStatus.Open,
            "Инцидентът е регистриран"));

        Console.WriteLine($"\n  [Manager] Инцидент {incident.IncidentNumber} регистриран.");

        // 5. Изстрели IncidentCreated
        var args = new IncidentEventArgs(
            incident.Id, incident.Title, priority,
            IncidentStatus.Open, user.FullName, dept.Name);

        OnIncidentCreated(args);

        // 6. Ако е Critical → изстрели и CriticalIncidentDetected
        if (priority == IncidentPriority.Critical)
        {
            var critArgs = new CriticalAlertEventArgs(
                incident.Id, incident.Title, category,
                user.FullName, dept.Name, affectedSystems);
            OnCriticalDetected(critArgs);
        }

        return incident;
    }

    /// <summary>
    /// Назначава техник към инцидент.
    /// Проверява дали техникът е наличен. Изстрелва IncidentAssigned.
    /// </summary>
    public void AssignTechnician(int incidentId, int technicianId, string assignedBy = "Мениджър")
    {
        var incident   = GetIncidentOrThrow(incidentId);
        var technician = GetTechnicianOrThrow(technicianId);

        if (!technician.IsAvailable)
            throw new TechnicianUnavailableException(technicianId, technician.FullName);

        if (incident.Status != IncidentStatus.Open)
            throw new InvalidIncidentException("Status",
                $"Инцидент {incident.IncidentNumber} е в статус '{incident.Status}' и не може да се назначи.");

        // Запиши промяната
        var oldStatus = incident.Status;
        incident.TechnicianId  = technicianId;
        incident.Technician    = technician;
        incident.Status        = IncidentStatus.InProgress;
        incident.AssignedAt    = DateTime.Now;

        // Обнови техника
        technician.ActiveCases++;
        technician.UpdateAvailability();
        technician.AssignedIncidents.Add(incident);

        // Запиши в история
        incident.History.Add(new IncidentHistory(
            incident.Id, assignedBy, oldStatus, IncidentStatus.InProgress,
            $"Назначен техник: {technician.FullName}"));

        Console.WriteLine($"\n  [Manager] {incident.IncidentNumber} → назначен на {technician.FullName}.");

        // Изстрели събитието
        var dept = GetDepartmentOrThrow(incident.DepartmentId);
        var user = GetUserOrThrow(incident.UserId);

        var args = new IncidentEventArgs(
            incident.Id, incident.Title, incident.Priority,
            IncidentStatus.InProgress, user.FullName, dept.Name,
            oldStatus, technician.FullName);

        OnIncidentAssigned(args);
    }

    /// <summary>
    /// Маркира инцидент като решен. Изстрелва IncidentResolved.
    /// </summary>
    public void ResolveIncident(int incidentId, string resolutionNotes, string resolvedBy = "Техник")
    {
        var incident = GetIncidentOrThrow(incidentId);

        if (incident.Status != IncidentStatus.InProgress)
            throw new InvalidIncidentException("Status",
                $"Може да се реши само инцидент в статус 'InProgress' (текущ: {incident.Status}).");

        if (string.IsNullOrWhiteSpace(resolutionNotes))
            throw new InvalidIncidentException("ResolutionNotes",
                "Трябва да се въведат бележки при решаването.");

        // Запиши промяната
        var oldStatus = incident.Status;
        incident.Status          = IncidentStatus.Resolved;
        incident.ResolvedAt      = DateTime.Now;
        incident.ResolutionNotes = resolutionNotes;

        // Освободи техника
        if (incident.Technician != null)
        {
            incident.Technician.ActiveCases =
                Math.Max(0, incident.Technician.ActiveCases - 1);
            incident.Technician.UpdateAvailability();
        }

        // Запиши в история
        incident.History.Add(new IncidentHistory(
            incident.Id, resolvedBy, oldStatus, IncidentStatus.Resolved,
            resolutionNotes));

        Console.WriteLine($"\n  [Manager] {incident.IncidentNumber} → решен " +
                          $"(за {incident.ResolutionTimeMinutes:F0} мин.)");

        // Изстрели събитието
        var dept = GetDepartmentOrThrow(incident.DepartmentId);
        var user = GetUserOrThrow(incident.UserId);

        var args = new IncidentEventArgs(
            incident.Id, incident.Title, incident.Priority,
            IncidentStatus.Resolved, user.FullName, dept.Name,
            oldStatus, incident.Technician?.FullName,
            resolutionNotes, incident.ResolutionTimeMinutes);

        OnIncidentResolved(args);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // LINQ ЗАЯВКИ — филтриране, сортиране, статистика
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Всички инциденти.</summary>
    public IReadOnlyList<Incident> GetAll() => _incidents.AsReadOnly();

    /// <summary>Филтриране с делегат (позволява custom предикати).</summary>
    public List<Incident> GetFiltered(IncidentFilter filter)
        => _incidents.Where(i => filter(i)).ToList();

    /// <summary>Инциденти по статус.</summary>
    public List<Incident> GetByStatus(IncidentStatus status)
        => _incidents.Where(i => i.Status == status)
                     .OrderBy(i => i.Priority)
                     .ToList();

    /// <summary>Инциденти по техник.</summary>
    public List<Incident> GetByTechnician(int technicianId)
        => _incidents.Where(i => i.TechnicianId == technicianId)
                     .OrderByDescending(i => i.CreatedAt)
                     .ToList();

    /// <summary>Топ 3 най-чести категории.</summary>
    public List<(IncidentCategory Category, int Count)> GetTopCategories(int take = 3)
        => _incidents
            .GroupBy(i => i.Category)
            .OrderByDescending(g => g.Count())
            .Take(take)
            .Select(g => (g.Key, g.Count()))
            .ToList();

    /// <summary>Средно времетраене за решаване (минути).</summary>
    public double GetAverageResolutionMinutes()
    {
        var resolved = _incidents
            .Where(i => i.ResolutionTimeMinutes.HasValue)
            .ToList();

        return resolved.Count == 0
            ? 0
            : resolved.Average(i => i.ResolutionTimeMinutes!.Value);
    }

    /// <summary>Всички критични инциденти, сортирани по дата.</summary>
    public List<Incident> GetCriticalIncidents()
        => _incidents
            .Where(i => i.Priority == IncidentPriority.Critical)
            .OrderBy(i => i.CreatedAt)
            .ToList();

    /// <summary>Статистика по техник.</summary>
    public List<(string Name, int Total, int Resolved)> GetTechnicianStats()
        => _technicians.Select(t => (
                t.FullName,
                Total:    _incidents.Count(i => i.TechnicianId == t.Id),
                Resolved: _incidents.Count(i => i.TechnicianId == t.Id &&
                                               i.Status == IncidentStatus.Resolved)
            ))
            .Where(x => x.Total > 0)
            .OrderByDescending(x => x.Total)
            .ToList();

    // ═══════════════════════════════════════════════════════════════════════
    // PROTECTED — изстрелване на събитията
    // ═══════════════════════════════════════════════════════════════════════

    protected virtual void OnIncidentCreated(IncidentEventArgs e)
        => IncidentCreated?.Invoke(this, e);

    protected virtual void OnIncidentAssigned(IncidentEventArgs e)
        => IncidentAssigned?.Invoke(this, e);

    protected virtual void OnIncidentResolved(IncidentEventArgs e)
        => IncidentResolved?.Invoke(this, e);

    protected virtual void OnCriticalDetected(CriticalAlertEventArgs e)
        => CriticalIncidentDetected?.Invoke(this, e);

    // ═══════════════════════════════════════════════════════════════════════
    // ПОМОЩНИ — намиране на обект по ID
    // ═══════════════════════════════════════════════════════════════════════

    private Incident GetIncidentOrThrow(int id)
        => _incidents.FirstOrDefault(i => i.Id == id)
           ?? throw new IncidentNotFoundException(id);

    private Technician GetTechnicianOrThrow(int id)
        => _technicians.FirstOrDefault(t => t.Id == id)
           ?? throw new IncidentNotFoundException(id);

    private User GetUserOrThrow(int id)
        => _users.FirstOrDefault(u => u.Id == id)
           ?? throw new InvalidIncidentException("UserId",
               $"Потребител с ID {id} не съществува.");

    private Department GetDepartmentOrThrow(int id)
        => _departments.FirstOrDefault(d => d.Id == id)
           ?? throw new InvalidIncidentException("DepartmentId",
               $"Отдел с ID {id} не съществува.");

    // ─── Публичен достъп до списъците (read-only) ─────────────────────────
    public IReadOnlyList<Technician>  GetTechnicians()  => _technicians.AsReadOnly();
    public IReadOnlyList<User>        GetUsers()        => _users.AsReadOnly();
    public IReadOnlyList<Department>  GetDepartments()  => _departments.AsReadOnly();
}
