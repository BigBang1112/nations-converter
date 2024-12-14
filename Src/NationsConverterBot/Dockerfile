FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG TARGETARCH
WORKDIR /src

# Copy project file and restore as distinct layers
COPY --link Src/NationsConverter/*.csproj NationsConverter/
COPY --link Src/NationsConverterBot/*.csproj NationsConverterBot/
RUN dotnet restore -a $TARGETARCH NationsConverterBot/NationsConverterBot.csproj

# Copy source code and publish app
COPY --link Src/NationsConverter/ NationsConverter/
COPY --link Src/NationsConverterBot/ NationsConverterBot/

WORKDIR /src/NationsConverterBot
RUN dotnet publish -a $TARGETARCH --no-restore -o /app

# Enable globalization and time zones:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/enable-globalization.md
# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine

ENV \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8
RUN apk add --no-cache \
    icu-data-full \
    icu-libs

WORKDIR /app
COPY --link --from=build /app .
USER $APP_UID
ENTRYPOINT ["./NationsConverterBot"]