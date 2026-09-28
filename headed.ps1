# Demo the time-capture app in a real browser
$env:HEADED = "1"
Set-Location $PSScriptRoot
dotnet test --nologo --filter Lawyer_CanSaveMatterTime
