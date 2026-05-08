using ITIncidentSystem.Enums;
using ITIncidentSystem.Models;

namespace ITIncidentSystem.Services;

/// <summary>
/// Услуга за статистика — всички LINQ заявки върху зареден списък инциденти.
///
/// Демонстрира: Where, OrderBy, GroupBy, Select, Take, Average, Max, Count, Any.
/// Работи с IEnumerable&lt;Incident&gt; — независима от DatabaseService.
/// В Week 3 се комбинира с DatabaseService: данните идват от MySQL.
/// </summary>
public class StatisticsService
{
    // ═══════════════════════════════════════════════════════════════════════
    // 1. ФИЛТРИРАНЕ — Where
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Всички инциденти по даден статус, сортирани по приоритет (най-спешни първи).</summary>
    public List<Incident> FilterByStatus(IEnumerable<Incident> incidents, IncidentStatus status)
        => incidents
            .Where(i => i.Status == status)
            .OrderByDescending(i => i.Priority)   // Critical=4 > High=3 > Medium=2 > Low=1
            .ThenBy(i => i.CreatedAt)              // по-стари преди по-нови
            .ToList();

    /// <summary>Инциденти по категория.</summary>
    public List<Incident> FilterByCategory(IEnumerable<Incident> incidents, IncidentCategory cat)
        => incidents
            .Where(i => i.Category == cat)
            .OrderByDescending(i => i.CreatedAt)
            .ToList();

    /// <summary>Инциденти на конкретен техник.</summary>
    public List<Incident> FilterByTechnician(IEnumerable<Incident> incidents, int technicianId)
        => incidents
            .Where(i => i.TechnicianId == technicianId)
            .OrderByDescending(i => i.CreatedAt)
            .ToList();

    /// <summary>Инциденти на конкретен отдел.</summary>
    public List<Incident> FilterByDepartment(IEnumerable<Incident> incidents, int departmentId)
        => incidents
            .Where(i => i.DepartmentId == departmentId)
            .OrderByDescending(i => i.Priority)
            .ToList();

    /// <summary>Активни (не решени) инциденти — за текущия работен ден.</summary>
    public List<Incident> GetActiveIncidents(IEnumerable<Incident> incidents)
        => incidents
            .Where(i => i.Status is IncidentStatus.Open or IncidentStatus.InProgress)
            .OrderByDescending(i => i.Priority)
            .ThenBy(i => i.CreatedAt)
            .ToList();

    /// <summary>Критични инциденти, сортирани по дата на регистриране.</summary>
    public List<Incident> GetCriticalIncidents(IEnumerable<Incident> incidents)
        => incidents
            .Where(i => i.Priority == IncidentPriority.Critical)
            .OrderBy(i => i.CreatedAt)
            .ToList();

    /// <summary>Нерешени инциденти, по-стари от N дни — нарушен SLA.</summary>
    public List<Incident> GetSlaBreaches(IEnumerable<Incident> incidents, int maxDays = 2)
        => incidents
            .Where(i => i.IsActive &&
                        (DateTime.Now - i.CreatedAt).TotalDays > maxDays)
            .OrderBy(i => i.CreatedAt)    // най-старите първи
            .ToList();

    // ═══════════════════════════════════════════════════════════════════════
    // 2. СОРТИРАНЕ — OrderBy / ThenBy
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Всички инциденти сортирани по приоритет (Critical първи).</summary>
    public List<Incident> SortByPriority(IEnumerable<Incident> incidents)
        => incidents
            .OrderByDescending(i => i.Priority)
            .ThenByDescending(i => i.CreatedAt)
            .ToList();

    /// <summary>Решените инциденти сортирани по времетраене (най-бързите първи).</summary>
    public List<Incident> SortByResolutionTime(IEnumerable<Incident> incidents)
        => incidents
            .Where(i => i.ResolutionTimeMinutes.HasValue)
            .OrderBy(i => i.ResolutionTimeMinutes!.Value)
            .ToList();

    // ═══════════════════════════════════════════════════════════════════════
    // 3. ГРУПИРАНЕ — GroupBy
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Брой инциденти по категория (всички категории).</summary>
    public Dictionary<IncidentCategory, int> GroupByCategory(IEnumerable<Incident> incidents)
        => incidents
            .GroupBy(i => i.Category)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());

    /// <summary>Брой инциденти по статус.</summary>
    public Dictionary<IncidentStatus, int> GroupByStatus(IEnumerable<Incident> incidents)
        => incidents
            .GroupBy(i => i.Status)
            .ToDictionary(g => g.Key, g => g.Count());

    /// <summary>Брой инциденти по отдел.</summary>
    public Dictionary<int, int> GroupByDepartment(IEnumerable<Incident> incidents)
        => incidents
            .GroupBy(i => i.DepartmentId)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());

    /// <summary>Инциденти групирани по месец и година.</summary>
    public Dictionary<string, int> GroupByMonth(IEnumerable<Incident> incidents)
        => incidents
            .GroupBy(i => i.CreatedAt.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

    // ═══════════════════════════════════════════════════════════════════════
    // 4. ТЪРСЕНЕ — FirstOrDefault, Any, Contains
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Търсене по текст в заглавие или описание.</summary>
    public List<Incident> SearchByText(IEnumerable<Incident> incidents, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return incidents.ToList();

        string kw = keyword.ToLower();
        return incidents
            .Where(i => i.Title.ToLower().Contains(kw)
                     || i.Description.ToLower().Contains(kw))
            .OrderByDescending(i => i.CreatedAt)
            .ToList();
    }

    /// <summary>Дали има активни критични инциденти (незабавна проверка).</summary>
    public bool HasActiveCritical(IEnumerable<Incident> incidents)
        => incidents.Any(i => i.IsCritical && i.IsActive);

    // ═══════════════════════════════════════════════════════════════════════
    // 5. СТАТИСТИКА — Average, Max, Min, Count, Sum
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Топ N категории по брой инциденти.</summary>
    public List<(IncidentCategory Category, int Count, double Percent)>
        GetTopCategories(IEnumerable<Incident> incidents, int top = 3)
    {
        var list      = incidents.ToList();
        int total     = list.Count;
        if (total == 0) return new();

        return list
            .GroupBy(i => i.Category)
            .OrderByDescending(g => g.Count())
            .Take(top)
            .Select(g => (
                g.Key,
                g.Count(),
                Math.Round((double)g.Count() / total * 100, 1)
            ))
            .ToList();
    }

    /// <summary>Средно времетраене за решаване в минути.</summary>
    public double GetAverageResolutionMinutes(IEnumerable<Incident> incidents)
    {
        var resolved = incidents
            .Where(i => i.ResolutionTimeMinutes.HasValue)
            .ToList();

        return resolved.Count == 0
            ? 0
            : resolved.Average(i => i.ResolutionTimeMinutes!.Value);
    }

    /// <summary>Максимално времетраене за решаване.</summary>
    public double GetMaxResolutionMinutes(IEnumerable<Incident> incidents)
    {
        var resolved = incidents
            .Where(i => i.ResolutionTimeMinutes.HasValue)
            .ToList();

        return resolved.Count == 0
            ? 0
            : resolved.Max(i => i.ResolutionTimeMinutes!.Value);
    }

    /// <summary>
    /// Пълна статистика по техник:
    /// (Имена, Общо инциденти, Решени, Средно времетраене)
    /// </summary>
    public List<TechnicianStat> GetTechnicianStats(
        IEnumerable<Incident> incidents,
        IEnumerable<Technician> technicians)
    {
        var incList = incidents.ToList();

        return technicians
            .Select(t =>
            {
                var techIncs = incList.Where(i => i.TechnicianId == t.Id).ToList();
                var resolved = techIncs.Where(i => i.ResolutionTimeMinutes.HasValue).ToList();

                return new TechnicianStat
                {
                    TechnicianId    = t.Id,
                    FullName        = t.FullName,
                    Specialization  = t.Specialization.ToString(),
                    TotalAssigned   = techIncs.Count,
                    ResolvedCount   = techIncs.Count(i => i.Status == IncidentStatus.Resolved),
                    AvgResolutionMin = resolved.Count > 0
                        ? resolved.Average(i => i.ResolutionTimeMinutes!.Value)
                        : 0,
                };
            })
            .Where(s => s.TotalAssigned > 0)
            .OrderByDescending(s => s.ResolvedCount)
            .ToList();
    }

    /// <summary>Обобщена статистика за целия период.</summary>
    public SummaryReport GenerateSummary(IEnumerable<Incident> incidents)
    {
        var list = incidents.ToList();
        var resolved = list.Where(i => i.ResolutionTimeMinutes.HasValue).ToList();

        return new SummaryReport
        {
            Total               = list.Count,
            Open                = list.Count(i => i.Status == IncidentStatus.Open),
            InProgress          = list.Count(i => i.Status == IncidentStatus.InProgress),
            Resolved            = list.Count(i => i.Status == IncidentStatus.Resolved),
            Closed              = list.Count(i => i.Status == IncidentStatus.Closed),
            Critical            = list.Count(i => i.Priority == IncidentPriority.Critical),
            AvgResolutionMinutes = resolved.Count > 0
                ? resolved.Average(i => i.ResolutionTimeMinutes!.Value)
                : 0,
            MaxResolutionMinutes = resolved.Count > 0
                ? resolved.Max(i => i.ResolutionTimeMinutes!.Value)
                : 0,
            TopCategory         = list.GroupBy(i => i.Category)
                                      .OrderByDescending(g => g.Count())
                                      .FirstOrDefault()?.Key.ToString() ?? "—",
            SlaBreaches         = list.Count(i => i.IsActive &&
                                                  (DateTime.Now - i.CreatedAt).TotalDays > 2),
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ФОРМАТИРАНЕ НА ОТЧЕТИ
    // ═══════════════════════════════════════════════════════════════════════

    public void PrintSummary(SummaryReport r)
    {
        Console.WriteLine("\n  ┌──────────────────────────────────────────────────┐");
        Console.WriteLine($"  │  ОБОБЩЕНА СТАТИСТИКА                              │");
        Console.WriteLine("  ├──────────────────────────────────────────────────┤");
        Console.WriteLine($"  │  Общо инциденти    : {r.Total,-5}                         │");
        Console.WriteLine($"  │  Open              : {r.Open,-5}                         │");
        Console.WriteLine($"  │  InProgress        : {r.InProgress,-5}                         │");
        Console.WriteLine($"  │  Resolved          : {r.Resolved,-5}                         │");
        Console.WriteLine($"  │  Критични          : {r.Critical,-5}                         │");
        Console.WriteLine($"  │  SLA нарушения     : {r.SlaBreaches,-5}                         │");
        Console.WriteLine("  ├──────────────────────────────────────────────────┤");
        string avgStr = r.AvgResolutionMinutes >= 60
            ? $"{r.AvgResolutionMinutes / 60:F1} ч."
            : $"{r.AvgResolutionMinutes:F0} мин.";
        Console.WriteLine($"  │  Средно решаване   : {avgStr,-10}                    │");
        Console.WriteLine($"  │  Най-чест тип      : {r.TopCategory,-15}               │");
        Console.WriteLine("  └──────────────────────────────────────────────────┘");
    }
}

// ── DTO класове за резултатите ────────────────────────────────────────────

public class TechnicianStat
{
    public int    TechnicianId     { get; set; }
    public string FullName         { get; set; } = "";
    public string Specialization   { get; set; } = "";
    public int    TotalAssigned    { get; set; }
    public int    ResolvedCount    { get; set; }
    public double AvgResolutionMin { get; set; }

    public double ResolutionRate =>
        TotalAssigned > 0 ? (double)ResolvedCount / TotalAssigned * 100 : 0;
}

public class SummaryReport
{
    public int    Total                { get; set; }
    public int    Open                 { get; set; }
    public int    InProgress           { get; set; }
    public int    Resolved             { get; set; }
    public int    Closed               { get; set; }
    public int    Critical             { get; set; }
    public int    SlaBreaches          { get; set; }
    public double AvgResolutionMinutes { get; set; }
    public double MaxResolutionMinutes { get; set; }
    public string TopCategory          { get; set; } = "";
}
