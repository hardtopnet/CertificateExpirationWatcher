# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /app

# Copy the project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the application files
COPY . ./

# Build the application for Linux
RUN dotnet publish -c Release -r linux-x64 -o out --self-contained false

# Use the official .NET runtime image to run the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set the working directory
WORKDIR /app

# Copy the built application from the build stage
COPY --from=build /app/out .

# Copy the config.json file
COPY config.json .

# Expose port 8793
EXPOSE 8793

# Set the entry point to run the application
ENTRYPOINT ["dotnet", "CertificateExpirationWatcher.dll"]