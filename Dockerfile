# Etapa 1: Construcción
# FIJATE LAS BARRAS: mcr.microsoft.com/dotnet/sdk:8.0
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos el archivo de proyecto (asegurate que se llame así o cambialo)
COPY ["FichajApp.csproj", "./"]
RUN dotnet restore "FichajApp.csproj"

# Copiamos el resto del código y publicamos
COPY . .
RUN dotnet publish "FichajApp.csproj" -c Release -o /app/publish

# Etapa 2: Ejecución
# FIJATE LAS BARRAS: mcr.microsoft.com/dotnet/aspnet:8.0
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Configuración para Render
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

# Asegurate de que el nombre del DLL sea el de tu proyecto
ENTRYPOINT ["dotnet", "FichajApp.dll"]