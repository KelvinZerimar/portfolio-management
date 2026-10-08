# Worker no sirve HTTP (sin ingress en el Container App) - imagen base runtime, no aspnet.

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

# Esta fase se usa para compilar el proyecto
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Worker/Worker.csproj", "Worker/"]
RUN dotnet restore "./Worker/Worker.csproj"
COPY . .
WORKDIR "/src/Worker"
RUN dotnet build "./Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase se usa para publicar el proyecto que se copiará en la fase final.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

# Esta fase se usa en producción
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./Worker"]
