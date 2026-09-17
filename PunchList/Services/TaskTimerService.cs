using PunchList.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PunchList.Services
{
    public class TaskTimerService
    {
        private readonly TaskStateMachine _stateMachine;
        private readonly TaskRepository _taskRepository;

        private CancellationTokenSource? _ctToken;

        public event Action<Models.TaskItem>? TaskNotificationTriggered;
        public TaskTimerService(TaskStateMachine stateMachine, TaskRepository taskRepository)
        {
            _stateMachine = stateMachine;
            _taskRepository = taskRepository;
        }

        public void Start()
        {
            _ctToken = new CancellationTokenSource();

            Task.Run(() => RunLoopAsync(_ctToken.Token));

        }
        public void Stop()
        {
            _ctToken?.Cancel();
        }
        private async Task RunLoopAsync(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
            try
            {

                while(!ct.IsCancellationRequested&& await timer.WaitForNextTickAsync(ct))
                {
                    var tasks = await _taskRepository.GetActiveAsync();
                    foreach (var task in tasks)
                    {
                        var result = _stateMachine.Evaluate(task, DateTimeOffset.Now);
                        if (result.StateChanged)
                        {
                            await _taskRepository.UpdateAsync(task);
                        }
                        if (result.ShouldNotify)
                        {
                            TaskNotificationTriggered?.Invoke(task);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation gracefully
            }

        }
    }
}
