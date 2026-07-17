# Use the official .NET SDK image to build the app
# The SDK stage always runs on the build machine's own architecture ($BUILDPLATFORM)
# and cross-compiles for the target architecture ($TARGETARCH: amd64/arm64) - no emulation
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
LABEL org.opencontainers.image.source=https://github.com/Jinjinov/OpenHabitTracker

WORKDIR /src

# Copy the project files and restore dependencies for the target architecture
COPY ["OpenHabitTracker/OpenHabitTracker.csproj", "OpenHabitTracker/"]
COPY ["OpenHabitTracker.Backup/OpenHabitTracker.Backup.csproj", "OpenHabitTracker.Backup/"]
COPY ["OpenHabitTracker.Blazor/OpenHabitTracker.Blazor.csproj", "OpenHabitTracker.Blazor/"]
COPY ["OpenHabitTracker.Blazor.Web/OpenHabitTracker.Blazor.Web.csproj", "OpenHabitTracker.Blazor.Web/"]
COPY ["OpenHabitTracker.EntityFrameworkCore/OpenHabitTracker.EntityFrameworkCore.csproj", "OpenHabitTracker.EntityFrameworkCore/"]
RUN dotnet restore "OpenHabitTracker.Blazor.Web/OpenHabitTracker.Blazor.Web.csproj" -a $TARGETARCH

# Copy the remaining files and publish the app (publish builds too)
COPY . .
WORKDIR "/src/OpenHabitTracker.Blazor.Web"
RUN dotnet publish "OpenHabitTracker.Blazor.Web.csproj" -c Release -a $TARGETARCH --no-restore -o /app/publish

# Use the official ASP.NET Core runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OpenHT.dll"]
