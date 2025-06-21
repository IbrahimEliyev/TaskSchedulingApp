namespace TaskSchedulingApp.Interfaces
{
    public interface INotificationService
    {
        Task NotifyAssigneesAsync(Guid taskId, string action, string initiatorUsername);
        Task NotifyDueDateAsync(Guid taskId);
        Task NotifyAlarmDateAsync(Guid taskId);
    }
}
