using ITIncidentSystem.Events;
using ITIncidentSystem.Models;

namespace ITIncidentSystem.Delegates;

// ═══════════════════════════════════════════════════════════════════════════
// ДЕЛЕГАТИ — IT Incident Management System
//
// Делегатът е тип, описващ "форма" на метод — параметри и върнат тип.
// Позволява методите да се подават като аргументи или да се съхраняват
// в променливи. Събитията в C# се базират на делегати.
// ═══════════════════════════════════════════════════════════════════════════


/// <summary>
/// Делегат за стандартни известявания за инциденти.
/// Използва EventHandler&lt;T&gt; pattern — стандарт в .NET.
/// Параметри: object sender = кой е изстрелял събитието,
///            IncidentEventArgs e = данните за инцидента.
/// </summary>
public delegate void IncidentEventHandler(object sender, IncidentEventArgs e);


/// <summary>
/// Делегат за аларми при критични инциденти.
/// Наследените EventArgs позволяват повече информация.
/// </summary>
public delegate void CriticalAlertHandler(object sender, CriticalAlertEventArgs e);


/// <summary>
/// Делегат за генериране на текстов отчет от инцидент.
/// Демонстрира делегат с върнат тип (string).
/// Използва се в StatisticsService и FileLogger за форматиране.
/// </summary>
public delegate string IncidentReportFormatter(Incident incident);


/// <summary>
/// Делегат за валидация на инцидент преди записване.
/// Връща true ако инцидентът е валиден, false ако не.
/// Позволява да се сменя логиката на валидация без промяна на кода.
/// </summary>
public delegate bool IncidentValidator(Incident incident, out string errorMessage);


/// <summary>
/// Делегат за филтриране — предикат върху инцидент.
/// Идентичен на Predicate&lt;Incident&gt;, но с по-описателно имe.
/// Използва се в IncidentManager.GetFiltered().
/// </summary>
public delegate bool IncidentFilter(Incident incident);


// ── Примерни готови делегатни инстанции (статични) ────────────────────────

/// <summary>
/// Готови делегати за честа употреба — могат да се подават директно
/// на GetFiltered() без да се пише lambda всеки път.
/// </summary>
public static class IncidentFilters
{
    /// <summary>Само критични инциденти.</summary>
    public static readonly IncidentFilter OnlyCritical
        = i => i.Priority == Enums.IncidentPriority.Critical;

    /// <summary>Само активни (не затворени/отменени) инциденти.</summary>
    public static readonly IncidentFilter OnlyActive
        = i => i.IsActive;

    /// <summary>Само нерешени инциденти (Open или InProgress).</summary>
    public static readonly IncidentFilter Unresolved
        = i => i.Status is Enums.IncidentStatus.Open
                        or Enums.IncidentStatus.InProgress;

    /// <summary>Инциденти без назначен техник.</summary>
    public static readonly IncidentFilter Unassigned
        = i => i.TechnicianId == null;
}
