# Stage 1: Build and publish the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the source and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Expose the port your app runs on (default is 8080 for Render, change if needed)
EXPOSE 8080

ENTRYPOINT ["dotnet", "AetherionContactAPI.dll"]
