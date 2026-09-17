using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PunchList.Data;
using PunchList.Models;
using PunchList.Services;
using TaskStatus = PunchList.Models.TaskStatus;

namespace PunchList.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly TaskRepository _repository;
        private readonly TaskStateMachine _stateMachine;
        private readonly TaskTimerService _timerService;
        private readonly NotificationService _notificationService;
        private readonly StartupService _startupService;

        [ObservableProperty]
        private ObservableCollection<TaskItem> _activeTasks = new();

        [ObservableProperty]
        private ObservableCollection<TaskItem> _historyTasks = new();

        [ObservableProperty]
        private string _newTaskTitle = string.Empty;

        [ObservableProperty]
        private string? _newTaskNotes;

        [ObservableProperty]
        private string? _newTaskProjectTag;

        [ObservableProperty]
        private string _reminderInput = "15m";

        [ObservableProperty]
        private string _calculatedDuePreview = "Due in 15m";

        [ObservableProperty]
        private int _reminderMinutes = 15;

        [ObservableProperty]
        private bool _isAutoStartEnabled;

        [ObservableProperty]
        private TaskItem? _bannerTask;

        [ObservableProperty]
        private bool _showBanner;

        [ObservableProperty]
        private bool _isHistoryView;

        [ObservableProperty]
        private int _activeCount;

        [ObservableProperty]
        private int _historyCount;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        private readonly System.Windows.Threading.DispatcherTimer _uiCountdownTimer;

        public MainViewModel(
            TaskRepository repository,
            TaskStateMachine stateMachine,
            TaskTimerService timerService,
            NotificationService notificationService,
            StartupService startupService)
        {
            _repository = repository;
            _stateMachine = stateMachine;
            _timerService = timerService;
            _notificationService = notificationService;
            _startupService = startupService;

            _isAutoStartEnabled = _startupService.IsAutoStartEnabled();

            // Wire in-app banner
            _notificationService.InAppBannerRequested += OnInAppBannerRequested;

            // When background timer ticks or updates occur, refresh active tasks on UI thread
            _timerService.TaskNotificationTriggered += _ =>
            {
                Application.Current?.Dispatcher?.Invoke(async () => await LoadDataAsync());
            };

            // When user clicks Complete or Snooze on the Windows toast notification, refresh UI immediately
            _notificationService.ToastActionCompleted += (taskId, action) =>
            {
                Application.Current?.Dispatcher?.Invoke(async () =>
                {
                    if (BannerTask?.ID == taskId)
                    {
                        ShowBanner = false;
                        BannerTask = null;
                    }
                    StatusMessage = action == "complete" 
                        ? "Task marked completed from notification ✓" 
                        : $"Task snoozed from notification (+{TaskItem.DEFAULT_SNOOZE_TIMER}m)";
                    await LoadDataAsync();
                });
            };

            // Lightweight 1-second UI timer to smoothly update countdown labels and progress rings
            _uiCountdownTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _uiCountdownTimer.Tick += (_, _) =>
            {
                if (ActiveTasks != null && ActiveTasks.Count > 0)
                {
                    foreach (var task in ActiveTasks)
                    {
                        task.RefreshProgress();
                    }
                }
            };
            _uiCountdownTimer.Start();

            UpdateDuePreview();
        }

        private void OnInAppBannerRequested(TaskItem task)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                BannerTask = task;
                ShowBanner = true;
            });
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            var active = await _repository.GetActiveAsync();
            ActiveTasks = new ObservableCollection<TaskItem>(active);
            ActiveCount = ActiveTasks.Count;

            var history = await _repository.GetHistoryAsync(DateTimeOffset.Now.AddDays(-30), DateTimeOffset.Now.AddDays(1));
            HistoryTasks = new ObservableCollection<TaskItem>(history);
            HistoryCount = HistoryTasks.Count;
        }

        [RelayCommand]
        public void SetView(string? view)
        {
            IsHistoryView = view?.Equals("history", StringComparison.OrdinalIgnoreCase) == true;
        }

        partial void OnReminderInputChanged(string value)
        {
            UpdateDuePreview();
        }

        public void UpdateDuePreview()
        {
            var span = ParseReminderDuration(ReminderInput);
            var due = DateTimeOffset.Now.Add(span);

            if (span.TotalHours >= 24)
            {
                CalculatedDuePreview = $"Due: {due:ddd, MMM d, h:mm tt}";
            }
            else if (span.TotalHours >= 1)
            {
                CalculatedDuePreview = $"Due: Today {due:h:mm tt} (in {span.TotalHours:0.#}h)";
            }
            else if (span.TotalSeconds < 60)
            {
                CalculatedDuePreview = $"Due in {(int)Math.Max(1, span.TotalSeconds)}s";
            }
            else
            {
                CalculatedDuePreview = $"Due: Today {due:h:mm tt} (in {(int)span.TotalMinutes}m)";
            }
        }

        public static TimeSpan ParseReminderDuration(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return TimeSpan.FromMinutes(15);

            var clean = input.Trim().ToLowerInvariant();

            if (clean.StartsWith("tomorrow"))
            {
                var tomorrow9am = DateTimeOffset.Now.Date.AddDays(1).AddHours(9);
                var diff = tomorrow9am - DateTimeOffset.Now;
                return diff > TimeSpan.Zero ? diff : TimeSpan.FromHours(24);
            }

            if (clean.EndsWith("s") || clean.EndsWith("sec") || clean.EndsWith("seconds"))
            {
                var numPart = clean.TrimEnd('s', 'e', 'c', 'o', 'n', 'd').Trim();
                if (double.TryParse(numPart, out double secs) && secs > 0)
                    return TimeSpan.FromSeconds(secs);
            }

            if (clean.EndsWith("h") || clean.EndsWith("hr") || clean.EndsWith("hrs") || clean.EndsWith("hour") || clean.EndsWith("hours"))
            {
                var numPart = clean.TrimEnd('h', 'r', 's', 'o', 'u').Trim();
                if (double.TryParse(numPart, out double hrs) && hrs > 0)
                    return TimeSpan.FromHours(hrs);
            }

            if (clean.EndsWith("d") || clean.EndsWith("day") || clean.EndsWith("days"))
            {
                var numPart = clean.TrimEnd('d', 'a', 'y', 's').Trim();
                if (double.TryParse(numPart, out double days) && days > 0)
                    return TimeSpan.FromDays(days);
            }

            var minPart = clean.TrimEnd('m', 'i', 'n', 'u', 't', 'e', 's').Trim();
            if (double.TryParse(minPart, out double mins) && mins > 0)
                return TimeSpan.FromMinutes(mins);

            return TimeSpan.FromMinutes(15);
        }

        [RelayCommand]
        public async Task AddTaskAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTaskTitle))
            {
                MessageBox.Show("Please enter a task title.", "Punchlist", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var span = ParseReminderDuration(ReminderInput);

            var task = new TaskItem
            {
                Title = NewTaskTitle.Trim(),
                Notes = string.IsNullOrWhiteSpace(NewTaskNotes) ? null : NewTaskNotes.Trim(),
                ProjectTag = string.IsNullOrWhiteSpace(NewTaskProjectTag) ? null : NewTaskProjectTag.Trim(),
                CreatedAt = DateTimeOffset.Now,
                ReminderAt = DateTimeOffset.Now.Add(span),
                Status = TaskStatus.Running
            };

            await _repository.InsertAsync(task);

            NewTaskTitle = string.Empty;
            NewTaskNotes = null;
            NewTaskProjectTag = null;
            StatusMessage = $"Added: '{task.Title}' ({CalculatedDuePreview})";

            await LoadDataAsync();
        }

        [RelayCommand]
        public async Task AddTestTaskAsync()
        {
            var task = new TaskItem
            {
                Title = "Quick 15s Test Task",
                Notes = "Verifying toast notification and in-app banner",
                ProjectTag = "Demo",
                CreatedAt = DateTimeOffset.Now,
                ReminderAt = DateTimeOffset.Now.AddSeconds(15),
                Status = TaskStatus.Running
            };

            await _repository.InsertAsync(task);
            StatusMessage = "Added 15-second test task! Notification will trigger on next tick...";
            await LoadDataAsync();
        }

        [RelayCommand]
        public void SetReminderPreset(string preset)
        {
            ReminderInput = preset;
            UpdateDuePreview();
            StatusMessage = $"Reminder offset set to {preset}";
        }

        [RelayCommand]
        public async Task CompleteTaskAsync(TaskItem? task)
        {
            if (task == null) return;
            _stateMachine.Complete(task, DateTimeOffset.Now);
            await _repository.UpdateAsync(task);

            if (BannerTask?.ID == task.ID)
            {
                ShowBanner = false;
                BannerTask = null;
            }

            StatusMessage = $"Completed: '{task.Title}'";
            await LoadDataAsync();
        }

        [RelayCommand]
        public async Task SnoozeTaskAsync(TaskItem? task)
        {
            if (task == null) return;
            _stateMachine.AddExtraTime(task, TimeSpan.FromMinutes(TaskItem.DEFAULT_SNOOZE_TIMER), DateTimeOffset.Now);
            await _repository.UpdateAsync(task);

            if (BannerTask?.ID == task.ID)
            {
                ShowBanner = false;
                BannerTask = null;
            }

            StatusMessage = $"Snoozed: '{task.Title}' for +{TaskItem.DEFAULT_SNOOZE_TIMER}m";
            await LoadDataAsync();
        }

        [RelayCommand]
        public async Task DeleteTaskAsync(TaskItem? task)
        {
            if (task == null) return;
            await _repository.DeleteAsync(task.ID);

            if (BannerTask?.ID == task.ID)
            {
                ShowBanner = false;
                BannerTask = null;
            }

            StatusMessage = $"Deleted: '{task.Title}'";
            await LoadDataAsync();
        }

        [RelayCommand]
        public void DismissBanner()
        {
            ShowBanner = false;
            BannerTask = null;
        }

        [RelayCommand]
        public void ToggleAutoStart()
        {
            IsAutoStartEnabled = !IsAutoStartEnabled;
            _startupService.SetAutoStart(IsAutoStartEnabled);
            StatusMessage = IsAutoStartEnabled ? "Auto-start enabled" : "Auto-start disabled";
        }
    }
}
