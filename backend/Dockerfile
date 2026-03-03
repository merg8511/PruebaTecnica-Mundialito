# ── Stage 1: Build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first (layer-cache optimisation)
COPY Mundialito.sln ./
COPY src/Mundialito.Domain/Mundialito.Domain.csproj               src/Mundialito.Domain/
COPY src/Mundialito.Application/Mundialito.Application.csproj     src/Mundialito.Application/
COPY src/Mundialito.Infrastructure/Mundialito.Infrastructure.csproj src/Mundialito.Infrastructure/
COPY src/Mundialito.Api/Mundialito.Api.csproj                     src/Mundialito.Api/

# Restore
RUN dotnet restore Mundialito.sln

# Copy everything else and publish
COPY . .
RUN dotnet publish src/Mundialito.Api/Mundialito.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Use non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser:appuser /app
USER appuser

EXPOSE 8080

ENTRYPOINT ["dotnet", "Mundialito.Api.dll"]
