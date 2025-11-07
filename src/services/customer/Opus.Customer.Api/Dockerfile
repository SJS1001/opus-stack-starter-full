# build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY building-blocks/Opus.Messaging/ building-blocks/Opus.Messaging/
COPY services/customer/Opus.Customer.Api/ services/customer/Opus.Customer.Api/
WORKDIR /src/services/customer/Opus.Customer.Api
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# run
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Opus.Customer.Api.dll"]
