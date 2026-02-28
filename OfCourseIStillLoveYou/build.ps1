$ErrorActionPreference = "Stop"

$msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
$fw = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"

Write-Host "Building ApexCS Project..." -ForegroundColor Cyan

# 1. Build the solution
& $msbuild "OfCourseIStillLoveYou.csproj" /t:Build /p:Configuration=Release /p:FrameworkPathOverride="$fw" /p:Version="1.0.0"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Please check the output." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Build succeeded! Distribution folder updated." -ForegroundColor Green

# 2. Automatically copy to KSP if requested
$kspPath = "C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program\GameData\OfCourseIStillLoveYou"

Write-Host ""
Write-Host "Do you want to automatically deploy the new build to your active Kerbal Space Program installation?" -ForegroundColor Yellow
$choice = Read-Host "Type 'y' to deploy, or press Enter to skip"

if ($choice -eq 'y') {
    Write-Host "Checking if KSP path exists..."
    
    if (Test-Path -Path $kspPath) {
        Write-Host "Deploying files to KSP GameData..." -ForegroundColor Cyan
        Copy-Item -Path "Distribution\GameData\OfCourseIStillLoveYou\Plugins\*" -Destination "$kspPath\Plugins" -Recurse -Force
        Write-Host "Deployment successful! You can start the game." -ForegroundColor Green
    } else {
        Write-Host "Creating OfCourseIStillLoveYou directory in GameData..."
        New-Item -ItemType Directory -Force -Path $kspPath | Out-Null
        Copy-Item -Path "Distribution\GameData\OfCourseIStillLoveYou\*" -Destination $kspPath -Recurse -Force
        Write-Host "Deployment successful! Initial installation created." -ForegroundColor Green
    }
}
