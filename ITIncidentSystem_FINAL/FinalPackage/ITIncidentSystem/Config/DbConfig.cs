namespace ITIncidentSystem.Config;

/// <summary>
/// Конфигурация на базата данни.
/// В реален проект се зарежда от appsettings.json или environment variables.
/// За учебния проект стойностите се задават директно тук.
/// </summary>
public static class DbConfig
{
    // ── Настройки за връзка ───────────────────────────────────────────────

    public static string Server   { get; set; } = "localhost";
    public static string Port     { get; set; } = "3306";
    public static string Database { get; set; } = "it_incidents";
    public static string Username { get; set; } = "root";
    public static string Password { get; set; } = "your_password_here";

    /// <summary>
    /// Съставя connection string за MySQL.
    /// CharSet=utf8mb4 е задължително за кирилица.
    /// </summary>
    public static string ConnectionString =>
        $"Server={Server};Port={Port};Database={Database};" +
        $"Uid={Username};Pwd={Password};" +
        $"CharSet=utf8mb4;Connection Timeout=30;";

    /// <summary>Зарежда настройките от прост конфигурационен файл (key=value).</summary>
    public static void LoadFromFile(string path = "db.config")
    {
        if (!File.Exists(path)) return;

        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
            var parts = line.Split('=', 2);
            if (parts.Length != 2) continue;

            switch (parts[0].Trim().ToLower())
            {
                case "server":   Server   = parts[1].Trim(); break;
                case "port":     Port     = parts[1].Trim(); break;
                case "database": Database = parts[1].Trim(); break;
                case "username": Username = parts[1].Trim(); break;
                case "password": Password = parts[1].Trim(); break;
            }
        }
    }
}
