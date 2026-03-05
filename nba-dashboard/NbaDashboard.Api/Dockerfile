FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["NbaDashboard.Api/NbaDashboard.Api.csproj", "NbaDashboard.Api/"]
COPY ["NbaDashboard.Core/NbaDashboard.Core.csproj", "NbaDashboard.Core/"]
COPY ["NbaDashboard.Infrastructure/NbaDashboard.Infrastructure.csproj", "NbaDashboard.Infrastructure/"]
RUN dotnet restore "NbaDashboard.Api/NbaDashboard.Api.csproj"

COPY . .
WORKDIR "/src/NbaDashboard.Api"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NbaDashboard.Api.dll"]
