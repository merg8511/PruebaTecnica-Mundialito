using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Mundialito.Api.Middlewares;
using Mundialito.Application.Features.Matches;
using Mundialito.Application.Features.Players;
using Mundialito.Application.Features.Teams;
using Mundialito.Infrastructure;
using Mundialito.Infrastructure.Persistence;
using Mundialito.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────────
// MVC / Controllers
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ─────────────────────────────────────────────────────────────────────────────
// Swagger — solo en Development
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Mundialito de Fútbol Corporativo — API",
        Version     = "v1",
        Description = "Backend REST para la gestión del torneo corporativo de fútbol."
    });

    // Soporte para el header Idempotency-Key en Swagger UI
    c.AddSecurityDefinition("IdempotencyKey", new OpenApiSecurityScheme
    {
        Name        = "Idempotency-Key",
        In          = ParameterLocation.Header,
        Type        = SecuritySchemeType.ApiKey,
        Description = "Clave de idempotencia requerida en todos los POST."
    });
});

// ─────────────────────────────────────────────────────────────────────────────
// Infrastructure layer (EF Core write + Dapper read + repositories + UoW)
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ─────────────────────────────────────────────────────────────────────────────
// Application layer — Use Cases (write side)
// Registrar como Scoped porque dependen de UoW/repos que también son Scoped.
// ─────────────────────────────────────────────────────────────────────────────

// Teams
builder.Services.AddScoped<CreateTeamUseCase>();
builder.Services.AddScoped<UpdateTeamUseCase>();
builder.Services.AddScoped<DeleteTeamUseCase>();

// Players
builder.Services.AddScoped<CreatePlayerUseCase>();
builder.Services.AddScoped<UpdatePlayerUseCase>();
builder.Services.AddScoped<DeletePlayerUseCase>();

// Matches & Results
builder.Services.AddScoped<CreateMatchUseCase>();
builder.Services.AddScoped<RecordMatchResultUseCase>();

// ─────────────────────────────────────────────────────────────────────────────
// Build the app
// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ─────────────────────────────────────────────────────────────────────────────
// DB Bootstrap: migrate + seed (with retry for Docker / cold SQL Server starts)
// ─────────────────────────────────────────────────────────────────────────────
await BootstrapDatabaseAsync(app);

// ─────────────────────────────────────────────────────────────────────────────
// Middleware pipeline — ORDEN IMPORTA:
// 1) ExceptionHandling (más externo posible para capturar todo)
// 2) Observability (traceId/correlationId/elapsedMs — debe ir antes de routing)
// ─────────────────────────────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ObservabilityMiddleware>();

// ─────────────────────────────────────────────────────────────────────────────
// Swagger — solo en Development
// ─────────────────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mundialito API v1");
        c.RoutePrefix = string.Empty; // Swagger en la raíz
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Aplica migraciones EF Core y ejecuta el seed inicial.
///
/// Política de retry:
/// - SOLO reintenta errores de conectividad (SqlException, TimeoutException,
///   DbUpdateException cuya causa raíz es SqlException).
/// - Si ocurre InvalidOperationException (bug del seed / factory),
///   falla INMEDIATAMENTE sin reintentar — el error es de programación,
///   no de infraestructura transitoria.
/// </summary>
static async Task BootstrapDatabaseAsync(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    const int maxRetries   = 10;
    const int delaySeconds = 6;

    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<MundialitoDbContext>();

            logger.LogInformation("DB bootstrap attempt {Attempt}/{Max}...", attempt, maxRetries);

            // Apply pending EF migrations
            await db.Database.MigrateAsync();
            logger.LogInformation("Migrations applied successfully.");

            // Run seed (no-op if data already exists)
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();

            logger.LogInformation("DB bootstrap complete.");
            return; // success — exit retry loop
        }
        catch (InvalidOperationException ex)
        {
            // Programming error in seed (factory returned null, coherence mismatch, etc.).
            // Do NOT retry — retrying won't fix a code bug.
            logger.LogCritical(
                ex,
                "Seed failed due to a programming error. Aborting immediately (no retry). " +
                "Fix the seed data and redeploy.");
            throw; // let the process crash — container will be restarted by orchestrator
        }
        catch (Exception ex) when (IsTransientDatabaseError(ex) && attempt < maxRetries)
        {
            // Transient connectivity issue (SQL Server not yet ready in Docker)
            logger.LogWarning(
                ex,
                "DB not ready yet (attempt {Attempt}/{Max}). " +
                "Retrying in {Delay}s... ({ExType}: {ExMsg})",
                attempt, maxRetries, delaySeconds,
                ex.GetType().Name, ex.Message);

            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
    }

    // Final attempt — let any exception propagate; container will restart
    logger.LogWarning(
        "All {Max} DB bootstrap attempts exhausted. Making one last attempt (will throw on failure).",
        maxRetries);

    await using var lastScope = app.Services.CreateAsyncScope();
    var lastDb     = lastScope.ServiceProvider.GetRequiredService<MundialitoDbContext>();
    var lastSeeder = lastScope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await lastDb.Database.MigrateAsync();
    await lastSeeder.SeedAsync();
}

/// <summary>
/// Returns true for exceptions that indicate the DB is temporarily unavailable
/// (network not yet up, SQL Server still initializing, login timeout, etc.).
/// Returns false for programming errors that should NOT be retried.
/// </summary>
static bool IsTransientDatabaseError(Exception ex)
{
    // SqlException covers: connection refused, login timeout, server not ready
    if (ex is SqlException) return true;

    // EF wraps some SQL failures in DbUpdateException
    if (ex is DbUpdateException dbe && dbe.InnerException is SqlException) return true;

    // Generic timeout
    if (ex is TimeoutException) return true;

    // Task/operation cancelled (usually from CancellationToken on timeout)
    if (ex is OperationCanceledException) return true;

    return false;
}
