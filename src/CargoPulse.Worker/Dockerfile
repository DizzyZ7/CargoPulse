FROM mcr.microsoft.com/dotnet/sdk:10.0.302-noble AS build
WORKDIR /source
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/CargoPulse.ServiceDefaults/CargoPulse.ServiceDefaults.csproj src/CargoPulse.ServiceDefaults/packages.lock.json src/CargoPulse.ServiceDefaults/
COPY src/CargoPulse.Worker/CargoPulse.Worker.csproj src/CargoPulse.Worker/packages.lock.json src/CargoPulse.Worker/
RUN dotnet restore src/CargoPulse.Worker/CargoPulse.Worker.csproj --locked-mode
COPY src/CargoPulse.ServiceDefaults/ src/CargoPulse.ServiceDefaults/
COPY src/CargoPulse.Worker/ src/CargoPulse.Worker/
RUN dotnet publish src/CargoPulse.Worker/CargoPulse.Worker.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.10-noble-chiseled-extra AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "CargoPulse.Worker.dll"]
