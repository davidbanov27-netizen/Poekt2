using System.Text.Json;
using System.Text.Json.Serialization;
using ITIncidentSystem.Enums;
using ITIncidentSystem.Exceptions;
using ITIncidentSystem.Models;

namespace ITIncidentSystem.Data;

/// <summary>
/// Зарежда и записва данни от/в JSON файлове.
///
/// Употреба:
///   • Начално зареждане на тестови данни (seed от JSON)
///   • Backup/restore на инциденти
///   • Обмен на данни с外部 системи
/// </summary>
public class DataLoader
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented          = true,
        PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
        Converters             = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ═══════════════════════════════════════════════════════════════════════
    // ЗАРЕЖДАНЕ ОТ JSON
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Зарежда инциденти от JSON файл.
    /// Хвърля FileLogException при липсващ/повреден файл.
    /// </summary>
    public List<IncidentDto> LoadIncidentsFromJson(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileLogException(filePath, "Файлът не съществува.");

        try
        {
            string json = File.ReadAllText(filePath);
            var dto = JsonSerializer.Deserialize<IncidentJsonRoot>(json, _options);

            if (dto?.Incidents == null || dto.Incidents.Count == 0)
                throw new FileLogException(filePath, "JSON файлът е празен или невалиден.");

            return dto.Incidents;
        }
        catch (JsonException ex)
        {
            throw new FileLogException(filePath,
                $"Невалиден JSON формат: {ex.Message}");
        }
        catch (IOException ex)
        {
            throw new FileLogException(filePath, ex);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ЗАПИС В JSON
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Записва списък с инциденти в JSON файл.</summary>
    public void ExportToJson(IEnumerable<Incident> incidents, string filePath)
    {
        try
        {
            var dtos = incidents.Select(i => new IncidentDto
            {
                Id               = i.Id,
                IncidentNumber   = i.IncidentNumber,
                Title            = i.Title,
                Description      = i.Description,
                Category         = i.Category.ToString(),
                Priority         = i.Priority.ToString(),
                Status           = i.Status.ToString(),
                UserId           = i.UserId,
                TechnicianId     = i.TechnicianId,
                DepartmentId     = i.DepartmentId,
                CreatedAt        = i.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                AssignedAt       = i.AssignedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                ResolvedAt       = i.ResolvedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                ResolutionNotes  = i.ResolutionNotes,
                ResolutionMinutes = i.ResolutionTimeMinutes,
            }).ToList();

            var root = new IncidentJsonRoot
            {
                ExportedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Count      = dtos.Count,
                Incidents  = dtos,
            };

            string json = JsonSerializer.Serialize(root, _options);
            File.WriteAllText(filePath, json);

            Console.WriteLine($"  [DataLoader] JSON записан: {filePath} ({dtos.Count} записа)");
        }
        catch (IOException ex)
        {
            throw new FileLogException(filePath, ex);
        }
    }

    /// <summary>Записва статистика в JSON.</summary>
    public void ExportStatsToJson(object stats, string filePath)
    {
        try
        {
            string json = JsonSerializer.Serialize(stats, _options);
            File.WriteAllText(filePath, json);
            Console.WriteLine($"  [DataLoader] Статистика записана: {filePath}");
        }
        catch (IOException ex)
        {
            throw new FileLogException(filePath, ex);
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// DTO КЛАСОВЕ ЗА JSON СЕРИАЛИЗАЦИЯ
// ═══════════════════════════════════════════════════════════════════════════

public class IncidentJsonRoot
{
    [JsonPropertyName("exportedAt")]
    public string ExportedAt { get; set; } = "";

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("incidents")]
    public List<IncidentDto> Incidents { get; set; } = new();
}

public class IncidentDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("incidentNumber")]
    public string IncidentNumber { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    [JsonPropertyName("technicianId")]
    public int? TechnicianId { get; set; }

    [JsonPropertyName("departmentId")]
    public int DepartmentId { get; set; }

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = "";

    [JsonPropertyName("assignedAt")]
    public string? AssignedAt { get; set; }

    [JsonPropertyName("resolvedAt")]
    public string? ResolvedAt { get; set; }

    [JsonPropertyName("resolutionNotes")]
    public string? ResolutionNotes { get; set; }

    [JsonPropertyName("resolutionMinutes")]
    public double? ResolutionMinutes { get; set; }
}
