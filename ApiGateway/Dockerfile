FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ApiGateway.API/ApiGateway.API.csproj", "src/ApiGateway.API/"]
RUN dotnet restore "src/ApiGateway.API/ApiGateway.API.csproj"
COPY . .
WORKDIR "/src/src/ApiGateway.API"
RUN dotnet build "ApiGateway.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ApiGateway.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.API.dll"]
