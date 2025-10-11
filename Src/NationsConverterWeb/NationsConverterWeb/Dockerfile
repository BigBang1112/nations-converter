# Debug phase
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Publish phase
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS publish
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH
ARG APPNAME=NationsConverterWeb
RUN apt-get update && apt-get install -y git python3
WORKDIR /src
COPY .git/ ./.git/
RUN dotnet workload install wasm-tools
RUN dotnet nuget add source https://nuget.gbx.tools/v3/index.json --name nuget.gbx.tools

# copy csproj and restore as distinct layers
COPY Src/$APPNAME/$APPNAME/*.csproj $APPNAME/$APPNAME/
COPY Src/$APPNAME/$APPNAME.Client/*.csproj $APPNAME/$APPNAME.Client/
COPY Src/NationsConverterShared/*.csproj NationsConverterShared/
RUN dotnet restore $APPNAME/$APPNAME/$APPNAME.csproj -a $TARGETARCH

# copy and publish app and libraries
COPY Src/$APPNAME/$APPNAME $APPNAME/$APPNAME
COPY Src/$APPNAME/$APPNAME.Client $APPNAME/$APPNAME.Client
COPY Src/NationsConverterShared NationsConverterShared
RUN dotnet publish $APPNAME/$APPNAME -c $BUILD_CONFIGURATION -a $TARGETARCH -o /app --no-restore


# Final phase
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled AS final
EXPOSE 8080
EXPOSE 8081
WORKDIR /app
COPY --from=publish /app .
USER $APP_UID
ENTRYPOINT ["./NationsConverterWeb"]