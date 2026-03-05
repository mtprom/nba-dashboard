FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore dependencies first (layer-cached)
COPY ["NbaDashboard.Worker/NbaDashboard.Worker.csproj", "NbaDashboard.Worker/"]
COPY ["NbaDashboard.Core/NbaDashboard.Core.csproj", "NbaDashboard.Core/"]
COPY ["NbaDashboard.Infrastructure/NbaDashboard.Infrastructure.csproj", "NbaDashboard.Infrastructure/"]
RUN dotnet restore "NbaDashboard.Worker/NbaDashboard.Worker.csproj"

COPY . .
WORKDIR "/src/NbaDashboard.Worker"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NbaDashboard.Worker.dll"]
