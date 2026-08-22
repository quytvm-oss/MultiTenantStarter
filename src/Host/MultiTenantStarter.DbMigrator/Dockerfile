#Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /app

# .NET SDK configuration
COPY ["global.json", "./"]

# Central build/package configuration
COPY ["src/Directory.Build.props", "src/"]
COPY ["src/Directory.Packages.props", "src/"]

#Copy BuildingBlocks
COPY ["src/BuildingBlocks/Caching/Caching.csproj", "src/BuildingBlocks/Caching/"]
COPY ["src/BuildingBlocks/Core/Core.csproj", "src/BuildingBlocks/Core/"]
COPY ["src/BuildingBlocks/Jobs/Jobs.csproj", "src/BuildingBlocks/Jobs/"]
COPY ["src/BuildingBlocks/Mailling/Mailling.csproj", "src/BuildingBlocks/Mailling/"]
COPY ["src/BuildingBlocks/MessageBus/MessageBus.csproj", "src/BuildingBlocks/MessageBus/"]
COPY ["src/BuildingBlocks/Persistence/Persistence.csproj", "src/BuildingBlocks/Persistence/"]
COPY ["src/BuildingBlocks/Storage/Storage.csproj", "src/BuildingBlocks/Storage/"]
COPY ["src/BuildingBlocks/Shared/Shared.csproj", "src/BuildingBlocks/Shared/"]
COPY ["src/BuildingBlocks/Web/Web.csproj", "src/BuildingBlocks/Web/"]

#Copy Modules
COPY ["src/Modules/Identity/Modules.Identity/Modules.Identity.csproj", "src/Modules/Identity/Modules.Identity/"]
COPY ["src/Modules/Identity/Modules.Identity.Contracts/Modules.Identity.Contracts.csproj", "src/Modules/Identity/Modules.Identity.Contracts/"]
COPY ["src/Modules/Multitenancy/Modules.Multitenancy/Modules.Multitenancy.csproj", "src/Modules/Multitenancy/Modules.Multitenancy/"]
COPY ["src/Modules/Multitenancy/Modules.Multitenancy.Contracts/Modules.Multitenancy.Contracts.csproj", "src/Modules/Multitenancy/Modules.Multitenancy.Contracts/"]
COPY ["src/Modules/Notifications/Modules.Notifications/Modules.Notifications.csproj", "src/Modules/Notifications/Modules.Notifications/"]
COPY ["src/Modules/Notifications/Modules.Notifications.Contracts/Modules.Notifications.Contracts.csproj", "src/Modules/Notifications/Modules.Notifications.Contracts/"]
COPY ["src/Modules/Auditing/Modules.Auditing/Modules.Auditing.csproj", "src/Modules/Auditing/Modules.Auditing/"]
COPY ["src/Modules/Auditing/Modules.Auditing.Contracts/Modules.Auditing.Contracts.csproj", "src/Modules/Auditing/Modules.Auditing.Contracts/"]
COPY ["src/Modules/Files/Modules.Files/Modules.Files.csproj", "src/Modules/Files/Modules.Files/"]
COPY ["src/Modules/Files/Modules.Files.Contracts/Modules.Files.Contracts.csproj", "src/Modules/Files/Modules.Files.Contracts/"]

# Host
COPY ["src/Host/MultiTenantStarter.Migrations.PostgreSQL/MultiTenantStarter.Migrations.PostgreSQL.csproj","src/Host/MultiTenantStarter.Migrations.PostgreSQL/"]
COPY ["src/Host/MultiTenantStarter.DbMigrator/MultiTenantStarter.DbMigrator.csproj", "src/Host/MultiTenantStarter.DbMigrator/"]

#Restore dependencies
RUN dotnet restore "src/Host/MultiTenantStarter.DbMigrator/MultiTenantStarter.DbMigrator.csproj"

#Build and publish
COPY src src

WORKDIR /app/src/Host/MultiTenantStarter.DbMigrator
RUN dotnet publish -c Release -o /app/publish --no-restore

#Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MultiTenantStarter.DbMigrator.dll"]