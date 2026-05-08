namespace ITIncidentSystem.Enums;

/// <summary>
/// Приоритет на инцидента — определя спешността на реакцията.
/// При Priority == Critical се изстрелва допълнителното събитие CriticalIncidentDetected.
/// </summary>
public enum IncidentPriority
{
    Low      = 1,  // Ниски — решава се в рамките на седмицата
    Medium   = 2,  // Средни — решава се в рамките на 2 работни дни
    High     = 3,  // Високи — решава се до края на работния ден
    Critical = 4   // Критични — незабавна реакция, ескалация до мениджмънт
}
