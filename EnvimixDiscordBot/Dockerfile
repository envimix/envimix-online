FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base

ENV \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8

RUN apk add --no-cache \
    icu-data-full \
    icu-libs \
    tzdata

WORKDIR /app
USER $APP_UID

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

COPY EnvimixDiscordBot/. /src

WORKDIR /src

ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet build -c $BUILD_CONFIGURATION -a ${TARGETARCH/amd64/x64} --self-contained false -o /app/build

FROM build AS publish
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish -c $BUILD_CONFIGURATION -a ${TARGETARCH/amd64/x64} --use-current-runtime --self-contained false -o /app/publish

FROM base AS final
COPY --from=publish /app/publish .


ENTRYPOINT ["dotnet", "EnvimixDiscordBot.dll"]
