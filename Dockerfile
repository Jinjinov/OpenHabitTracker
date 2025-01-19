# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project files and restore dependencies
COPY ["OpenHabitTracker/OpenHabitTracker.csproj", "OpenHabitTracker/"]
COPY ["OpenHabitTracker.Backup/OpenHabitTracker.Backup.csproj", "OpenHabitTracker.Backup/"]
COPY ["OpenHabitTracker.Blazor/OpenHabitTracker.Blazor.csproj", "OpenHabitTracker.Blazor/"]
COPY ["OpenHabitTracker.Blazor.Web/OpenHabitTracker.Blazor.Web.csproj", "OpenHabitTracker.Blazor.Web/"]
COPY ["OpenHabitTracker.EntityFrameworkCore/OpenHabitTracker.EntityFrameworkCore.csproj", "OpenHabitTracker.EntityFrameworkCore/"]
RUN dotnet restore "OpenHabitTracker.Blazor.Web/OpenHabitTracker.Blazor.Web.csproj"

# Copy the remaining files and build the app
COPY . .
WORKDIR "/src/OpenHabitTracker.Blazor.Web"
RUN dotnet build "OpenHabitTracker.Blazor.Web.csproj" -c Release -o /app/build

# Publish the app
RUN dotnet publish "OpenHabitTracker.Blazor.Web.csproj" -c Release -o /app/publish

# Use the official ASP.NET Core runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OpenHT.dll"]
