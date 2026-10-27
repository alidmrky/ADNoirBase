using ADNoirBase.Shared.Application;

namespace ADNoirBase.API.Extensions;

public static class ModuleExtensions
{
    /// <summary>
    /// Tüm assembly'lerdeki IModuleRegistration implementasyonlarını reflection ile bulur ve kaydeder.
    /// Yeni modül eklemek için bu metoda dokunmak gerekmez.
    /// </summary>
    public static IServiceCollection AddModules(
        this IServiceCollection services,
        IConfiguration configuration,
        params Type[] moduleMarkerTypes)
    {
        foreach (var markerType in moduleMarkerTypes)
        {
            var registrations = markerType.Assembly
                .GetTypes()
                .Where(t => typeof(IModuleRegistration).IsAssignableFrom(t)
                            && !t.IsInterface
                            && !t.IsAbstract)
                .Select(Activator.CreateInstance)
                .Cast<IModuleRegistration>();

            foreach (var registration in registrations)
            {
                registration.Register(services, configuration);
            }
        }

        return services;
    }
}
