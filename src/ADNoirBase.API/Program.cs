using System.Text.Json.Serialization;
using ADNoirBase.API.Extensions;
using ADNoirBase.API.Hubs;
using ADNoirBase.API.Middleware;
using ADNoirBase.Shared.Infrastructure;
using Identity.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Notification.Application.Delivery;
using Notification.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Kestrel'in sürüm bilgisi sızdırmasını engeller.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// ─── Serilog ──────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// ─── Shared Infrastructure ────────────────────────────────────────────────────
builder.Services.AddSharedInfrastructure(builder.Configuration);

// ─── Modüller ─────────────────────────────────────────────────────────────────
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddNotificationInfrastructure(builder.Configuration);
builder.Services.AddNotificationDeliveryWorker();
builder.Services.AddModules(
    builder.Configuration,
    typeof(Identity.Application.IdentityApplicationRegistration),
    typeof(Notification.Application.NotificationApplicationRegistration));

// ─── API ──────────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();
builder.Services.AddSingleton<IInAppNotificationPublisher, SignalRInAppNotificationPublisher>();
builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.Events ??= new JwtBearerEvents();
    options.Events.OnMessageReceived = context =>
    {
        if (context.Request.Path.StartsWithSegments("/hubs/notifications") &&
            context.Request.Query.TryGetValue("access_token", out var token))
        {
            context.Token = token;
        }

        return Task.CompletedTask;
    };
});
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // ResponseStatus ve MessageSeverity sözleşmenin parçası; istemciler sayı
    // değil isim görmeli, aksi halde enum'a değer eklemek kırıcı olur.
    // ApiJsonOptions.Default bu ayarın aynısını pipeline dışına taşır.
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddSwagger();
builder.Services.AddAppCors(builder.Configuration, builder.Environment);
builder.Services.AddAppHealthChecks(builder.Configuration);
builder.Services.AddAppForwardedHeaders(builder.Configuration);
builder.Services.AddAppRateLimiter(builder.Configuration);

var app = builder.Build();

// ─── Middleware pipeline ──────────────────────────────────────────────────────

// En başta: hata yanıtları da güvenlik başlıklarını taşısın.
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Rate limiting istemci IP'sine göre bölümlendiği için gerçek IP, proxy
// başlıkları çözüldükten sonra okunmalıdır.
app.UseForwardedHeaders();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSerilogRequestLogging();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUi();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Auth'tan sonra: kimliği doğrulanmış istekler kullanıcı başına sayılabilsin.
app.UseRateLimiter();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications", options => options.CloseOnAuthenticationExpiration = true);
app.MapAppHealthChecks();

app.Run();
