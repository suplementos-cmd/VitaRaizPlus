$env:DOTNET_CLI_HOME = "$PSScriptRoot\..\.dotnet"
$env:NUGET_PACKAGES = "$PSScriptRoot\..\.nuget\packages"
$env:APPDATA = "$PSScriptRoot\..\.appdata"

dotnet restore "$PSScriptRoot\..\SalesCobrosGeo.sln" --configfile "$PSScriptRoot\..\NuGet.Config"
dotnet build "$PSScriptRoot\..\SalesCobrosGeo.sln" --no-restore -m:1
