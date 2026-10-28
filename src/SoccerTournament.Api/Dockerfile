# Imagen de la API. Contexto de build: raíz del repositorio (docker-compose.yml).

# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0.426 AS build
WORKDIR /src

# Primero solo los archivos de proyecto: la restauración se cachea mientras no cambien las dependencias
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/SoccerTournament.Domain/SoccerTournament.Domain.csproj src/SoccerTournament.Domain/
COPY src/SoccerTournament.Application/SoccerTournament.Application.csproj src/SoccerTournament.Application/
COPY src/SoccerTournament.Infrastructure/SoccerTournament.Infrastructure.csproj src/SoccerTournament.Infrastructure/
COPY src/SoccerTournament.Api/SoccerTournament.Api.csproj src/SoccerTournament.Api/
RUN dotnet restore src/SoccerTournament.Api/SoccerTournament.Api.csproj

COPY src/ src/
RUN dotnet publish src/SoccerTournament.Api/SoccerTournament.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0.32 AS runtime

# curl solo para el healthcheck del contenedor
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    TZ=UTC

EXPOSE 8080

# Usuario sin privilegios incluido en las imágenes oficiales de .NET 8
USER app

HEALTHCHECK --interval=10s --timeout=5s --start-period=60s --retries=6 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "SoccerTournament.Api.dll"]
