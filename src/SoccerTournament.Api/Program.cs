var builder = WebApplication.CreateBuilder(args);

// Etapa 0: esqueleto mínimo. Logging estructurado, tracing, CQRS e infraestructura se agregan en etapas siguientes.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
