$env:DOTNET_CLI_HOME = "$PSScriptRoot\..\.dotnet"
$env:NUGET_PACKAGES = "$PSScriptRoot\..\.nuget\packages"
$env:APPDATA = "$PSScriptRoot\..\.appdata"

dotnet run --project "$PSScriptRoot\..\src\SalesCobrosGeo.Api\SalesCobrosGeo.Api.csproj"
