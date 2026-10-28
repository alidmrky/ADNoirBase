# ─── Build stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ADNoirBase.sln .
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/ src/

RUN dotnet restore src/ADNoirBase.API/ADNoirBase.API.csproj

WORKDIR /src/src/ADNoirBase.API
RUN dotnet publish -c Release -o /app/publish --no-restore

# ─── Runtime stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ADNoirBase.API.dll"]
