FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AccountService.API/AccountService.API.csproj", "src/AccountService.API/"]
COPY ["src/AccountService.Application/AccountService.Application.csproj", "src/AccountService.Application/"]
COPY ["src/AccountService.Infrastructure/AccountService.Infrastructure.csproj", "src/AccountService.Infrastructure/"]
COPY ["src/AccountService.Domain/AccountService.Domain.csproj", "src/AccountService.Domain/"]
RUN dotnet restore "src/AccountService.API/AccountService.API.csproj"
COPY . .
WORKDIR "/src/src/AccountService.API"
RUN dotnet build "AccountService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AccountService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AccountService.API.dll"]
