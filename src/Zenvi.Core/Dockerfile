FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Zenvi.Core/Zenvi.Core.csproj", "Zenvi.Core/"]
COPY ["Zenvi.Shared/Zenvi.Shared.csproj", "Zenvi.Shared/"]
RUN dotnet restore "Zenvi.Core/Zenvi.Core.csproj"
COPY . .
WORKDIR "/src/Zenvi.Core"
RUN dotnet build "Zenvi.Core.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Zenvi.Core.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Zenvi.Core.dll"]
