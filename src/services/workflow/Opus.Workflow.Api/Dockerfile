# build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY services/workflow/Opus.Workflow.Api/ services/workflow/Opus.Workflow.Api/
WORKDIR /src/services/workflow/Opus.Workflow.Api
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# run
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Opus.Workflow.Api.dll"]
