# Etapa de compilación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivo de proyecto y restaurar dependencias NuGet
COPY ["FactorGym.Web.csproj", "./"]
RUN dotnet restore "./FactorGym.Web.csproj"

# Copiar el resto del código y compilar en modo Release
COPY . .
RUN dotnet publish "FactorGym.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa final de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Instalar dependencias nativas y fuentes requeridas por QuestPDF (SkiaSharp) en Linux
RUN apt-get update && apt-get install -y --no-install-recommends \
    libfontconfig1 \
    libfreetype6 \
    fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Puerto por defecto para ASP.NET Core 8
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FactorGym.Web.dll"]
