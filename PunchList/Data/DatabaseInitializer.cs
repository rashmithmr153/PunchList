using Microsoft.Data.Sqlite;
using System.IO;
using System.Data;
using Dapper;

namespace PunchList.Data
{
    public class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
        {
            parameter.Value = value.ToString("o");
        }

        public override DateTimeOffset Parse(object value)
        {
            if (value is DateTimeOffset dto) return dto;
            if (value is string s) return DateTimeOffset.Parse(s);
            if (value is DateTime dt) return new DateTimeOffset(dt);
            return DateTimeOffset.Parse(value.ToString()!);
        }
    }

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
            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());

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
