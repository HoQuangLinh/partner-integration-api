FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /source

COPY ["Directory.Build.props", "."]
COPY ["src/PartnerIntegration.Domain/PartnerIntegration.Domain.csproj", "src/PartnerIntegration.Domain/"]
COPY ["src/PartnerIntegration.Application/PartnerIntegration.Application.csproj", "src/PartnerIntegration.Application/"]
COPY ["src/PartnerIntegration.Infrastructure/PartnerIntegration.Infrastructure.csproj", "src/PartnerIntegration.Infrastructure/"]
COPY ["src/PartnerIntegration.Api/PartnerIntegration.Api.csproj", "src/PartnerIntegration.Api/"]
RUN dotnet restore "src/PartnerIntegration.Api/PartnerIntegration.Api.csproj"

FROM restore AS build
COPY src/ src/
RUN dotnet publish "src/PartnerIntegration.Api/PartnerIntegration.Api.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM restore AS test
COPY ["PartnerIntegration.sln", "."]
COPY ["tests/PartnerIntegration.UnitTests/PartnerIntegration.UnitTests.csproj", "tests/PartnerIntegration.UnitTests/"]
COPY ["tests/PartnerIntegration.IntegrationTests/PartnerIntegration.IntegrationTests.csproj", "tests/PartnerIntegration.IntegrationTests/"]
RUN dotnet restore "PartnerIntegration.sln"

COPY src/ src/
COPY tests/ tests/
CMD ["sh", "tests/run-tests.sh"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/data-protection-keys \
    && chown "$APP_UID:$APP_UID" /app/data-protection-keys

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=5 \
    CMD ["wget", "--spider", "-q", "http://127.0.0.1:8080/health"]

ENTRYPOINT ["dotnet", "PartnerIntegration.Api.dll"]
