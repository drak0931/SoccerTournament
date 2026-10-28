using SoccerTournament.Api;
using SoccerTournament.Api.Observability;
using SoccerTournament.Application;
using SoccerTournament.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddObservability(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPresentation(builder.Configuration);

var app = builder.Build();

// Aplica scripts de BD pendientes y el seed antes de aceptar requests
try
{
    await app.Services.InitializeDatabaseAsync();
}
catch (Exception exception)
{
    // Límite del host: sin BD la API no puede atender. Se registra y se termina con código 1
    // (en Docker, la política de reinicio vuelve a intentar el arranque)
    app.Logger.LogCritical(exception, "No se pudo inicializar la base de datos; la API se detiene");
    return 1;
}

// Orden: correlación → log del request (con duración) → manejo de errores → endpoints
app.UseObservability();
app.UseExceptionHandler();

// Swagger en desarrollo, o donde se habilite explícitamente (docker-compose para la evaluación)
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
return 0;

// Expone Program para las pruebas de integración HTTP (WebApplicationFactory)
public partial class Program;
