# build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY building-blocks/Opus.Storage/ building-blocks/Opus.Storage/
COPY services/files/Opus.Files.Api/ services/files/Opus.Files.Api/
WORKDIR /src/services/files/Opus.Files.Api
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# run
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Opus.Files.Api.dll"]
