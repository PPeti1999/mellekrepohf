#FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
#WORKDIR /src
#COPY ["BookingService.csproj", "./"]
#RUN dotnet restore "BookingService.csproj"
#COPY . .
#RUN dotnet publish "BookingService.csproj" -c Release -o /app/publish /p:UseAppHost=false

#FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
#WORKDIR /app
#EXPOSE 8080
#COPY --from=build /app/publish .
#ENTRYPOINT ["dotnet", "BookingService.dll"]
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# CSAK a BookingService-t másoljuk (és a Solution-t ha van)
COPY ["TicketMaster.sln", "./"]
COPY ["BookingService/BookingService.csproj", "BookingService/"]

# Restore csak erre a projektre
RUN dotnet restore "BookingService/BookingService.csproj"

# A teljes forráskód bemásolása
COPY . .
WORKDIR "/src/BookingService"
RUN dotnet build "BookingService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BookingService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookingService.dll"]