# syntax=docker/dockerfile:1

# Compilar la API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/TasksApi/TasksApi.csproj src/TasksApi/
RUN dotnet restore src/TasksApi/TasksApi.csproj

COPY src/TasksApi/ src/TasksApi/
RUN dotnet publish src/TasksApi/TasksApi.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# Imagen final
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Carpeta de la base de datos, con permiso para el usuario "app".
RUN mkdir -p /app/data && chown -R $APP_UID /app/data

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="Data Source=/app/data/tasks.db"

COPY --from=build /app/publish .

VOLUME ["/app/data"]
EXPOSE 8080

# La API corre con un usuario sin permisos de administrador.
USER $APP_UID

ENTRYPOINT ["dotnet", "TasksApi.dll"]
