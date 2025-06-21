using Microsoft.AspNetCore.Authentication;
using TaskSchedulingApp.Data;
using TaskSchedulingApp.Models;

namespace TaskSchedulingApp.Interfaces
{
    public interface ITaskRepository
    {
        public Task<IEnumerable<TaskItem>> GetAllByUserAsync(string userName);
        public Task<TaskItem> GetTaskByIdAsync(Guid id, string userName);
        public Task<TaskItem> GetTaskByTitleAsync(string title, string userName);
        public Task CreateAsync(TaskItem task, IEnumerable<string> assigneeUsernames);
        public Task UpdateAsync(TaskItem task, IEnumerable<string> assigneeUsernames);
        public Task DeleteAsync(Guid id, string userName);
        public Task RejectTaskAsync(Guid id, string userName);
        //Task GetTaskByIdAsync(int id); don't remember why I wrote this 
    }
}