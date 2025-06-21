using Microsoft.AspNetCore.Authorization;
using TaskSchedulingApp.Authorization;

namespace TaskSchedulingApp.Configurations
{
    public static class AuthorizationConfiguration
    {
        public static IServiceCollection ConfigureAuthorization(this IServiceCollection services)
        {
            services.AddScoped<IAuthorizationHandler, TaskAuthorizationHandler>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("TaskCreator", policy =>
                    policy.Requirements.Add(new TaskCreatorRequirement()));
                options.AddPolicy("TeamMember", policy =>
                    policy.Requirements.Add(new TeamMemberRequirement()));
            });


            return services;
        }
    }
}