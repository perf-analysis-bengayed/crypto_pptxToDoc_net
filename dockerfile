# # # Use the official .NET SDK image for building the application
# # FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# # # Set the working directory
# # WORKDIR /src

# # # Copy the project file and restore dependencies
# # COPY ./pptx2doc/pptx2doc.csproj ./pptx2doc/
# # RUN dotnet restore "./pptx2doc/pptx2doc.csproj"

# # # Copy the remaining application files
# # COPY ./pptx2doc ./pptx2doc/

# # # Build the application
# # RUN dotnet publish "./pptx2doc/pptx2doc.csproj" -o /app/publish

# # # Use the official ASP.NET runtime image for running the application
# # FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# # # Install dependencies (LibreOffice & Poppler-utils for PDF conversion)
# # RUN apt-get update && apt-get install -y \
# #     libreoffice \
# #     poppler-utils \
# #     && rm -rf /var/lib/apt/lists/*

# # WORKDIR /app

# # # Copy the published application from the build stage
# # COPY --from=build /app/publish .

# # # Create necessary directories for input and output
# # RUN mkdir -p /app/input /app/output

# # # Expose the port the app runs on
# # EXPOSE 3000

# # # Set the entry point to run the application
# # ENTRYPOINT ["dotnet", "pptx2doc.dll"]


# # Use the official .NET SDK image for building the application
# FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# # Set the working directory
# WORKDIR /src

# # Copy the project file and restore dependencies
# COPY ./pptx2doc/pptx2doc.csproj ./pptx2doc/
# RUN dotnet restore "./pptx2doc/pptx2doc.csproj"

# # Copy the remaining application files
# COPY ./pptx2doc ./pptx2doc/

# # Build the application
# RUN dotnet publish "./pptx2doc/pptx2doc.csproj" -o /app/publish

# # Use the official ASP.NET runtime image for running the application
# FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# # Install dependencies (LibreOffice & Poppler-utils for PDF conversion)
# RUN apt-get update && apt-get install -y \
#     libreoffice \
#     poppler-utils \
#     && rm -rf /var/lib/apt/lists/*

# WORKDIR /app

# # Copy the published application from the build stage
# COPY --from=build /app/publish .

# # Copy the SSL certificate (PFX) into the container
# COPY ssl_certificates/your_certificate.pfx ./ssl_certificates/

# # Create necessary directories for input and output
# RUN mkdir -p /app/input /app/output

# # Expose the port the app runs on
# EXPOSE 3000

# # Set the entry point to run the application
# ENTRYPOINT ["dotnet", "pptx2doc.dll"]



# Utiliser l'image officielle .NET SDK pour compiler l'application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Définir le répertoire de travail
WORKDIR /src

# Copier le fichier projet et restaurer les dépendances
COPY ./pptx2doc/pptx2doc.csproj ./pptx2doc/
RUN dotnet restore "./pptx2doc/pptx2doc.csproj"

# Copier le reste des fichiers de l'application
COPY ./pptx2doc ./pptx2doc/

# Compiler et publier l'application
RUN dotnet publish "./pptx2doc/pptx2doc.csproj" -o /app/publish

# Utiliser l'image runtime ASP.NET pour exécuter l'application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Installer les dépendances (LibreOffice & Poppler-utils pour la conversion PDF)
RUN apt-get update && apt-get install -y \
    libreoffice \
    poppler-utils \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copier l'application publiée depuis l'étape de build
COPY --from=build /app/publish .

# Copier le certificat SSL (PFX) dans le container
COPY ssl_certificates/your_certificate.pfx ./ssl_certificates/

# Créer les répertoires nécessaires pour l'input et l'output
RUN mkdir -p /app/input /app/output

# Exposer les ports (HTTP et HTTPS)
EXPOSE 443

# Définir le point d'entrée
ENTRYPOINT ["dotnet", "pptx2doc.dll"]
