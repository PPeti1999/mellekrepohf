FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["NotificationWorker.csproj", "./"]
RUN dotnet restore "NotificationWorker.csproj"
COPY . .
RUN dotnet publish "NotificationWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
# A Workernek nem kötelező portot nyitni, de nem árt
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationWorker.dll"]