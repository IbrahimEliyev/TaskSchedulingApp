using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using TaskSchedulingApp.DTOs;
using TaskSchedulingApp.Hubs;
using TaskSchedulingApp.Interfaces;
using TaskSchedulingApp.Models;
using TaskSchedulingApp.Services;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace TaskSchedulingApp.Controllers
{
    /// <summary>
    /// Controller for managing tasks in the Task Scheduling application.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IHubContext<TaskHub> _hubContext;
        private readonly INotificationService _notificationService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<TasksController> _logger;

        public TasksController(
            ITaskRepository taskRepository,
            IHubContext<TaskHub> hubContext,
            INotificationService notificationService,
            UserManager<User> userManager,
            ILogger<TasksController> logger)
        {
            _taskRepository = taskRepository;
            _hubContext = hubContext;
            _notificationService = notificationService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all tasks assigned to or created by the authenticated user.
        /// </summary>
        /// <returns>A list of tasks in <see cref="TaskResponseDto"/> format.</returns>
        /// <response code="200">Returns the list of tasks.</response>
        /// <response code="404">No tasks found for the user.</response>
        /// <response code="500">Internal server error occurred.</response>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tasks = await _taskRepository.GetAllByUserAsync(User.Identity.Name);
            if (tasks == null)
                return NotFound();
            try
            {
                var responseDtos = tasks.Select(task => new TaskResponseDto
                {
                    Id = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    DueDate = task?.DueDate,
                    AlarmDate = task?.AlarmDate,
                    Status = task.Status,
                    CreatedDate = task.CreatedDate,
                    CreatedBy = task.CreatedBy,
                    AssigneeUsernames = task.TaskAssignments.Select(ta => ta.User.UserName).ToList()
                }).ToList();

                return Ok(responseDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all tasks");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Retrieves a specific task by its ID, if the user is a team member.
        /// </summary>
        /// <param name="id">The unique identifier of the task.</param>
        /// <returns>The task details in <see cref="TaskResponseDto"/> format.</returns>
        /// <response code="200">Returns the task details.</response>
        /// <response code="404">Task not found or user not authorized.</response>
        /// <response code="500">Internal server error occurred.</response>
        [Authorize(Policy = "TeamMember")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var task = await _taskRepository.GetTaskByIdAsync(id, User.Identity.Name);
                if (task == null)
                {
                    _logger.LogWarning("Task not found: Id: {Id}", id);
                    return NotFound();
                }

                var responseDto = new TaskResponseDto
                {
                    Id = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    DueDate = task?.DueDate,
                    AlarmDate = task?.AlarmDate,
                    Status = task.Status,
                    CreatedDate = task.CreatedDate,
                    CreatedBy = task.CreatedBy,
                    AssigneeUsernames = task.TaskAssignments.Select(ta => ta.User.UserName).ToList()
                };

                return Ok(responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve task: Id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Creates a new task, restricted to users with the TeamLead role.
        /// </summary>
        /// <param name="taskDto">The task creation data in <see cref="CreateTaskDto"/> format.</param>
        /// <returns>The created task in <see cref="TaskResponseDto"/> format.</returns>
        /// <response code="201">Task created successfully.</response>
        /// <response code="400">Invalid task data provided.</response>
        /// <response code="500">Internal server error occurred.</response>
        [Authorize(Roles = "TeamLead")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTaskDto taskDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var task = new TaskItem
                {
                    Title = taskDto.Title,
                    Description = taskDto.Description,
                    DueDate = taskDto.DueDate,
                    Status = "Pending",
                    CreatedBy = User.FindFirst(ClaimTypes.Name)?.Value,
                    CreatedDate = DateTime.UtcNow
                };

                await _taskRepository.CreateAsync(task, taskDto.AssigneeUsernames);

                var responseDto = new TaskResponseDto
                {
                    Id = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    DueDate = task?.DueDate,
                    AlarmDate = task?.AlarmDate,
                    Status = task.Status,
                    CreatedDate = task.CreatedDate,
                    CreatedBy = task.CreatedBy,
                    AssigneeUsernames = task.TaskAssignments.Select(ta => ta.User.UserName).ToList()
                };

                return CreatedAtAction(nameof(GetById), new { id = task.Id }, responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create task: Title: {Title}", taskDto.Title);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Updates an existing task, restricted to the team member(either task creator or assignee).
        /// </summary>
        /// <param name="id">The unique identifier of the task to update.</param>
        /// <param name="taskDto">The updated task data in <see cref="UpdateTaskDto"/> format.</param>
        /// <returns>The updated task in <see cref="TaskResponseDto"/> format.</returns>
        /// <response code="200">Task updated successfully.</response>
        /// <response code="400">Invalid task data or due date in the past.</response>
        /// <response code="404">Task not found or user not authorized.</response>
        [Authorize(Policy = "TaskCreator")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskDto taskDto)
        {
            if (!ModelState.IsValid || (taskDto.DueDate.HasValue && taskDto.DueDate < DateTime.UtcNow))
                return BadRequest(ModelState);

            var task = await _taskRepository.GetTaskByIdAsync(id, User.Identity.Name);
            if (task == null)
                return NotFound();

            task.Title = taskDto?.Title;
            task.Description = taskDto?.Description;
            task.DueDate = taskDto?.DueDate;
            task.Status = taskDto?.Status;
            task.AlarmDate = taskDto?.AlarmDate;

            var responseDto = new TaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                DueDate = task?.DueDate,
                AlarmDate = task?.AlarmDate,
                Status = task.Status,
                CreatedDate = task.CreatedDate,
                CreatedBy = task.CreatedBy,
                AssigneeUsernames = task.TaskAssignments.Select(ta => ta.User.UserName).ToList()
            };

            await _taskRepository.UpdateAsync(task, taskDto.AssigneeUsernames);
            return Ok(responseDto);
        }

        /// <summary>
        /// Deletes a task by its ID, restricted to the task creator.
        /// </summary>
        /// <param name="id">The unique identifier of the task to delete.</param>
        /// <returns>No content if deletion is successful.</returns>
        /// <response code="204">Task deleted successfully.</response>
        /// <response code="404">Task not found or user not authorized.</response>
        /// <response code="500">Internal server error occurred.</response>
        [Authorize(Policy = "TaskCreator")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                _logger.LogInformation("Deleting task: Id: {Id}", id);

                var task = await _taskRepository.GetTaskByIdAsync(id, User.Identity.Name);
                if (task == null)
                {
                    _logger.LogWarning("Task not found for deletion: Id: {Id}", id);
                    return NotFound();
                }

                await _taskRepository.DeleteAsync(id, User.Identity.Name);

                _logger.LogInformation("Task deleted successfully: Id: {Id}", id);
                return StatusCode(204, $"Task deleted successfully: Id: {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete task: Id: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Rejects a task assignment, restricted to developers who are team members.
        /// </summary>
        /// <param name="id">The unique identifier of the task to reject.</param>
        /// <returns>No content if rejection is successful.</returns>
        /// <response code="204">Task rejected successfully.</response>
        /// <response code="401">User not authenticated.</response>
        /// <response code="403">User not authorized to reject the task.</response>
        /// <response code="404">Task or assignment not found.</response>
        /// <response code="500">Internal server error occurred.</response>
        [Authorize(Roles = "Developer")]
        [Authorize(Policy = "TeamMember")]
        [HttpPost("reject/{id}")]
        public async Task<IActionResult> RejectTask(Guid id)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                await _taskRepository.RejectTaskAsync(id, username);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove user from task {TaskId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}