# Stage 1: Build environment
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy solution and project files for layer caching
COPY HuyetMach175.slnx ./
COPY src/HuyetMach175.SharedKernel/HuyetMach175.SharedKernel.csproj src/HuyetMach175.SharedKernel/
COPY src/HuyetMach175.Modules.Auth/HuyetMach175.Modules.Auth.csproj src/HuyetMach175.Modules.Auth/
COPY src/HuyetMach175.Modules.Audit/HuyetMach175.Modules.Audit.csproj src/HuyetMach175.Modules.Audit/
COPY src/HuyetMach175.Modules.Clinical/HuyetMach175.Modules.Clinical.csproj src/HuyetMach175.Modules.Clinical/
COPY src/HuyetMach175.Modules.Donation/HuyetMach175.Modules.Donation.csproj src/HuyetMach175.Modules.Donation/
COPY src/HuyetMach175.Modules.Inventory/HuyetMach175.Modules.Inventory.csproj src/HuyetMach175.Modules.Inventory/
COPY src/HuyetMach175.Api/HuyetMach175.Api.csproj src/HuyetMach175.Api/

# Restore NuGet packages
RUN dotnet restore src/HuyetMach175.Api/HuyetMach175.Api.csproj

# Copy source code and build/publish
COPY src/ src/
RUN dotnet publish src/HuyetMach175.Api/HuyetMach175.Api.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "HuyetMach175.Api.dll"]