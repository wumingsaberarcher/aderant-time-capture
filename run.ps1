Set-Location $PSScriptRoot
# dotnet run does not honor launchBrowser; open the page ourselves.
Start-Process "http://localhost:5288"
dotnet run --project src\TimeCapture.Web --urls http://localhost:5288
