using PunchList.Models;


namespace PunchList.Services
{
    public class TaskStateMachine
    {
        public record EvaluationResult(bool StateChanged, bool ShouldNotify);


        public EvaluationResult Evaluate(TaskItem task, DateTimeOffset now)
        {
            switch(task.Status)
            {
                case Models.TaskStatus.Running:
                    if (task.ReminderAt <= now)
                    {
                        task.Status = Models.TaskStatus.Overdue;
                        task.LastNotifiedAt = now;
                        return new EvaluationResult(true, true);
                    }
                    break;
                case Models.TaskStatus.Overdue:
                    if (task.LastNotifiedAt == null || now >= task.LastNotifiedAt.Value.AddMinutes(TaskItem.DEFAULT_SNOOZE_TIMER))
                    {
                        task.LastNotifiedAt = now;
                        return new EvaluationResult(true, true);
                    }
                    break;
                case Models.TaskStatus.Completed:
                    // No state change for completed tasks
                    return new EvaluationResult(false, false);

            }
            return new EvaluationResult(false, false);
        }

        public void AddExtraTime(TaskItem task, TimeSpan extraDuration, DateTimeOffset now)
        {
            task.ReminderAt = now + extraDuration;
            task.Status = Models.TaskStatus.Running;
            task.LastSnoozedAt = now;
            task.SnoozeCount += 1;
            task.LastNotifiedAt = null;
        }
        public void Complete(TaskItem task, DateTimeOffset now)
        {
            task.Status = Models.TaskStatus.Completed;
            task.CompletedAt = now;
        }
    }
}
