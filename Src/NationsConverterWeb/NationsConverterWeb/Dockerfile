# Debug phase
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Publish phase
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

RUN dotnet nuget add source https://nuget.gbx.tools/v3/index.json --name nuget.gbx.tools

COPY .git .

COPY Src/NationsConverterWeb /src/NationsConverterWeb
COPY Src/NationsConverterShared /src/NationsConverterShared

WORKDIR /src/NationsConverterWeb/NationsConverterWeb

ARG TARGETARCH

RUN dotnet publish -a ${TARGETARCH/amd64/x64} --self-contained false -o /app
    
# Final phase
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

COPY --from=build /app .

USER $APP_UID
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R $APP_UID /home/app/.aspnet
ENTRYPOINT ["dotnet", "NationsConverterWeb.dll"]