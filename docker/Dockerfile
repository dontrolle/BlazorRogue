FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet publish BlazorRogue.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Shown as a small corner badge in the app (see BuildInfo.cs) - passed via --build-arg by the
# workflow that publishes the image; a plain `docker build` with no args falls back to "dev".
ARG BLAZORROGUE_VERSION=dev
ARG BLAZORROGUE_BUILD_DATE=unknown
ENV BLAZORROGUE_VERSION=$BLAZORROGUE_VERSION
ENV BLAZORROGUE_BUILD_DATE=$BLAZORROGUE_BUILD_DATE

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "BlazorRogue.dll"]
