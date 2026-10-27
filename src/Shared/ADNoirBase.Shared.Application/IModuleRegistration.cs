using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ADNoirBase.Shared.Application;

/// <summary>
/// Her modülün implement ettiği DI kayıt arayüzü.
/// API projesi reflection ile tüm assembly'leri tarar ve Register() çağırır —
/// yeni modül eklemek için API projesine dokunmak gerekmez.
/// </summary>
public interface IModuleRegistration
{
    IServiceCollection Register(IServiceCollection services, IConfiguration configuration);
}
