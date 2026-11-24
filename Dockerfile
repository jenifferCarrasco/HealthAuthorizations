FROM mcr.microsoft.com/dotnet/sdk:10.0 AS tooling
USER root
WORKDIR /src

ENV HOME=/home/app \
    DOTNET_CLI_HOME=/home/app \
    DOTNET_CLI_TELEMETRY_OPTOUT=1

RUN mkdir -p /home/app/.microsoft/usersecrets \
    && chown -R app:app /src /home/app

COPY --chown=app:app .config/dotnet-tools.json .config/
COPY --chown=app:app src/ src/

USER $APP_UID

RUN dotnet tool restore \
    && dotnet restore src/HealthAuthorization.API/HealthAuthorization.API.csproj \
    && dotnet publish src/HealthAuthorization.API/HealthAuthorization.API.csproj \
        --configuration Release --no-restore --output /src/publish -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
USER root
WORKDIR /app
ENV HOME=/home/app \
    ASPNETCORE_HTTP_PORTS=8080

RUN mkdir -p /home/app/.microsoft/usersecrets \
    && chown -R app:app /home/app

COPY --from=tooling /src/publish/ ./

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "HealthAuthorization.API.dll"]
