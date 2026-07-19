#!/bin/sh
set -eu

TEST_RESULTS_DIRECTORY=/source/TestResults

mkdir -p "$TEST_RESULTS_DIRECTORY"
find "$TEST_RESULTS_DIRECTORY" -type f -delete
find "$TEST_RESULTS_DIRECTORY" -mindepth 1 -depth -type d -empty -delete

dotnet test tests/PartnerIntegration.UnitTests/PartnerIntegration.UnitTests.csproj \
    --configuration Release \
    --no-restore \
    --logger "trx;LogFileName=unit.trx" \
    --logger "html;LogFileName=unit.html" \
    --results-directory "$TEST_RESULTS_DIRECTORY"

dotnet test tests/PartnerIntegration.IntegrationTests/PartnerIntegration.IntegrationTests.csproj \
    --configuration Release \
    --no-restore \
    --logger "trx;LogFileName=integration.trx" \
    --logger "html;LogFileName=integration.html" \
    --results-directory "$TEST_RESULTS_DIRECTORY"
