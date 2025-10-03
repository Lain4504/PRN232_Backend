# Dùng hình ảnh chính thức của .NET SDK để build ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sao chép solution file và project files để restore dependencies
COPY AISAM.sln ./
COPY AISAM.API/AISAM.API.csproj AISAM.API/
COPY AISAM.Common/AISAM.Common.csproj AISAM.Common/
COPY AISAM.Data/AISAM.Data.csproj AISAM.Data/
COPY AISAM.Repositories/AISAM.Repositories.csproj AISAM.Repositories/
COPY AISAM.Services/AISAM.Services.csproj AISAM.Services/

# Restore dependencies
RUN dotnet restore

# Sao chép toàn bộ source code
COPY . .

# Build và publish ứng dụng
RUN dotnet publish AISAM.API/AISAM.API.csproj -c Release -o /app/publish

# Dùng hình ảnh .NET Runtime để chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published application từ build stage
COPY --from=build /app/publish .

# Expose port 8080 (hoặc port mà bạn muốn sử dụng)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "AISAM.API.dll"]
