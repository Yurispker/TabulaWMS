$root = $PSScriptRoot
$backendPath = Join-Path $root "src\TabulaWMS"
$clientPath = Join-Path $root "client"

if (-not (Test-Path -LiteralPath (Join-Path $backendPath "TabulaWMS.csproj"))) {
    Write-Error "Could not find TabulaWMS.csproj at: $backendPath"
    exit 1
}

if (-not (Test-Path -LiteralPath $clientPath)) {
    Write-Error "Could not find client directory at: $clientPath"
    exit 1
}

Write-Host "Starting C# backend from: $backendPath" -ForegroundColor Cyan
Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location -LiteralPath '$backendPath'; dotnet run"
)

Write-Host "Starting Vite frontend from: $clientPath" -ForegroundColor Cyan
Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location -LiteralPath '$clientPath'; npm run dev"
)