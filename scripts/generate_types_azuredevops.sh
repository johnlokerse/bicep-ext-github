#!/bin/bash
set -e

root="$(dirname ${BASH_SOURCE[0]})/.."

dotnet run --project "$root/src/Bicep.Types.AzureDevOps/Bicep.Types.AzureDevOps.csproj" -- --outdir "$root/types-azuredevops"
