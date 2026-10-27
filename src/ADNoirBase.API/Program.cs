using ADNoirBase.API.Extensions;
using ADNoirBase.API.Middleware;
using ADNoirBase.Shared.Infrastructure;
using Identity.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog ──────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// ─── Shared Infrastructure ────────────────────────────────────────────────────
builder.Services.AddSharedInfrastructure(builder.Configuration);

// ─── Modüller ─────────────────────────────────────────────────────────────────
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddModules(
    builder.Configuration,
    typeof(Identity.Application.IdentityApplicationRegistration));

// ─── API ──────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddSwagger();
builder.Services.AddAppCors(builder.Configuration);
builder.Services.AddAppHealthChecks(builder.Configuration);

var app = builder.Build();

// ─── Middleware pipeline ──────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSerilogRequestLogging();
app.UseCors();

if (app.Environment.IsDevelopment())
    app.UseSwaggerUi();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapAppHealthChecks();

app.Run();
