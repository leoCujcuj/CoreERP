# Etapa 1: Base de ejecución ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Instalar bash y utilidades para compatibilidad con scripts
USER root
RUN apt-get update && apt-get install -y --no-install-recommends bash dos2unix && rm -rf /var/lib/apt/lists/*

# Etapa 2: Compilación y restauración con SDK
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiar archivo de proyecto y restaurar dependencias
COPY ["UsuariosAPI/UsuariosAPI.csproj", "UsuariosAPI/"]
RUN dotnet restore "UsuariosAPI/UsuariosAPI.csproj"

# Copiar el código fuente y compilar
COPY ["UsuariosAPI/", "UsuariosAPI/"]
WORKDIR "/src/UsuariosAPI"
RUN dotnet build "UsuariosAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Etapa 3: Publicación
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "UsuariosAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Etapa 4: Imagen final de producción
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Copiar script de espera de conexión TCP
COPY UsuariosAPI/wait-for-it.sh /app/wait-for-it.sh
RUN dos2unix /app/wait-for-it.sh && chmod +x /app/wait-for-it.sh

# Puerto HTTP predeterminado en .NET 8+
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "UsuariosAPI.dll"]
