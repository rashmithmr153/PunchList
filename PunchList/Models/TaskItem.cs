

namespace PunchList.Models
{
    public class TaskItem
    {
        public const int DEFAULT_SNOOZE_TIMER = 5;
        public long ID { get; set; }
        public string Title { get; set; } = "";
        public string? Notes { get; set; }

        public string? ProjectTag { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset ReminderAt { get; set; }

        public TaskStatus Status { get; set; }

        public DateTimeOffset? CompletedAt {get; set; }

        public int SnoozeCount { get; set; }

        public DateTimeOffset? LastSnoozedAt { get; set; }
        public DateTimeOffset? LastNotifiedAt { get; set; }


    }
}
