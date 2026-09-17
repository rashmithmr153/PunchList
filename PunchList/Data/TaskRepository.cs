using Dapper;
using Microsoft.Data.Sqlite;
using PunchList.Models;



namespace PunchList.Data
{
    public class TaskRepository
    {
        private readonly string _dbPath;

        public TaskRepository(DatabaseInitializer dbInit)
        {
            _dbPath = dbInit.DB_PATH;
        }


        public async Task<long> InsertAsync(Models.TaskItem task)
        {
            
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            //return await conn.QueryAsync<TaskItem>(sql, parameters);

            string insertQuery = @"
                INSERT INTO Tasks (Title, Notes, ProjectTag, CreatedAt, ReminderAt, Status, CompletedAt, SnoozeCount, LastSnoozedAt, LastNotifiedAt)
                VALUES (@Title, @Notes, @ProjectTag, @CreatedAt, @ReminderAt, @Status, @CompletedAt, @SnoozeCount, @LastSnoozedAt, @LastNotifiedAt);
                SELECT last_insert_rowid();
                ";

            return await conn.ExecuteScalarAsync<long>(insertQuery, task);
        }

        public async Task<IEnumerable<Models.TaskItem>> GetActiveAsync()
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");

            string selectQuery = @"
               SELECT * FROM Tasks 
                WHERE Status != @CompletedStatus 
                ORDER BY Status DESC, ReminderAt ASC;
            ";
            return await conn.QueryAsync<Models.TaskItem>(selectQuery, new { CompletedStatus = (int)Models.TaskStatus.Completed });
        }
        public async Task<IEnumerable<Models.TaskItem>> GetHistoryAsync(DateTimeOffset from, DateTimeOffset to)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            string sql = @"
                SELECT * FROM Tasks 
                WHERE Status = @CompletedStatus 
                  AND CompletedAt >= @From 
                  AND CompletedAt <= @To 
                ORDER BY CompletedAt DESC;
                ";

            return await conn.QueryAsync<Models.TaskItem>(sql, new { CompletedStatus = (int)Models.TaskStatus.Completed, From = from, To = to });
        }

        public async Task UpdateAsync(Models.TaskItem task)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            string updateQuery = @"
                UPDATE Tasks SET 
                Title = @Title,
                Notes = @Notes,
                ProjectTag = @ProjectTag,
                ReminderAt = @ReminderAt,
                Status = @Status,
                CompletedAt = @CompletedAt,
                SnoozeCount = @SnoozeCount,
                LastSnoozedAt = @LastSnoozedAt,
                LastNotifiedAt = @LastNotifiedAt
                WHERE Id = @ID;";

            await conn.ExecuteAsync(updateQuery, task);
        }
        public async Task DeleteAsync(long taskId)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            string deleteQuery = "DELETE FROM Tasks WHERE Id = @Id;";
            await conn.ExecuteAsync(deleteQuery, new { Id = taskId });
        }
        public async Task<Models.TaskItem?> GetByIdAsync(long taskId)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            string selectQuery = "SELECT * FROM Tasks WHERE Id = @Id;";
            return await conn.QueryFirstOrDefaultAsync<Models.TaskItem>(selectQuery, new { Id = taskId });
        }


    }
}
