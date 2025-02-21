# Use the official .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /src

# Copy the project file and restore dependencies
COPY ./pptx2doc/pptx2doc.csproj ./pptx2doc/
RUN dotnet restore "./pptx2doc/pptx2doc.csproj"

# Copy the remaining application files
COPY ./pptx2doc ./pptx2doc/

# Build the application
RUN dotnet publish "./pptx2doc/pptx2doc.csproj" -o /app/publish

# Use the official ASP.NET runtime image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Install dependencies (LibreOffice & Poppler-utils for PDF conversion)
RUN apt-get update && apt-get install -y \
    libreoffice \
    poppler-utils \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /app/publish .

# Create necessary directories for input and output
RUN mkdir -p /app/input /app/output

# Expose the port the app runs on
EXPOSE 3000

# Set the entry point to run the application
ENTRYPOINT ["dotnet", "pptx2doc.dll"]
