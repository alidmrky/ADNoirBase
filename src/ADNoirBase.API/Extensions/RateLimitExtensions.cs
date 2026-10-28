using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using ADNoirBase.API.Configuration;
using ADNoirBase.Shared.Contracts.Messages;
using ADNoirBase.Shared.Contracts.Responses;
using Microsoft.AspNetCore.RateLimiting;

namespace ADNoirBase.API.Extensions;

public static class RateLimitExtensions
{
    public static IServiceCollection AddAppRateLimiter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration
            .GetSection(RateLimitSettings.SectionName)
            .Get<RateLimitSettings>() ?? new RateLimitSettings();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => CreatePartition(context, settings.Enabled, settings.Global));

            // Politika limiter'ı kapalıyken de kayıtlı kalmalı; aksi halde
            // [EnableRateLimiting] taşıyan uçlar çalışma zamanında hata verir.
            options.AddPolicy(
                RateLimitPolicies.Authentication,
                context => CreatePartition(context, settings.Enabled, settings.Authentication));

            options.OnRejected = WriteRejectionAsync;
            options.AddPolicy(RateLimitPolicies.NotificationSend,
                context => CreatePartition(context, settings.Enabled, settings.NotificationSend));
        });

        return services;
    }

    private static RateLimitPartition<string> CreatePartition(
        HttpContext context,
        bool enabled,
        RateLimitWindow window)
    {
        var key = ResolveClientKey(context);

        if (!enabled)
        {
            return RateLimitPartition.GetNoLimiter(key);
        }

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = window.PermitLimit,
            Window = TimeSpan.FromSeconds(window.WindowSeconds),
            // Sıraya alma yok: limit aşıldığında bekletmek yerine hemen 429 dön.
            QueueLimit = 0
        });
    }

    /// <summary>
    /// Kimliği doğrulanmış istekler kullanıcı başına sayılır; böylece aynı ofis
    /// ağındaki kullanıcılar tek bir kovayı paylaşmaz. Anonim istekler — ki
    /// login denemeleri bunlardır — IP başına sayılır.
    /// </summary>
    private static string ResolveClientKey(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // IPv6'da saldırgan kendi /64 bloğu içinde adres değiştirebilir. Gerçek
        // koruma için ön tarafta WAF veya IP itibar listesi gerekir.
        var ip = context.Connection.RemoteIpAddress;
        return ip is null ? "ip:unknown" : $"ip:{ip}";
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken ct)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;
        response.ContentType = "application/json";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds)
                .ToString(CultureInfo.InvariantCulture);
        }

        var body = BaseResponse.Failure(ResponseStatus.RateLimited, MessageCodes.Common.RateLimited);

        await response.WriteAsJsonAsync(body, ApiJsonOptions.Default, cancellationToken: ct);
    }
}
