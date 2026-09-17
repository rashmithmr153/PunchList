using Microsoft.Data.Sqlite;
using System.IO;
using Dapper;

namespace PunchList.Data
{
    public class DatabaseInitializer
    {
        public string DB_PATH;

        public DatabaseInitializer()
        {
            string appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PunchList");
            Directory.CreateDirectory(appFolder);
            DB_PATH = Path.Combine(appFolder, "punchlist.db");

            InitializeDatabase();
        }

        public void InitializeDatabase()
        {
            using var conn = new SqliteConnection($"Data Source={DB_PATH}");
            conn.Open();
            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Tasks (
                    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title          TEXT NOT NULL,
                    Notes          TEXT,
                    ProjectTag     TEXT,
                    CreatedAt      TEXT NOT NULL,
                    ReminderAt     TEXT NOT NULL,
                    Status         INTEGER NOT NULL,
                    CompletedAt    TEXT,
                    SnoozeCount    INTEGER NOT NULL DEFAULT 0,
                    LastSnoozedAt  TEXT,
                    LastNotifiedAt TEXT
            );
             ";
            conn.Execute(createTableQuery);
        }

    }
}
