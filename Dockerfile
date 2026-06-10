# Etapa de Compilación
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar archivo del proyecto y restaurar dependencias
COPY ["Control_flota.csproj", "./"]
RUN dotnet restore "Control_flota.csproj"

# Copiar el resto del código fuente y compilar la aplicación en modo Release
COPY . .
RUN dotnet publish "Control_flota.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa de Ejecución (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Copiar la base de datos SQLite con los datos existentes al directorio de ejecución
COPY app.db .

# Configurar ASP.NET Core para escuchar en el puerto 8080 (puerto por defecto para contenedores en Render)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Control_flota.dll"]
