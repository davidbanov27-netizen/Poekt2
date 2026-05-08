# 🖥️ IT Incident Management System

> Система за регистриране и проследяване на технически инциденти в ИТ отдел

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?style=flat-square&logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![MySQL](https://img.shields.io/badge/MySQL-8.0-4479A1?style=flat-square&logo=mysql&logoColor=white)](https://www.mysql.com/)
[![License](https://img.shields.io/badge/License-MIT-yellow?style=flat-square)](LICENSE)
[![Status](https://img.shields.io/badge/Status-Завършен-brightgreen?style=flat-square)]()

---

## 📋 Съдържание

- [За проекта](#-за-проекта)
- [Технологии](#-технологии)
- [Архитектура](#-архитектура)
- [Функционалност](#-функционалност)
- [Стартиране](#-стартиране)
- [База данни](#-база-данни)
- [Структура на проекта](#-структура-на-проекта)
- [Примерен изход](#-примерен-изход)
- [Автор](#-автор)

---

## 🎯 За проекта

**IT Incident Management System** е конзолно приложение на C# за регистриране, разпределяне и проследяване на технически проблеми в организация — компютри, софтуер, мрежа, сигурност.

Проектът е разработен като курсова работа по **ООП на C#** за 11 клас, специалност „Системно програмиране" и демонстрира:

| Концепция | Реализация |
|---|---|
| **ООП** | 17 класа, наследяване, инкапсулация, полиморфизъм |
| **Делегати** | 5 делегата — `IncidentFilter`, `IncidentReportFormatter`, `IncidentValidator` и др. |
| **Събития** | 4 events с `EventHandler<T>` pattern и 2+ слушателя всяко |
| **LINQ** | 18 метода — `Where`, `GroupBy`, `OrderBy`, `Average`, `Take`, `Select` |
| **Файлове** | `.log`, `.csv`, `.txt`, `.json` — запис и четене |
| **Изключения** | 6 custom exception класа в йерархия |
| **MySQL** | 5 таблици, транзакции, параметризирани заявки, VIEW-та |

---

## 🛠️ Технологии

- **C# 12** / **.NET 8.0**
- **MySQL 8.0** + `MySql.Data` драйвер
- **System.Text.Json** — JSON сериализация
- **Windows Forms** / **Console App**

---

## 🏗️ Архитектура

```
┌─────────────────────────────────────────────────────────────┐
│                      Program.cs (Демо)                      │
├──────────────┬──────────────────────────────────────────────┤
│   МОДЕЛИ     │  Incident · Technician · User                │
│              │  Department · IncidentHistory                 │
├──────────────┼──────────────────────────────────────────────┤
│   УСЛУГИ     │  IncidentManager  ← изстрелва 4 събития      │
│              │  DatabaseService  ← CRUD + транзакции        │
│              │  StatisticsService← 18 LINQ метода           │
│              │  FileLogger       ← .log / .csv / .txt       │
│              │  NotificationService ← Слушател #1           │
├──────────────┼──────────────────────────────────────────────┤
│   СЪБИТИЯ    │  IncidentCreated  → 2 слушателя              │
│              │  IncidentAssigned → 2 слушателя              │
│              │  IncidentResolved → 2 слушателя              │
│              │  CriticalDetected → 2 слушателя + АЛАРМА     │
├──────────────┼──────────────────────────────────────────────┤
│  ИЗКЛЮЧЕНИЯ  │  IncidentSystemException (базово)            │
│              │  ├── InvalidIncidentException                 │
│              │  ├── DatabaseException                        │
│              │  ├── FileLogException                         │
│              │  ├── TechnicianUnavailableException           │
│              │  └── IncidentNotFoundException                │
└──────────────┴──────────────────────────────────────────────┘
```

---

## ✨ Функционалност

### Управление на инциденти
- ✅ Регистриране на нов инцидент с категория, приоритет и описание
- ✅ Назначаване на техник (автоматична проверка за наличност)
- ✅ Решаване с бележки и автоматично изчисляване на времетраенето
- ✅ Пълна одиторска следа (история на всяка промяна)

### Категории и приоритети
```
Категории : Hardware | Software | Network | Peripheral | Security | Other
Приоритети: Low(1)  | Medium(2) | High(3) | Critical(4)
Статуси   : Open → InProgress → Resolved → Closed | Cancelled
```

### LINQ статистика
```csharp
// Топ 3 категории с проценти
incidents.GroupBy(i => i.Category)
         .OrderByDescending(g => g.Count())
         .Take(3)
         .Select(g => (g.Key, g.Count(), g.Count()*100.0/total))
         .ToList();

// SLA нарушения — активни > 2 дни
incidents.Where(i => i.IsActive && (DateTime.Now - i.CreatedAt).TotalDays > 2)
         .OrderBy(i => i.CreatedAt)
         .ToList();
```

### Event Chain при критичен инцидент
```
CreateIncident(priority: Critical)
  │
  ├── OnIncidentCreated()
  │     ├── NotificationService.OnCreated()  → зелено съобщение
  │     └── FileLogger.OnCreated()           → incident.log
  │
  └── OnCriticalDetected()
        ├── NotificationService.OnCritical() → ╔═══ АЛАРМА ═══╗
        └── FileLogger.OnCritical()          → critical.log
```

---

## 🚀 Стартиране

### Изисквания
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [MySQL 8.0](https://dev.mysql.com/downloads/) *(по избор — работи и без него)*

### Стъпка 1 — Клониране
```bash
git clone https://github.com/davidbanov27-netizen/Poekt2.git
cd Poekt2/ITIncidentSystem_FINAL/FinalPackage/ITIncidentSystem
```

### Стъпка 2 — MySQL (по избор)
```bash
# Импортирай схемата с 30 тестови инцидента
mysql -u root -p < Database/schema.sql
```

### Стъпка 3 — Конфигурация
Създай файл `Config/db.config`:
```ini
server=localhost
port=3306
database=it_incidents
username=root
password=ТВОЯТА_ПАРОЛА
```
> ⚠️ Без `db.config` системата автоматично зарежда данни от `Data/seed_data.json`

### Стъпка 4 — Стартиране
```bash
dotnet restore
dotnet run
```

---

## 🗄️ База данни

```sql
departments (6 записа)
    └── users (10 записа)
              └── incidents (30 записа)
                      ├── technicians (5 записа)
                      └── incident_history (17+ записа)
```

### Статистика на тестовите данни
| Показател | Стойност |
|---|---|
| Общо инциденти | 30 |
| Resolved | 20 (67%) |
| InProgress | 4 (13%) |
| Open | 6 (20%) |
| Critical | 6 |
| Средно времетраене | ~2 часа |
| Най-честа категория | Hardware (9) |

---

## 📁 Структура на проекта

```
ITIncidentSystem/
├── Config/
│   └── DbConfig.cs              # Connection string
├── Data/
│   ├── DataLoader.cs            # JSON сериализация
│   └── seed_data.json           # 30 тестови инцидента
├── Database/
│   └── schema.sql               # MySQL DDL + seed данни
├── Delegates/
│   └── IncidentDelegates.cs     # 5 делегата
├── Enums/
│   ├── IncidentCategory.cs
│   ├── IncidentPriority.cs
│   └── IncidentStatus.cs
├── Events/
│   ├── IncidentEventArgs.cs
│   └── CriticalAlertEventArgs.cs
├── Exceptions/
│   └── IncidentExceptions.cs    # 6 custom изключения
├── Models/
│   ├── Incident.cs
│   ├── Technician.cs
│   ├── User.cs
│   ├── Department.cs
│   └── IncidentHistory.cs
├── Services/
│   ├── IncidentManager.cs       # Бизнес логика + 4 събития
│   ├── DatabaseService.cs       # MySQL CRUD
│   ├── StatisticsService.cs     # 18 LINQ метода
│   ├── FileLogger.cs            # Логове и експорт
│   └── NotificationService.cs   # Слушател #1
├── Validators/
│   └── InputValidator.cs
├── Exports/                     # Генерира се при стартиране
│   ├── incidents_week3.csv
│   ├── critical_report.txt
│   ├── backup_incidents.json
│   └── statistics.json
├── Logs/                        # Генерира се при стартиране
│   ├── incident.log
│   └── critical.log
└── ITIncidentSystem.csproj
```

---

## 🖥️ Примерен изход

```
╔══════════════════════════════════════════════════════╗
║  IT Incident Management System — Week 3 Demo         ║
║  LINQ • Файлове • Изключения • MySQL                 ║
╚══════════════════════════════════════════════════════╝

┌─── 2. LINQ СПРАВКИ

  2a. Топ 3 най-чести типа проблеми:
      Категория       Брой  Процент
      ─────────────────────────────────
      Hardware           9    30.0%
      Software           8    26.7%
      Network            7    23.3%

  2c. Средно времетраене за решаване : 1.6 ч.

  2d. Всички критични инциденти:
      ✓ INC-00003 | Network      | Без интернет — целия 3-ти етаж
      ✓ INC-00005 | Security     | Подозрителни опити за вход
      ✓ INC-00010 | Security     | Ransomware подозрение
      ⚠ INC-00028 | Security     | Изтекъл SSL сертификат
```

---

## 📚 Документация

| Документ | Съдържание |
|---|---|
| `Week1_Анализ.docx` | Описание, UML, БД схема, тестови данни |
| `Week2_Класове.docx` | Модели, делегати, събития, слушатели |
| `Week3_LINQ_БД.docx` | LINQ заявки, файлова дейност, DatabaseService |
| `Week4_Финал.docx` | Ръководство, дневник, AI дневник, самооценка |
| `Presentation.pptx` | 16-слайдова финална презентация |

---

## 👤 Автор

**David Banov**
GitHub: [@davidbanov27-netizen](https://github.com/davidbanov27-netizen)

---

> *Проект по ООП на C# — 11 клас, специалност „Системно програмиране" · 2025*
