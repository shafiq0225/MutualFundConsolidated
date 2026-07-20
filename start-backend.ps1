# Run from workspace root.
# Launches the consolidated monolithic API project.

$root = $PSScriptRoot
$projectPath = Join-Path $root "MutualFund.ConsolidatedAPI"

Write-Host "Starting Consolidated Monolithic API..." -ForegroundColor Cyan
dotnet run --project $projectPath --launch-profile https
