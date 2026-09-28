# Playwright recorder: left = browser, right = Inspector.
# The website must already be running at http://localhost:5288.
#
#   Terminal 1:  .\run.ps1
#   Terminal 2:  .\codegen.ps1
param(
    [string]$Url = "http://localhost:5288"
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$playwright = Join-Path $PSScriptRoot "tests\TimeCapture.Tests\bin\Debug\net10.0\playwright.ps1"
if (-not (Test-Path $playwright)) {
    Write-Host "playwright.ps1 missing — building tests (stop dotnet run first if the exe is locked)."
    dotnet build tests\TimeCapture.Tests\TimeCapture.Tests.csproj --nologo -v q
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed. Ctrl+C the website, build, then start the site and record."
    }
}

& $playwright codegen --target csharp $Url
