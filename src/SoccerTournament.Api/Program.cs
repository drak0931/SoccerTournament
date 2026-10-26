using SoccerTournament.Api;
using SoccerTournament.Application;
using SoccerTournament.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPresentation(builder.Configuration);

var app = builder.Build();

// Aplica scripts de BD pendientes y el seed antes de aceptar requests
await app.Services.InitializeDatabaseAsync();

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
