# 1. Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["STD_Database.csproj", "./"]
RUN dotnet restore "STD_Database.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "STD_Database.csproj" -c Release -o /app/publish

# 2. Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy from build
COPY --from=build /app/publish .

# Expose app ports (internal)
EXPOSE 5241
EXPOSE 7002

# Entry point
ENTRYPOINT ["dotnet", "STD_Database.dll"]