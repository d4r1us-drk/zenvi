# Use the official ASP.NET Core runtime image from Microsoft:
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080  # HTTP port
EXPOSE 8443  # HTTPS port

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Zenvi.Core/Zenvi.Core.csproj", "Zenvi.Core/"]
RUN dotnet restore "Zenvi.Core/Zenvi.Core.csproj"
COPY . .
WORKDIR "/src/Zenvi.Core"
RUN dotnet build "Zenvi.Core.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "Zenvi.Core.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
RUN groupadd -r zenvigroup && useradd -r -g zenvigroup zenviuser
COPY --from=publish /app/publish .
RUN mkdir -p /data/uploads && chown -R zenviuser:zenvigroup /data
RUN mkdir -p /config && chown -R zenviuser:zenvigroup /config
COPY Zenvi.Core/appsettings.json /config/appsettings.json
USER zenviuser
ENTRYPOINT ["dotnet", "Zenvi.Core.dll"]
