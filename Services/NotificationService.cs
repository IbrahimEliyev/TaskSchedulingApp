using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TaskSchedulingApp.Data;
using TaskSchedulingApp.Hubs;
using TaskSchedulingApp.Interfaces;

namespace TaskSchedulingApp.Services
{
    public class NotificationService : INotificationService
    {
        private readonly DataContext _context;
        private readonly IHubContext<TaskHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            DataContext context,
            IHubContext<TaskHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyAssigneesAsync(Guid taskId, string action, string initiatorUsername)
        {
            _logger.LogInformation("Notifying group for task {TaskId}, action: {Action}", taskId, action);

            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found for notification", taskId);
                return;
            }

            var message = $"Task '{task.Title}' {action} by {initiatorUsername}.";
            await _hubContext.Clients.Group($"task_{taskId}").SendAsync("ReceiveNotification", message);
        }

        public async Task NotifyDueDateAsync(Guid taskId)
        {
            _logger.LogInformation("Notifying due date for task {TaskId}", taskId);

            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found for due date notification", taskId);
                return;
            }

            var message = $"Task '{task.Title}' is due now.";
            await _hubContext.Clients.Group($"task_{taskId}").SendAsync("ReceiveNotification", message);
        }

        public async Task NotifyAlarmDateAsync(Guid taskId)
        {
            _logger.LogInformation("Notifying alarm date for task {TaskId}", taskId);

            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found for alarm date notification", taskId);
                return;
            }

            var message = $"Reminder: Task '{task.Title}' alarm triggered.";
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", message);
        }
    }
}