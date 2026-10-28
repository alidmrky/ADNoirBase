namespace ADNoirBase.API.Middleware;

/// <summary>
/// Tarayıcı tabanlı istemciler için temel güvenlik başlıklarını ekler.
/// Pipeline'ın en başında çalışır, böylece hata yanıtları da başlıkları taşır.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    /// <summary>
    /// API yalnızca JSON döner; hiçbir alt kaynak yüklemesine izin verilmez.
    /// </summary>
    private const string ApiContentSecurityPolicy =
        "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

    /// <summary>
    /// Swagger UI kendi stillerini ve script'lerini inline çalıştırdığı için
    /// ayrı bir politikaya ihtiyaç duyar. Yalnızca Development'ta sunulur.
    /// </summary>
    private const string SwaggerContentSecurityPolicy =
        "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; " +
        "script-src 'self' 'unsafe-inline'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

    private const string PermissionsPolicy =
        "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), " +
        "microphone=(), payment=(), usb=()";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";
        headers["Permissions-Policy"] = PermissionsPolicy;
        headers["Content-Security-Policy"] = IsSwaggerRequest(context.Request.Path)
            ? SwaggerContentSecurityPolicy
            : ApiContentSecurityPolicy;

        return _next(context);
    }

    private static bool IsSwaggerRequest(PathString path) =>
        path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase);
}
