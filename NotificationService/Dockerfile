FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/NotificationService.API/NotificationService.API.csproj", "src/NotificationService.API/"]
COPY ["src/NotificationService.Application/NotificationService.Application.csproj", "src/NotificationService.Application/"]
COPY ["src/NotificationService.Infrastructure/NotificationService.Infrastructure.csproj", "src/NotificationService.Infrastructure/"]
COPY ["src/NotificationService.Domain/NotificationService.Domain.csproj", "src/NotificationService.Domain/"]
RUN dotnet restore "src/NotificationService.API/NotificationService.API.csproj"
COPY . .
WORKDIR "/src/src/NotificationService.API"
RUN dotnet build "NotificationService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NotificationService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.API.dll"]
