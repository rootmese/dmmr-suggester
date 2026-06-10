param(
    [string]$VersionSuffix = "",
    [switch]$PushToNuGet
)

$projectPath = "DMMRSuggestionEngine/DMMRSuggestionEngine.csproj"
$outputDir = "./artifacts"
$versionArg = if ($VersionSuffix) { "--version-suffix $VersionSuffix" } else { "" }

Write-Host "Limpando builds anteriores..." -ForegroundColor Cyan
dotnet clean $projectPath -c Release

Write-Host "Restaurando e buildando em Release..." -ForegroundColor Cyan
dotnet restore $projectPath
dotnet build $projectPath -c Release --no-restore

Write-Host "Gerando pacote NuGet..." -ForegroundColor Cyan
dotnet pack $projectPath -c Release --no-build -o $outputDir $versionArg

Write-Host "Pacote criado em $outputDir" -ForegroundColor Green

if ($PushToNuGet) {
    $apiKey = Read-Host "Digite sua API Key do NuGet.org" -AsSecureString
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($apiKey)
    $plainKey = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
    
    Write-Host "Publicando no NuGet.org..." -ForegroundColor Cyan
    dotnet nuget push "$outputDir/*.nupkg" --api-key $plainKey --source https://api.nuget.org/v3/index.json --skip-duplicate
}