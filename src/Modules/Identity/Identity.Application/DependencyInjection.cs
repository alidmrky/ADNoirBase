using ADNoirBase.Shared.Application;
using FluentValidation;
using Identity.Application.Services.Auth;
using Identity.Application.Services.User;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application;

public sealed class IdentityApplicationRegistration : IModuleRegistration
{
    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserManagementService, UserManagementService>();

        // Bu assembly'deki tüm validator'ları kayıt et
        services.AddValidatorsFromAssemblyContaining<IdentityApplicationRegistration>();

        return services;
    }
}
