using ITIncidentSystem.Enums;
using ITIncidentSystem.Exceptions;
using ITIncidentSystem.Models;

namespace ITIncidentSystem.Validators;

/// <summary>
/// Валидатор на входни данни — проверява всички полета преди запис.
/// Хвърля InvalidIncidentException при невалидни данни.
/// Използва се от IncidentManager преди всяка операция Create/Update.
/// </summary>
public class InputValidator
{
    // ── Константи ─────────────────────────────────────────────────────────

    private const int MinTitleLength = 5;
    private const int MaxTitleLength = 200;
    private const int MaxDescLength  = 2000;

    // ── Валидация на инцидент ─────────────────────────────────────────────

    /// <summary>
    /// Валидира целия инцидент. Хвърля InvalidIncidentException при първата грешка.
    /// </summary>
    public void ValidateIncident(Incident incident)
    {
        ValidateTitle(incident.Title);
        ValidateDescription(incident.Description);
        ValidateUserId(incident.UserId);
        ValidateDepartmentId(incident.DepartmentId);
        ValidatePriority(incident.Priority);
        ValidateCategory(incident.Category);
    }

    /// <summary>Валидира само заглавието.</summary>
    public void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidIncidentException("Title",
                "Заглавието не може да е празно.");

        if (title.Trim().Length < MinTitleLength)
            throw new InvalidIncidentException("Title",
                $"Заглавието трябва да е поне {MinTitleLength} символа.");

        if (title.Length > MaxTitleLength)
            throw new InvalidIncidentException("Title",
                $"Заглавието не може да надвишава {MaxTitleLength} символа.");
    }

    /// <summary>Валидира описанието.</summary>
    public void ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidIncidentException("Description",
                "Описанието не може да е празно.");

        if (description.Length > MaxDescLength)
            throw new InvalidIncidentException("Description",
                $"Описанието не може да надвишава {MaxDescLength} символа.");
    }

    /// <summary>Валидира ID на потребителя.</summary>
    public void ValidateUserId(int userId)
    {
        if (userId <= 0)
            throw new InvalidIncidentException("UserId",
                "Трябва да се избере валиден потребител (ID > 0).");
    }

    /// <summary>Валидира ID на отдела.</summary>
    public void ValidateDepartmentId(int departmentId)
    {
        if (departmentId <= 0)
            throw new InvalidIncidentException("DepartmentId",
                "Трябва да се избере валиден отдел (ID > 0).");
    }

    /// <summary>Валидира приоритета — трябва да е дефиниран в enum-а.</summary>
    public void ValidatePriority(IncidentPriority priority)
    {
        if (!Enum.IsDefined(typeof(IncidentPriority), priority))
            throw new InvalidIncidentException("Priority",
                $"Невалиден приоритет: {priority}.");
    }

    /// <summary>Валидира категорията — трябва да е дефинирана в enum-а.</summary>
    public void ValidateCategory(IncidentCategory category)
    {
        if (!Enum.IsDefined(typeof(IncidentCategory), category))
            throw new InvalidIncidentException("Category",
                $"Невалидна категория: {category}.");
    }

    // ── Валидация на потребител ───────────────────────────────────────────

    /// <summary>Валидира имейл адрес (опростена проверка).</summary>
    public void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidIncidentException("Email", "Имейлът не може да е празен.");

        if (!email.Contains('@') || !email.Contains('.'))
            throw new InvalidIncidentException("Email",
                $"'{email}' не е валиден имейл адрес.");
    }

    /// <summary>Валидира пълно имена (поне 2 думи).</summary>
    public void ValidateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new InvalidIncidentException("FullName", "Името не може да е празно.");

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            throw new InvalidIncidentException("FullName",
                "Въведете пълно имена (поне 2 думи).");
    }

    // ── Делегатна реализация (за демонстрация) ────────────────────────────

    /// <summary>
    /// Връща делегат от тип IncidentValidator — за предаване като параметър.
    /// Демонстрира използване на делегат с out параметър.
    /// </summary>
    public Delegates.IncidentValidator GetValidatorDelegate()
    {
        return (Incident incident, out string errorMessage) =>
        {
            errorMessage = string.Empty;
            try
            {
                ValidateIncident(incident);
                return true;
            }
            catch (InvalidIncidentException ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        };
    }
}
