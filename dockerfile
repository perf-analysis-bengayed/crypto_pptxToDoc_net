# Use lightweight Python Alpine image
FROM python:3.10-alpine

# Install system dependencies
RUN apk add --no-cache \
    libreoffice \
    libreoffice-impress \
    poppler-utils \
    fontconfig \
    ttf-dejavu \
    ttf-liberation \
    bash \
    && rm -rf /var/cache/apk/*

# Install Python dependencies
COPY requirements.txt /app/requirements.txt
WORKDIR /app
RUN pip install --upgrade pip && pip install -r requirements.txt

# Copy the script and fonts (if any)
COPY app.py . 
COPY /fonts /usr/share/fonts

# Refresh font cache
RUN fc-cache -f -v

# Create necessary directories
RUN mkdir -p /app/pptx_files /app/output

# Define mountable volumes
VOLUME ["/app/pptx_files", "/app/output", "/app/riso_files"]

# Default command to run the script
CMD ["sh", "-c", "python app.py; tail -f /dev/null"]












    
