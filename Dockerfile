# Use the ASP.NET Core 7 base image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

# Install libgdiplus
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgdiplus \
    && rm -rf /var/lib/apt/lists/*

# Use the SDK image to build your application
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY . .

# Restore dependencies
RUN dotnet restore "2cpbackend.csproj" --disable-parallel
# Build the application
RUN dotnet build "2cpbackend.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "2cpbackend.csproj" -c Release -o /app/publish

# Create the final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set the entry point command
CMD ["dotnet", "2cpbackend.dll"]

