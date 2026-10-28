using System.Globalization;
using System.Net;
using ADNoirBase.API.Configuration;
using Microsoft.AspNetCore.HttpOverrides;

// ForwardedHeadersOptions.KnownNetworks bu tipi bekler; System.Net.IPNetwork ile karışmasın.
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace ADNoirBase.API.Extensions;

public static class ProxyExtensions
{
    public static IServiceCollection AddAppForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration
            .GetSection(ForwardedHeadersSettings.SectionName)
            .Get<ForwardedHeadersSettings>() ?? new ForwardedHeadersSettings();

        if (!settings.Enabled)
        {
            return services;
        }

        if (settings.KnownProxies.Count == 0 && settings.KnownNetworks.Count == 0)
        {
            throw new InvalidOperationException(
                "ForwardedHeaders:Enabled true ise ForwardedHeaders:KnownProxies veya " +
                "ForwardedHeaders:KnownNetworks dolu olmalıdır. Aksi halde her istemci " +
                "X-Forwarded-For başlığıyla kendi IP'sini taklit edip rate limit'i aşabilir.");
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Varsayılan güvenilir listeler temizlenmeli; aksi halde loopback
            // her zaman güvenilir sayılır ve açık liste anlamını yitirir.
            options.KnownProxies.Clear();
            options.KnownNetworks.Clear();

            foreach (var proxy in settings.KnownProxies)
            {
                options.KnownProxies.Add(ParseAddress(proxy));
            }

            foreach (var network in settings.KnownNetworks)
            {
                options.KnownNetworks.Add(ParseNetwork(network));
            }
        });

        return services;
    }

    private static IPAddress ParseAddress(string value) =>
        IPAddress.TryParse(value, out var address)
            ? address
            : throw new InvalidOperationException(
                $"ForwardedHeaders:KnownProxies içindeki '{value}' geçerli bir IP adresi değil.");

    private static IPNetwork ParseNetwork(string value)
    {
        var parts = value.Split('/', 2);

        if (parts.Length != 2 ||
            !IPAddress.TryParse(parts[0], out var prefix) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var length))
        {
            throw new InvalidOperationException(
                $"ForwardedHeaders:KnownNetworks içindeki '{value}' CIDR biçiminde değil, örnek: 10.0.0.0/8");
        }

        return new IPNetwork(prefix, length);
    }
}
