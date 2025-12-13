# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY AuthManagement/AuthManagement.csproj AuthManagement/
COPY Directory.Packages.props ./
RUN dotnet restore "AuthManagement/AuthManagement.csproj"

# Copy source code and build
COPY . .
WORKDIR /src/AuthManagement
RUN dotnet build "AuthManagement.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AuthManagement.csproj" -c Release -o /app/publish

# Runtime stage - using nginx for static file serving
FROM nginx:alpine AS runtime

# Install openssl for certificate generation
RUN apk add --no-cache openssl

# Create SSL directory
RUN mkdir -p /etc/nginx/ssl

# Generate self-signed certificate for HTTPS
RUN openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout /etc/nginx/ssl/nginx.key \
    -out /etc/nginx/ssl/nginx.crt \
    -subj "/CN=localhost"

# Copy published Blazor WebAssembly files
COPY --from=publish /app/publish/wwwroot /usr/share/nginx/html

# Copy custom nginx configuration (path relative to build context ./BlazorUI)
COPY AuthManagement/nginx.conf /etc/nginx/nginx.conf

# Expose ports
EXPOSE 80
EXPOSE 443

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
