using Serilog;
using TaskSchedulingApp.Authorization;
using TaskSchedulingApp.Configurations;
using TaskSchedulingApp.Hubs;
using TaskSchedulingApp.Services;

namespace TaskSchedulingApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSignalR();
            builder.Services.AddSwaggerGen();
            builder.Services.ConfigureDatabase(builder.Configuration);
            builder.Services.ConfigureIdentity(); // Identity Framework configurations
            builder.Services.ConfigureCookie();
            builder.Services.ConfigureJwtToken(builder.Configuration);
            builder.Services.ConfigureSwagger(); // Swagger configuration with Jwt Bearer
            builder.Services.ConfigureServices(); // All the custom services registered
            builder.Services.AddHostedService<ReminderBackgroundService>();

            builder.Services.ConfigureAuthorization(); // contains policy based authorization logic

            builder.Host.UseSerilog((context, config) =>
            {
                config.ReadFrom.Configuration(context.Configuration);
            });

            var app = builder.Build();


            using (var scope = app.Services.CreateScope())
            {
                await scope.ServiceProvider.SeedRolesAsync();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();
            app.MapHub<TaskHub>("/taskHub");

            app.Run();
        }
    }
}