using Microsoft.EntityFrameworkCore;
using System;
using TaskSchedulingApp.Data;

namespace TaskSchedulingApp.Configurations
{
    public static class DataConfiguration
    {
        public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<DataContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            return services;
        }
    }
}