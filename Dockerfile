FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY *.sln .
COPY src/NotificationService.API/*.csproj ./src/NotificationService.API/
COPY src/NotificationService.Core/*.csproj ./src/NotificationService.Core/
COPY src/NotificationService.Infrastructure/*.csproj ./src/NotificationService.Infrastructure/
COPY src/NotificationService.Workers/*.csproj ./src/NotificationService.Workers/

RUN dotnet restore

COPY . .
RUN dotnet publish src/NotificationService.API -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "NotificationService.API.dll"]
