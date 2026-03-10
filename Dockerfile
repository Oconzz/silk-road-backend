# NMJ Backend API Dockerfile
# Multi-stage build for .NET 8

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY APIServer.sln .
COPY APIServer/APIServer.csproj APIServer/
COPY Business/Business.csproj Business/

# Restore dependencies with retry logic for network issues
RUN dotnet restore --disable-parallel || \
    (sleep 5 && dotnet restore --disable-parallel) || \
    (sleep 10 && dotnet restore --disable-parallel)

# Copy source code
COPY . .

# Build the application
WORKDIR /src/APIServer
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - usando SDK para permitir comandos dotnet ef
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN useradd --create-home --shell /bin/bash appuser && \
    mkdir -p /home/appuser/.dotnet/tools && \
    chown -R appuser:appuser /home/appuser

# Copy published files
COPY --from=publish /app/publish .

# Copy source files para migrations (necessário para dotnet ef)
COPY --from=build /src /src
RUN chown -R appuser:appuser /src

# Change to non-root user
USER appuser

# Instalar dotnet-ef tool como appuser
RUN dotnet tool install --global dotnet-ef --version 8.0.*
ENV PATH="${PATH}:/home/appuser/.dotnet/tools"

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ConnectionString=DockerConnectionString

# Expose port
EXPOSE 5000

# Start the application
ENTRYPOINT ["dotnet", "APIServer.dll"]
