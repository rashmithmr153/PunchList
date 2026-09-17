using Microsoft.Toolkit.Uwp.Notifications;
using PunchList.Data;
using PunchList.Models;

namespace PunchList.Services
{
    public class NotificationService
    {
        private readonly TaskRepository _taskRepository;
        private readonly TaskTimerService _taskTimerService;
        private readonly TaskStateMachine _stateMachine;
        public event Action<TaskItem>? InAppBannerRequested;

        public NotificationService(TaskRepository taskRepository, TaskTimerService taskTimerService, TaskStateMachine stateMachine)

        {
            _taskRepository = taskRepository;
            _taskTimerService = taskTimerService;
            _stateMachine = stateMachine;
            ToastNotificationManagerCompat.OnActivated += OnToastActivated;
            _taskTimerService.TaskNotificationTriggered += ShowToast;


        }

        public async void OnToastActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            var args = ToastArguments.Parse(e.Argument);
            if (args.TryGetValue("action", out string? action) && args.TryGetValue("taskId", out string? taskIdStr) && int.TryParse(taskIdStr, out int taskId))
            {
                await HandleToastAction(action, taskId);
            }
        }

        public void ShowToast(TaskItem task)
        {
            if (System.Windows.Application.Current?.MainWindow?.IsVisible == true)
            {
                InAppBannerRequested?.Invoke(task);
            }

            new ToastContentBuilder()
                .AddText($"Task Reminder: {task.Title}")
                .AddText(string.IsNullOrWhiteSpace(task.Notes) ? "Task is overdue!" : task.Notes)
                .AddArgument("action", "open")
                .AddArgument("taskId", task.ID.ToString())
                .AddButton(new ToastButton()
                    .SetContent("Complete")
                    .AddArgument("action", "complete")
                    .AddArgument("taskId", task.ID.ToString())
                    )
    
                .AddButton(new ToastButton()
                    .SetContent("+5m")
                    .AddArgument("action", "snooze")
                    .AddArgument("taskId", task.ID.ToString())
                )
                .Show();
        }

        public async Task HandleToastAction(string action, long taskId)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null) return;
            switch (action)
            {
                case "complete":
                    _stateMachine.Complete(task, DateTimeOffset.Now);
                    await _taskRepository.UpdateAsync(task);
                    break;
                case "snooze":
                    _stateMachine.AddExtraTime(task, TimeSpan.FromMinutes(TaskItem.DEFAULT_SNOOZE_TIMER), DateTimeOffset.Now);
                    await _taskRepository.UpdateAsync(task);
                    break;

                
                default:
                    break;
            }
            if (action=="open")
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var window = System.Windows.Application.Current.MainWindow;
                    if (window != null)
                    {
                        window.Show();
                        window.WindowState = System.Windows.WindowState.Normal;
                        window.Activate();
                    }
                });
            }
        }
    }
}
