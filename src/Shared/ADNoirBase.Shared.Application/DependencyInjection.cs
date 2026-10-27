using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ADNoirBase.Shared.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedApplication(
        this IServiceCollection services)
    {
        // FluentValidation validator'larını otomatik kayıt
        // Her modül kendi validator'larını kendi DI extension'ında kaydeder
        return services;
    }
}
