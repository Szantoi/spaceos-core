# syntax=docker/dockerfile:1.4
# ── Build stage ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first for layer-cached restore
COPY ["SpaceOS.Kernel.Api/SpaceOS.Kernel.Api.csproj", "SpaceOS.Kernel.Api/"]
COPY ["SpaceOS.Kernel.Application/SpaceOS.Kernel.Application.csproj", "SpaceOS.Kernel.Application/"]
COPY ["SpaceOS.Kernel.Domain/SpaceOS.Kernel.Domain.csproj", "SpaceOS.Kernel.Domain/"]
COPY ["SpaceOS.Infrastructure/SpaceOS.Infrastructure.csproj", "SpaceOS.Infrastructure/"]

# MSG-NEXUS-011: Use BuildKit cache mount for NuGet packages
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "SpaceOS.Kernel.Api/SpaceOS.Kernel.Api.csproj"

# Copy source and publish
COPY . .
WORKDIR "/src/SpaceOS.Kernel.Api"

# MSG-NEXUS-011: Use BuildKit cache mount for build outputs
RUN --mount=type=cache,target=/root/.nuget/packages \
    --mount=type=cache,target=/src/obj \
    dotnet publish -c Release -o /app/publish --no-restore

# ── Runtime stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Install curl for HEALTHCHECK, then create non-root user (Alpine syntax)
RUN apk add --no-cache curl \
    && addgroup -S appgroup && adduser -S -G appgroup appuser

COPY --chown=appuser:appgroup --from=build /app/publish .

USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "SpaceOS.Kernel.Api.dll"]
