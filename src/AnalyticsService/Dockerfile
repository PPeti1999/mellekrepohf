FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Másoljuk a .csproj fájlokat és restore-olunk
COPY TicketMaster.sln ./
COPY BookingService/BookingService.csproj BookingService/
COPY CatalogService/CatalogService.csproj CatalogService/
COPY NotificationWorker/NotificationWorker.csproj NotificationWorker/
COPY AnalyticsService/AnalyticsService.csproj AnalyticsService/

RUN dotnet restore AnalyticsService/AnalyticsService.csproj

# Másoljuk a többi fájlt és buildelünk
COPY . ./
WORKDIR /app/AnalyticsService
RUN dotnet publish -c Release -o /out

# Futtató image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "AnalyticsService.dll"]