

using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PunchList.Models
{
    public class TaskItem : ObservableObject
    {
        public const int DEFAULT_SNOOZE_TIMER = 5;

        public long ID { get; set; }
        public string Title { get; set; } = "";
        public string? Notes { get; set; }
        public string? ProjectTag { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ReminderAt { get; set; }
        public TaskStatus Status { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public int SnoozeCount { get; set; }
        public DateTimeOffset? LastSnoozedAt { get; set; }
        public DateTimeOffset? LastNotifiedAt { get; set; }

        public bool IsOverdue => Status == TaskStatus.Overdue || (Status == TaskStatus.Running && DateTimeOffset.Now >= ReminderAt);

        public double Progress
        {
            get
            {
                if (Status == TaskStatus.Completed || Status == TaskStatus.Overdue) return 1.0;
                var total = (ReminderAt - CreatedAt).TotalSeconds;
                if (total <= 0) return 1.0;
                var elapsed = (DateTimeOffset.Now - CreatedAt).TotalSeconds;
                return Math.Clamp(elapsed / total, 0.0, 1.0);
            }
        }

        public string RemainingTimeText
        {
            get
            {
                if (Status == TaskStatus.Completed)
                {
                    return CompletedAt.HasValue
                        ? $"Completed {CompletedAt.Value:MMM dd, HH:mm}"
                        : "Completed";
                }

                var diff = ReminderAt - DateTimeOffset.Now;
                if (diff.TotalSeconds <= 0)
                {
                    var overdue = DateTimeOffset.Now - ReminderAt;
                    if (overdue.TotalMinutes < 1)
                        return $"Overdue by {Math.Max(1, (int)overdue.TotalSeconds)}s";
                    if (overdue.TotalHours < 1)
                        return $"Overdue by {(int)overdue.TotalMinutes}m";
                    return $"Overdue by {(int)overdue.TotalHours}h {overdue.Minutes}m";
                }
                else
                {
                    if (diff.TotalMinutes < 1)
                        return $"Due in {Math.Max(1, (int)diff.TotalSeconds)}s";
                    if (diff.TotalHours < 1)
                        return $"Due in {(int)diff.TotalMinutes}m {diff.Seconds}s";
                    if (diff.TotalHours < 24)
                        return $"Due in {(int)diff.TotalHours}h {diff.Minutes}m";
                    return $"Due {ReminderAt:MMM dd, HH:mm}";
                }
            }
        }

        public void RefreshProgress()
        {
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(RemainingTimeText));
            OnPropertyChanged(nameof(IsOverdue));
        }
    }
}

