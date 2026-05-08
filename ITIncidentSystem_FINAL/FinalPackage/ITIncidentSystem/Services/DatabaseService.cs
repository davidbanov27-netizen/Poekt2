using ITIncidentSystem.Config;
using ITIncidentSystem.Enums;
using ITIncidentSystem.Exceptions;
using ITIncidentSystem.Models;
using MySql.Data.MySqlClient;

namespace ITIncidentSystem.Services;

/// <summary>
/// Клас за достъп до базата данни — всички SQL операции.
///
/// Принципи:
///   • Само параметризирани заявки — защита от SQL injection
///   • using блокове — автоматично затваряне на връзките
///   • DatabaseException обвива всяка MySqlException
///   • Транзакции при операции, засягащи повече от 1 таблица
/// </summary>
public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string? connectionString = null)
    {
        _connectionString = connectionString ?? DbConfig.ConnectionString;
    }

    // ── Помощен метод: отваряне на връзка ────────────────────────────────

    private MySqlConnection OpenConnection()
    {
        try
        {
            var conn = new MySqlConnection(_connectionString);
            conn.Open();
            return conn;
        }
        catch (MySqlException ex)
        {
            throw new DatabaseException("OpenConnection",
                $"Не може да се свърже с базата данни: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // INCIDENTS — CRUD
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Взима всички инциденти с JOIN към Users, Technicians, Departments.</summary>
    public List<Incident> GetAllIncidents()
    {
        const string sql = @"
            SELECT i.*, u.full_name AS user_name, u.email AS user_email,
                   t.full_name AS tech_name, d.name AS dept_name
            FROM   incidents i
            JOIN   users       u ON i.user_id       = u.user_id
            JOIN   departments d ON i.department_id  = d.department_id
            LEFT JOIN technicians t ON i.technician_id = t.technician_id
            ORDER  BY i.created_at DESC";

        return ExecuteQuery(sql, MapIncident);
    }

    /// <summary>Взима инцидент по ID. Хвърля IncidentNotFoundException ако не е намерен.</summary>
    public Incident GetIncidentById(int id)
    {
        const string sql = @"
            SELECT i.*, u.full_name AS user_name, u.email AS user_email,
                   t.full_name AS tech_name, d.name AS dept_name
            FROM   incidents i
            JOIN   users       u ON i.user_id       = u.user_id
            JOIN   departments d ON i.department_id  = d.department_id
            LEFT JOIN technicians t ON i.technician_id = t.technician_id
            WHERE  i.incident_id = @id";

        var results = ExecuteQuery(sql, MapIncident, ("@id", id));
        return results.FirstOrDefault()
               ?? throw new IncidentNotFoundException(id);
    }

    /// <summary>
    /// Вмъква нов инцидент в транзакция:
    ///   1. INSERT INTO incidents
    ///   2. INSERT INTO incident_history (Open)
    /// Връща новото ID.
    /// </summary>
    public int InsertIncident(Incident incident)
    {
        const string sqlInc = @"
            INSERT INTO incidents
                (title, description, category, priority, status,
                 user_id, technician_id, department_id, created_at)
            VALUES
                (@title, @desc, @cat, @pri, 'Open',
                 @uid, NULL, @did, @now);
            SELECT LAST_INSERT_ID();";

        const string sqlHist = @"
            INSERT INTO incident_history
                (incident_id, changed_by, old_status, new_status, notes, changed_at)
            VALUES (@iid, 'SYSTEM', NULL, 'Open', 'Инцидентът е регистриран', @now)";

        try
        {
            using var conn = OpenConnection();
            using var tx   = conn.BeginTransaction();
            try
            {
                // 1. Insert incident
                using var cmd = new MySqlCommand(sqlInc, conn, tx);
                cmd.Parameters.AddWithValue("@title", incident.Title);
                cmd.Parameters.AddWithValue("@desc",  incident.Description);
                cmd.Parameters.AddWithValue("@cat",   incident.Category.ToString());
                cmd.Parameters.AddWithValue("@pri",   incident.Priority.ToString());
                cmd.Parameters.AddWithValue("@uid",   incident.UserId);
                cmd.Parameters.AddWithValue("@did",   incident.DepartmentId);
                cmd.Parameters.AddWithValue("@now",   DateTime.Now);

                int newId = Convert.ToInt32(cmd.ExecuteScalar());
                incident.Id = newId;

                // 2. Insert history
                using var histCmd = new MySqlCommand(sqlHist, conn, tx);
                histCmd.Parameters.AddWithValue("@iid", newId);
                histCmd.Parameters.AddWithValue("@now", DateTime.Now);
                histCmd.ExecuteNonQuery();

                tx.Commit();
                return newId;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
        catch (MySqlException ex)
        {
            throw new DatabaseException("InsertIncident", ex);
        }
    }

    /// <summary>
    /// Назначава техник — UPDATE incidents + INSERT history в транзакция.
    /// </summary>
    public void AssignTechnicianDb(int incidentId, int technicianId, string assignedBy)
    {
        const string sqlUpd = @"
            UPDATE incidents
            SET    technician_id = @tid,
                   status        = 'InProgress',
                   assigned_at   = @now
            WHERE  incident_id = @iid
              AND  status      = 'Open'";

        const string sqlHist = @"
            INSERT INTO incident_history
                (incident_id, changed_by, old_status, new_status, notes, changed_at)
            VALUES (@iid, @by, 'Open', 'InProgress', @notes, @now)";

        const string sqlTech = @"
            UPDATE technicians
            SET active_cases = active_cases + 1,
                is_available = CASE WHEN active_cases + 1 >= 3 THEN 0 ELSE 1 END
            WHERE technician_id = @tid";

        ExecuteInTransaction(conn =>
        {
            ExecuteNonQuery(conn, sqlUpd,
                ("@tid", technicianId), ("@now", DateTime.Now), ("@iid", incidentId));

            ExecuteNonQuery(conn, sqlHist,
                ("@iid", incidentId), ("@by", assignedBy),
                ("@notes", $"Назначен техник (ID: {technicianId})"),
                ("@now", DateTime.Now));

            ExecuteNonQuery(conn, sqlTech, ("@tid", technicianId));
        }, "AssignTechnician");
    }

    /// <summary>
    /// Решава инцидент — UPDATE + history + освобождаване на техника.
    /// </summary>
    public void ResolveIncidentDb(int incidentId, string notes, string resolvedBy)
    {
        const string sqlUpd = @"
            UPDATE incidents
            SET    status           = 'Resolved',
                   resolved_at      = @now,
                   resolution_notes = @notes
            WHERE  incident_id = @iid
              AND  status      = 'InProgress'";

        const string sqlHist = @"
            INSERT INTO incident_history
                (incident_id, changed_by, old_status, new_status, notes, changed_at)
            VALUES (@iid, @by, 'InProgress', 'Resolved', @notes, @now)";

        const string sqlTech = @"
            UPDATE technicians t
            JOIN   incidents   i ON i.technician_id = t.technician_id
            SET    t.active_cases = GREATEST(0, t.active_cases - 1),
                   t.is_available = CASE WHEN t.active_cases - 1 < 3 THEN 1 ELSE 0 END
            WHERE  i.incident_id = @iid";

        ExecuteInTransaction(conn =>
        {
            ExecuteNonQuery(conn, sqlUpd,
                ("@now", DateTime.Now), ("@notes", notes), ("@iid", incidentId));

            ExecuteNonQuery(conn, sqlHist,
                ("@iid", incidentId), ("@by", resolvedBy),
                ("@notes", notes), ("@now", DateTime.Now));

            ExecuteNonQuery(conn, sqlTech, ("@iid", incidentId));
        }, "ResolveIncident");
    }

    /// <summary>Изтрива (отменя) инцидент — само ако е в статус Open.</summary>
    public void CancelIncident(int incidentId, string cancelledBy, string reason)
    {
        const string sqlUpd = @"
            UPDATE incidents
            SET status = 'Cancelled'
            WHERE incident_id = @iid AND status = 'Open'";

        const string sqlHist = @"
            INSERT INTO incident_history
                (incident_id, changed_by, old_status, new_status, notes, changed_at)
            VALUES (@iid, @by, 'Open', 'Cancelled', @reason, @now)";

        ExecuteInTransaction(conn =>
        {
            int rows = ExecuteNonQuery(conn, sqlUpd, ("@iid", incidentId));
            if (rows == 0)
                throw new InvalidIncidentException("Status",
                    "Може да се отмени само инцидент в статус Open.");

            ExecuteNonQuery(conn, sqlHist,
                ("@iid", incidentId), ("@by", cancelledBy),
                ("@reason", reason), ("@now", DateTime.Now));
        }, "CancelIncident");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ИСТОРИЯ
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Пълна история на инцидент, сортирана по дата.</summary>
    public List<IncidentHistory> GetHistory(int incidentId)
    {
        const string sql = @"
            SELECT * FROM incident_history
            WHERE  incident_id = @iid
            ORDER  BY changed_at ASC";

        return ExecuteQuery(sql, MapHistory, ("@iid", incidentId));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TECHNICIANS
    // ═══════════════════════════════════════════════════════════════════════

    public List<Technician> GetAllTechnicians()
    {
        const string sql = "SELECT * FROM technicians ORDER BY full_name";
        return ExecuteQuery(sql, MapTechnician);
    }

    public List<Technician> GetAvailableTechnicians()
    {
        const string sql = "SELECT * FROM technicians WHERE is_available = 1 ORDER BY active_cases ASC";
        return ExecuteQuery(sql, MapTechnician);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // USERS & DEPARTMENTS
    // ═══════════════════════════════════════════════════════════════════════

    public List<User> GetAllUsers()
    {
        const string sql = "SELECT * FROM users ORDER BY full_name";
        return ExecuteQuery(sql, MapUser);
    }

    public List<Department> GetAllDepartments()
    {
        const string sql = "SELECT * FROM departments ORDER BY name";
        return ExecuteQuery(sql, MapDepartment);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // СТАТИСТИЧЕСКИ ЗАЯВКИ (директно в SQL за производителност)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Брой инциденти по статус.</summary>
    public Dictionary<string, int> GetCountByStatus()
    {
        const string sql = @"
            SELECT status, COUNT(*) AS cnt
            FROM   incidents
            GROUP  BY status";

        var result = new Dictionary<string, int>();
        try
        {
            using var conn = OpenConnection();
            using var cmd  = new MySqlCommand(sql, conn);
            using var rdr  = cmd.ExecuteReader();
            while (rdr.Read())
                result[rdr.GetString("status")] = rdr.GetInt32("cnt");
        }
        catch (MySqlException ex) { throw new DatabaseException("GetCountByStatus", ex); }
        return result;
    }

    /// <summary>Топ N категории по брой инциденти.</summary>
    public List<(string Category, int Count)> GetTopCategories(int top = 3)
    {
        string sql = $@"
            SELECT category, COUNT(*) AS cnt
            FROM   incidents
            GROUP  BY category
            ORDER  BY cnt DESC
            LIMIT  {top}";

        var result = new List<(string, int)>();
        try
        {
            using var conn = OpenConnection();
            using var cmd  = new MySqlCommand(sql, conn);
            using var rdr  = cmd.ExecuteReader();
            while (rdr.Read())
                result.Add((rdr.GetString("category"), rdr.GetInt32("cnt")));
        }
        catch (MySqlException ex) { throw new DatabaseException("GetTopCategories", ex); }
        return result;
    }

    /// <summary>Средно времетраене за решаване в минути.</summary>
    public double GetAvgResolutionMinutes()
    {
        const string sql = @"
            SELECT AVG(TIMESTAMPDIFF(MINUTE, created_at, resolved_at)) AS avg_min
            FROM   incidents
            WHERE  status IN ('Resolved','Closed') AND resolved_at IS NOT NULL";

        try
        {
            using var conn = OpenConnection();
            using var cmd  = new MySqlCommand(sql, conn);
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? 0 : Convert.ToDouble(result);
        }
        catch (MySqlException ex) { throw new DatabaseException("GetAvgResolution", ex); }
    }

    /// <summary>Проверява дали връзката с DB работи.</summary>
    public bool TestConnection()
    {
        try
        {
            using var conn = OpenConnection();
            return conn.State == System.Data.ConnectionState.Open;
        }
        catch { return false; }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ЧАСТНИ ПОМОЩНИ МЕТОДИ
    // ═══════════════════════════════════════════════════════════════════════

    private List<T> ExecuteQuery<T>(
        string sql,
        Func<MySqlDataReader, T> mapper,
        params (string name, object value)[] parameters)
    {
        var results = new List<T>();
        try
        {
            using var conn = OpenConnection();
            using var cmd  = new MySqlCommand(sql, conn);
            foreach (var (name, value) in parameters)
                cmd.Parameters.AddWithValue(name, value);

            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
                results.Add(mapper(rdr));
        }
        catch (MySqlException ex)
        {
            throw new DatabaseException("ExecuteQuery", ex);
        }
        return results;
    }

    private int ExecuteNonQuery(
        MySqlConnection conn,
        string sql,
        params (string name, object value)[] parameters)
    {
        using var cmd = new MySqlCommand(sql, conn);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd.ExecuteNonQuery();
    }

    private void ExecuteInTransaction(Action<MySqlConnection> work, string operationName)
    {
        try
        {
            using var conn = OpenConnection();
            using var tx   = conn.BeginTransaction();
            try
            {
                work(conn);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
        catch (MySqlException ex)
        {
            throw new DatabaseException(operationName, ex);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MAPPER МЕТОДИ — MySqlDataReader → Model
    // ═══════════════════════════════════════════════════════════════════════

    private static Incident MapIncident(MySqlDataReader r)
    {
        var inc = new Incident
        {
            Id           = r.GetInt32("incident_id"),
            Title        = r.GetString("title"),
            Description  = r.GetString("description"),
            Category     = Enum.Parse<IncidentCategory>(r.GetString("category")),
            Priority     = Enum.Parse<IncidentPriority>(r.GetString("priority")),
            Status       = Enum.Parse<IncidentStatus>(r.GetString("status")),
            UserId       = r.GetInt32("user_id"),
            DepartmentId = r.GetInt32("department_id"),
            CreatedAt    = r.GetDateTime("created_at"),
        };

        // Nullable полета
        if (!r.IsDBNull(r.GetOrdinal("technician_id")))
            inc.TechnicianId = r.GetInt32("technician_id");

        if (!r.IsDBNull(r.GetOrdinal("assigned_at")))
            inc.AssignedAt = r.GetDateTime("assigned_at");

        if (!r.IsDBNull(r.GetOrdinal("resolved_at")))
            inc.ResolvedAt = r.GetDateTime("resolved_at");

        if (!r.IsDBNull(r.GetOrdinal("resolution_notes")))
            inc.ResolutionNotes = r.GetString("resolution_notes");

        // Навигационни свойства от JOIN-а
        try
        {
            inc.User = new User
            {
                Id       = inc.UserId,
                FullName = r.GetString("user_name"),
                Email    = r.GetString("user_email"),
            };

            if (inc.TechnicianId.HasValue)
                inc.Technician = new Technician
                {
                    Id       = inc.TechnicianId.Value,
                    FullName = r.GetString("tech_name"),
                };
        }
        catch { /* Навигационните свойства са опционални */ }

        return inc;
    }

    private static IncidentHistory MapHistory(MySqlDataReader r)
    {
        var h = new IncidentHistory
        {
            Id         = r.GetInt32("history_id"),
            IncidentId = r.GetInt32("incident_id"),
            ChangedBy  = r.GetString("changed_by"),
            NewStatus  = Enum.Parse<IncidentStatus>(r.GetString("new_status")),
            ChangedAt  = r.GetDateTime("changed_at"),
        };

        if (!r.IsDBNull(r.GetOrdinal("old_status")))
            h.OldStatus = Enum.Parse<IncidentStatus>(r.GetString("old_status"));

        if (!r.IsDBNull(r.GetOrdinal("notes")))
            h.Notes = r.GetString("notes");

        return h;
    }

    private static Technician MapTechnician(MySqlDataReader r) => new()
    {
        Id             = r.GetInt32("technician_id"),
        FullName       = r.GetString("full_name"),
        Email          = r.GetString("email"),
        Specialization = Enum.Parse<IncidentCategory>(r.GetString("specialization")),
        IsAvailable    = r.GetBoolean("is_available"),
        ActiveCases    = r.GetInt32("active_cases"),
    };

    private static User MapUser(MySqlDataReader r) => new()
    {
        Id           = r.GetInt32("user_id"),
        FullName     = r.GetString("full_name"),
        Email        = r.GetString("email"),
        Phone        = r.IsDBNull(r.GetOrdinal("phone")) ? "" : r.GetString("phone"),
        DepartmentId = r.GetInt32("department_id"),
    };

    private static Department MapDepartment(MySqlDataReader r) => new()
    {
        Id            = r.GetInt32("department_id"),
        Name          = r.GetString("name"),
        ManagerName   = r.GetString("manager_name"),
        EmployeeCount = r.GetInt32("employee_count"),
    };
}
