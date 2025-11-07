# build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY building-blocks/Opus.Security/ building-blocks/Opus.Security/
COPY services/gateway/Opus.Gateway.Api/ services/gateway/Opus.Gateway.Api/
WORKDIR /src/services/gateway/Opus.Gateway.Api
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# run
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Opus.Gateway.Api.dll"]
