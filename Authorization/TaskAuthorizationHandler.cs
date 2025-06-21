using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using TaskSchedulingApp.Data;
using TaskSchedulingApp.Models;

namespace TaskSchedulingApp.Authorization
{
    public class TaskAuthorizationHandler : IAuthorizationHandler
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<TaskAuthorizationHandler> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TaskAuthorizationHandler(
            DataContext context,
            UserManager<User> userManager,
            ILogger<TaskAuthorizationHandler> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public async Task HandleAsync(AuthorizationHandlerContext context)
        {
            var claims = context.User.Claims.ToList();
            var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;
            var username = context.User.FindFirst(ClaimTypes.Name)?.Value ??
                           context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("AuthorizationHandler: No username found. IsAuthenticated: {IsAuthenticated}, Claims: {Claims}",
                    isAuthenticated, string.Join(", ", claims.Select(c => $"{c.Type}: {c.Value}")));
                return;
            }

            var user = await _userManager.FindByNameAsync(username) ??
                       await _userManager.Users.FirstOrDefaultAsync(u => u.Id == username);
            if (user == null)
            {
                _logger.LogWarning("AuthorizationHandler: User not found for identifier: {Username}", username);
                return;
            }

            TaskItem? task = context.Resource as TaskItem;
            if (task == null)
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null && httpContext.Request.RouteValues.TryGetValue("id", out var idValue) &&
                    Guid.TryParse(idValue?.ToString(), out var taskId))
                {
                    task = await _context.Tasks
                        .Include(t => t.TaskAssignments)
                        .ThenInclude(ta => ta.User)
                        .FirstOrDefaultAsync(t => t.Id == taskId);
                    if (task == null)
                    {
                        _logger.LogWarning("AuthorizationHandler: Task not found for ID {TaskId}", taskId);
                        return;
                    }
                }
                else
                {
                    _logger.LogWarning("AuthorizationHandler: No task resource or valid task ID for {Username}", username);
                    return;
                }
            }

            foreach (var requirement in context.PendingRequirements.ToList())
            {
                if (requirement is TaskCreatorRequirement)
                {
                    var isTeamLead = await _userManager.IsInRoleAsync(user, "TeamLead");
                    if (!isTeamLead)
                    {
                        _logger.LogInformation("TaskCreatorRequirement: User {Username} is not TeamLead", username);
                        continue;
                    }

                    if (task.CreatedBy == username)
                    {
                        _logger.LogInformation("TaskCreatorRequirement: User {Username} is creator of task {TaskId}", username, task.Id);
                        context.Succeed(requirement);
                    }
                    else
                    {
                        _logger.LogInformation("TaskCreatorRequirement: User {Username} is not creator of task {TaskId}", username, task.Id);
                    }
                }
                else if (requirement is TeamMemberRequirement)
                {
                    var isCreator = await _userManager.IsInRoleAsync(user, "TeamLead") && task.CreatedBy == username;
                    var isAssignee = await _context.TaskAssignments
                        .AnyAsync(ta => ta.TaskId == task.Id && ta.UserId == user.Id);

                    if (isCreator || isAssignee)
                    {
                        _logger.LogInformation("TeamMemberRequirement: User {Username} is {Status} for task {TaskId}",
                            username, isCreator ? "creator" : "assignee", task.Id);
                        context.Succeed(requirement);
                    }
                    else
                    {
                        _logger.LogInformation("TeamMemberRequirement: User {Username} is neither creator nor assignee for task {TaskId}",
                            username, task.Id);
                    }
                }
            }
        }
    }
}