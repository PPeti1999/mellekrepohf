#FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
 #   WORKDIR /app
  #  COPY AuthService.csproj ./
   # RUN dotnet restore
   # COPY . ./
    #RUN dotnet publish -c Release -o /out

    #FROM mcr.microsoft.com/dotnet/aspnet:8.0
    #WORKDIR /app
    #COPY --from=build /out .
    #ENTRYPOINT ["dotnet", "AuthService.dll"]
    FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
    WORKDIR /src
    COPY ["TicketMaster.sln", "./"]
    COPY ["AuthService/AuthService.csproj", "AuthService/"]
    RUN dotnet restore "AuthService/AuthService.csproj"
    COPY . .
    WORKDIR "/src/AuthService"
    RUN dotnet build "AuthService.csproj" -c Release -o /app/build
    
    FROM build AS publish
    RUN dotnet publish "AuthService.csproj" -c Release -o /app/publish /p:UseAppHost=false
    
    FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
    WORKDIR /app
    COPY --from=publish /app/publish .
    ENTRYPOINT ["dotnet", "AuthService.dll"]