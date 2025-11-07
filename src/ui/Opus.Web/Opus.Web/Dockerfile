FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["ui/Opus.Web/Opus.Web/Opus.Web.csproj", "ui/Opus.Web/Opus.Web/"]
COPY ["ui/Opus.Web/Opus.Web.Client/Opus.Web.Client.csproj", "ui/Opus.Web/Opus.Web.Client/"]

# Restore dependencies
RUN dotnet restore "ui/Opus.Web/Opus.Web/Opus.Web.csproj"

# Copy all source files
COPY ["ui/Opus.Web/", "ui/Opus.Web/"]

# Build
WORKDIR "/src/ui/Opus.Web/Opus.Web"
RUN dotnet build "Opus.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Opus.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Opus.Web.dll"]
