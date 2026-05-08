namespace ITIncidentSystem.Exceptions;

// ═══════════════════════════════════════════════════════════════════════════
// ПЕРСОНАЛИЗИРАНИ ИЗКЛЮЧЕНИЯ — IT Incident Management System
//
// Собствените изключения позволяват точна обработка на грешки:
//   catch (InvalidIncidentException ex) — само валидационни грешки
//   catch (DatabaseException ex)        — само DB грешки
//   catch (FileLogException ex)         — само файлови грешки
// ═══════════════════════════════════════════════════════════════════════════


/// <summary>
/// Базово изключение за системата — от него наследяват всички останали.
/// Позволява catch (IncidentSystemException) да хване всяка системна грешка.
/// </summary>
public class IncidentSystemException : Exception
{
    public DateTime OccurredAt { get; } = DateTime.Now;

    public IncidentSystemException(string message)
        : base(message) { }

    public IncidentSystemException(string message, Exception innerException)
        : base(message, innerException) { }
}


/// <summary>
/// Хвърля се при невалидни входни данни за инцидент.
/// Например: празно заглавие, несъществуващ потребител.
/// </summary>
public class InvalidIncidentException : IncidentSystemException
{
    /// <summary>Полето, което е невалидно (напр. "Title", "UserId").</summary>
    public string FieldName { get; }

    public InvalidIncidentException(string fieldName, string message)
        : base($"Невалидно поле '{fieldName}': {message}")
    {
        FieldName = fieldName;
    }
}


/// <summary>
/// Хвърля се при проблем с базата данни.
/// Обвива SqlException, за да не изтича DB детайли към UI.
/// </summary>
public class DatabaseException : IncidentSystemException
{
    /// <summary>SQL операцията, при която е настъпила грешката.</summary>
    public string Operation { get; }

    public DatabaseException(string operation, string message)
        : base($"Грешка при операция '{operation}': {message}")
    {
        Operation = operation;
    }

    public DatabaseException(string operation, Exception innerException)
        : base($"Грешка при операция '{operation}': {innerException.Message}", innerException)
    {
        Operation = operation;
    }
}


/// <summary>
/// Хвърля се при проблем с файловата система — четене, запис, лог.
/// </summary>
public class FileLogException : IncidentSystemException
{
    public string FilePath { get; }

    public FileLogException(string filePath, string message)
        : base($"Файлова грешка при '{filePath}': {message}")
    {
        FilePath = filePath;
    }

    public FileLogException(string filePath, Exception innerException)
        : base($"Файлова грешка при '{filePath}': {innerException.Message}", innerException)
    {
        FilePath = filePath;
    }
}


/// <summary>
/// Хвърля се при опит за назначаване на зает техник.
/// </summary>
public class TechnicianUnavailableException : IncidentSystemException
{
    public int TechnicianId { get; }

    public TechnicianUnavailableException(int technicianId, string technicianName)
        : base($"Техникът '{technicianName}' (ID: {technicianId}) не е наличен.")
    {
        TechnicianId = technicianId;
    }
}


/// <summary>
/// Хвърля се при опит за намиране на несъществуващ инцидент.
/// </summary>
public class IncidentNotFoundException : IncidentSystemException
{
    public int IncidentId { get; }

    public IncidentNotFoundException(int incidentId)
        : base($"Инцидент с ID {incidentId} (INC-{incidentId:D5}) не е намерен.")
    {
        IncidentId = incidentId;
    }
}
