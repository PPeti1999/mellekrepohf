# 1. Fázis: Build (SDK képfájl használata a fordításhoz)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Csak a projektfájlt másoljuk először (cache miatt gyorsabb lesz a rebuild)
COPY ["CatalogService.csproj", "./"]
RUN dotnet restore "CatalogService.csproj"

# A többi fájl másolása és build
COPY . .
RUN dotnet publish "CatalogService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Fázis: Futás (Kisebb, Runtime képfájl)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CatalogService.dll"]