using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using TaskSchedulingApp.Services;
using TaskSchedulingApp.Authorization;
using TaskSchedulingApp.Repositories;
using TaskSchedulingApp.Interfaces;

namespace TaskSchedulingApp.Configurations
{
    public static class ServiceConfigurations
    {
        public static IServiceCollection ConfigureServices(this IServiceCollection services)
        {
            services.AddScoped<TokenService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<ITaskRepository, TaskRepository>();

            return services;
        }
    }
}