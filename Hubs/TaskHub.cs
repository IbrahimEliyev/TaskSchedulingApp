using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Serilog.Context;
using System.Security.Claims;
using TaskSchedulingApp.Data;

namespace TaskSchedulingApp.Hubs
{
    [Authorize]
    public class TaskHub : Hub
    {
        private readonly DataContext _context;
        private readonly ILogger<TaskHub> _logger;

        public TaskHub(DataContext context, ILogger<TaskHub> logger)
        {
            _context = context;
            _logger = logger;
        }
        public override async Task OnConnectedAsync()
        {
            var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
            using (LogContext.PushProperty("ConnectionId", Context.ConnectionId))
            using (LogContext.PushProperty("Username", username ?? "Anonymous"))
            {
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("Unauthorized connection attempt: No username found");
                    throw new HubException("User not authenticated");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
                if (user == null)
                {
                    _logger.LogWarning("User not found in database");
                    throw new HubException("User not found");
                }

                var taskIds = await _context.TaskAssignments
                    .Where(ta => ta.UserId == user.Id)
                    .Select(ta => ta.TaskId)
                    .ToListAsync();
                foreach (var taskId in taskIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"task_{taskId}");
                    _logger.LogInformation("Added user to group task_{TaskId}", taskId);
                }

                _logger.LogInformation("User connected with claims: {Claims}",
                    string.Join(", ", Context.User.Claims.Select(c => $"{c.Type}: {c.Value}")));
                await base.OnConnectedAsync();
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
            using (LogContext.PushProperty("ConnectionId", Context.ConnectionId))
            using (LogContext.PushProperty("Username", username ?? "Anonymous"))
            {
                if (!string.IsNullOrEmpty(username))
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
                    if (user != null)
                    {
                        var taskIds = await _context.TaskAssignments
                            .Where(ta => ta.UserId == user.Id)
                            .Select(ta => ta.TaskId)
                            .ToListAsync();
                        foreach (var taskId in taskIds)
                        {
                            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"task_{taskId}");
                            _logger.LogInformation("Removed user from group task_{TaskId}", taskId);
                        }
                    }
                }

                _logger.LogInformation("User disconnected. Exception: {ExceptionMessage}",
                    exception?.Message ?? "None");
                await base.OnDisconnectedAsync(exception);
            }
        }
    }
}