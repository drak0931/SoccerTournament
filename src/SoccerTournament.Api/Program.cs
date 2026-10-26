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
await app.Services.InitializeDatabaseAsync();

// Orden: correlación → log del request (con duración) → manejo de errores → endpoints
app.UseObservability();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Expone Program para las pruebas de integración HTTP (WebApplicationFactory)
public partial class Program;
