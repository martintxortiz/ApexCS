<#
.SYNOPSIS
    Builds the OfCourseIStillLoveYou solution.

.PARAMETER Configuration
    The build configuration (Debug or Release). Defaults to Release.

.PARAMETER SkipEvents
    If set, MSBuild will skip PreBuild and PostBuild events. 
    Useful if you don't have the local deployment environment setup.

.EXAMPLE
    .\build.ps1 -Configuration Release -SkipEvents
#>

param (
    [string]$Configuration = "Release",
    [switch]$SkipEvents = $false
)

$ErrorActionPreference = "Stop"

$solutionFile = Join-Path $PSScriptRoot "OfCourseIStillLoveYou.sln"

Write-Host "--- Starting Build for OfCourseIStillLoveYou ---" -ForegroundColor Cyan

# 1. Check for tools
$useDotnet = $false
$msbuildPath = "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe"

Write-Host "Restoring NuGet packages..." -ForegroundColor Gray
& $msbuildPath $solutionFile /t:Restore /p:Configuration=$Configuration /v:m /clp:NoSummary | Out-File -FilePath "build.log" -Encoding utf8

Write-Host "Building solution ($Configuration)..." -ForegroundColor Gray
$msbuildArgs = @($solutionFile, "/p:Configuration=$Configuration", "/v:m", "/clp:NoSummary")
if ($SkipEvents) {
    $msbuildArgs += "/p:PreBuildEvent="
    $msbuildArgs += "/p:PostBuildEvent="
}

& $msbuildPath $msbuildArgs | Out-File -FilePath "build.log" -Append -Encoding utf8
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build Failed with exit code $LASTEXITCODE. Check build.log for details." -ForegroundColor Red
    exit 1
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBuild Successful!" -ForegroundColor Green
}
else {
    Write-Host "`nBuild Failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}
