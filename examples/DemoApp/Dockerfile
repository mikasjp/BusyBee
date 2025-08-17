FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["examples/DemoApp/DemoApp.csproj", "examples/DemoApp/"]
COPY ["src/BusyBee/BusyBee.csproj", "src/BusyBee/"]
RUN dotnet restore "examples/DemoApp/DemoApp.csproj"
COPY . .
WORKDIR "/src/examples/DemoApp"
RUN dotnet build "./DemoApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DemoApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DemoApp.dll"]
