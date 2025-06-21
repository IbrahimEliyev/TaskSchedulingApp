using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskSchedulingApp.Data;
using TaskSchedulingApp.Hubs;
using TaskSchedulingApp.Interfaces;
using TaskSchedulingApp.Models;
using TaskSchedulingApp.Services;

namespace TaskSchedulingApp.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly DataContext _context;
        private readonly ILogger<TaskRepository> _logger;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<TaskHub> _hubContext;

        public TaskRepository(
            DataContext context,
            ILogger<TaskRepository> logger,
            INotificationService notificationService,
            IHubContext<TaskHub> hubContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }

        public async Task CreateAsync(TaskItem task, IEnumerable<string> assigneeUsernames)
        {
            _logger.LogInformation("Creating task with ID {TaskId}, Title: {Title}", task.Id, task.Title);
            _context.Tasks.Add(task);

            if (assigneeUsernames != null && assigneeUsernames.Any())
            {
                _logger.LogInformation("Processing {Count} assignee usernames", assigneeUsernames.Count());
                foreach (var username in assigneeUsernames)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
                    if (user != null && username != task.CreatedBy)
                    {
                        var assignment = new TaskAssignment
                        {
                            TaskId = task.Id,
                            UserId = user.Id
                        };
                        _context.TaskAssignments.Add(assignment);
                        _logger.LogInformation("Added TaskAssignment for User {UserName}, TaskId: {TaskId}", username, task.Id);
                    }
                    else
                    {
                        _logger.LogWarning("User {UserName} not found or is creator, skipping assignment", username);
                    }
                }
            }
            else
            {
                _logger.LogWarning("No assignee usernames provided for TaskId: {TaskId}", task.Id);
            }

            _logger.LogInformation("TaskAssignments count before SaveChanges: {Count}", task.TaskAssignments.Count);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Saved changes for TaskId: {TaskId}, TaskAssignments count: {Count}", task.Id, task.TaskAssignments.Count);

            foreach (var username in assigneeUsernames ?? Enumerable.Empty<string>())
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
                if (user != null && username != task.CreatedBy)
                {
                    await _hubContext.Groups.AddToGroupAsync(user.Id, $"task_{task.Id}");
                }
            }
            await _notificationService.NotifyAssigneesAsync(task.Id, "created", task.CreatedBy);
        }

        public async Task DeleteAsync(Guid id, string userName)
        {
            var task = await _context.Tasks
                .Include(t => t.TaskAssignments)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                throw new KeyNotFoundException("Task not found");

            _context.TaskAssignments.RemoveRange(task.TaskAssignments);
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            await _notificationService.NotifyAssigneesAsync(id, "deleted", userName);
        }

        public async Task<IEnumerable<TaskItem>> GetAllByUserAsync(string userName)
        {
            return await _context.Tasks
                .Include(t => t.TaskAssignments)
                .ThenInclude(ta => ta.User)
                .Where(t => t.CreatedBy == userName || t.TaskAssignments.Any(ta => ta.User.UserName == userName))
                .ToListAsync();
        }

        public async Task<TaskItem> GetTaskByIdAsync(Guid id, string userName)
        {
            return await _context.Tasks
                .Include(t => t.TaskAssignments)
                .ThenInclude(ta => ta.User)
                .FirstOrDefaultAsync(t => (t.Id == id) && (t.CreatedBy == userName || t.TaskAssignments.Any(ta => ta.User.UserName == userName)));
        }

        public async Task<TaskItem> GetTaskByTitleAsync(string title, string userName)
        {
            return await _context.Tasks
                .Include(t => t.TaskAssignments)
                .ThenInclude(ta => ta.User)
                .FirstOrDefaultAsync(t => (t.Title == title) && (t.CreatedBy == userName || t.TaskAssignments.Any(ta => ta.User.UserName == userName)));
        }

        public async Task UpdateAsync(TaskItem task, IEnumerable<string> assigneeUsernames)
        {
            var existingTask = await _context.Tasks
                .Include(t => t.TaskAssignments)
                .FirstOrDefaultAsync(t => t.Id == task.Id);

            if (existingTask == null)
                throw new KeyNotFoundException("Task not found");

            existingTask.Title = task.Title;
            existingTask.Description = task.Description;
            existingTask.DueDate = task.DueDate;
            existingTask.Status = task.Status;

            _context.TaskAssignments.RemoveRange(existingTask.TaskAssignments);

            if (assigneeUsernames != null && assigneeUsernames.Any())
            {
                foreach (var username in assigneeUsernames)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
                    if (user != null)
                    {
                        var assignment = new TaskAssignment
                        {
                            TaskId = task.Id,
                            UserId = user.Id
                        };
                        _context.TaskAssignments.Add(assignment);
                    }
                }
            }

            await _context.SaveChangesAsync();

            foreach (var username in assigneeUsernames ?? Enumerable.Empty<string>())
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
                if (user != null)
                {
                    await _hubContext.Groups.AddToGroupAsync(user.Id, $"task_{task.Id}");
                }
            }
            await _notificationService.NotifyAssigneesAsync(task.Id, "updated", task.CreatedBy);
        }

        public async Task RejectTaskAsync(Guid taskId, string username)
        {
            _logger.LogInformation("Removing user {Username} from task {TaskId} assignments", username, taskId);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null)
            {
                _logger.LogWarning("User {Username} not found", username);
                throw new KeyNotFoundException("User not found");
            }

            var task = await _context.Tasks
                .Include(t => t.TaskAssignments)
                .FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found", taskId);
                throw new KeyNotFoundException("Task not found");
            }

            var assignment = task.TaskAssignments.FirstOrDefault(ta => ta.UserId == user.Id);
            if (assignment == null)
            {
                _logger.LogWarning("User {Username} (Id: {UserId}) is not assigned to task {TaskId}", username, user.Id, taskId);
                throw new InvalidOperationException("User is not assigned to this task");
            }

            _context.TaskAssignments.Remove(assignment);

            await _hubContext.Groups.RemoveFromGroupAsync(user.Id, $"task_{taskId}");
            await _context.SaveChangesAsync();

            await _notificationService.NotifyAssigneesAsync(taskId, $"rejected by {username}", username);

            _logger.LogInformation("User {Username} (Id: {UserId}) removed from task {TaskId} assignments", username, user.Id, taskId);
        }
    }
}