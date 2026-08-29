FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R $APP_UID /home/app/.aspnet
USER $APP_UID
EXPOSE 8080

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

RUN apk add --no-cache git
COPY .git .

COPY EnvimixWebsite/. /src

WORKDIR /src/EnvimixWebsite

ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet build -c $BUILD_CONFIGURATION -a ${TARGETARCH/amd64/x64} --self-contained false -o /app/build

FROM build AS publish
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish -c $BUILD_CONFIGURATION -a ${TARGETARCH/amd64/x64} --self-contained false -o /app/publish

FROM base AS final
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "EnvimixWebsite.dll"]
