using Microsoft.EntityFrameworkCore;
using TaskSchedulingApp.Data;
using TaskSchedulingApp.Interfaces;

namespace TaskSchedulingApp.Services
{
    public class ReminderBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReminderBackgroundService> _logger;

        public ReminderBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<ReminderBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TaskReminderService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                        var now = DateTime.UtcNow;
                        var tasks = await context.Tasks
                            .Where(t => (t.DueDate >= now && t.DueDate <= now.AddMinutes(1)) ||
                                        (t.AlarmDate >= now && t.AlarmDate <= now.AddMinutes(1)))
                            .ToListAsync(stoppingToken);

                        foreach (var task in tasks)
                        {
                            if (task.DueDate >= now && task.DueDate <= now.AddMinutes(1))
                            {
                                await notificationService.NotifyDueDateAsync(task.Id);
                            }
                            if (task.AlarmDate >= now && task.AlarmDate <= now.AddMinutes(1))
                            {
                                await notificationService.NotifyAlarmDateAsync(task.Id);
                            }
                        }
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ReminderBackgroundService");
                }
            }

            _logger.LogInformation("ReminderBackgroundService stopped");
        }
    }
}