param(
    [switch]$Debug
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path (Join-Path $scriptDir '..')
$csprojPath = Join-Path $root 'CsvLauncher\CsvLauncher.csproj'
$distDir = Join-Path $root 'dist'

Write-Host '[0/6] Resolving application version...'
[xml]$csproj = Get-Content -Path $csprojPath
$appVersion = ($csproj.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1)
if ([string]::IsNullOrWhiteSpace($appVersion)) {
    throw 'Failed to read Version from CsvLauncher/CsvLauncher.csproj'
}

Write-Host '[1/6] Cleaning dist directory...'
if (Test-Path $distDir) {
    try {
        Remove-Item -Path $distDir -Recurse -Force
    }
    catch {
        throw "Failed to clean dist directory. Close processes using files under dist (especially dist/CsvLauncher.exe) and retry. Details: $($_.Exception.Message)"
    }
}
New-Item -Path $distDir -ItemType Directory | Out-Null

$publishSymbols = @(
    '-p:DebugSymbols=false',
    '-p:DebugType=None',
    '-p:CopyOutputSymbolsToPublishDirectory=false'
)
if ($Debug) {
    $publishSymbols = @(
        '-p:DebugSymbols=true',
        '-p:DebugType=portable',
        '-p:CopyOutputSymbolsToPublishDirectory=true'
    )
}

Write-Host '[2/6] Embedding Google OAuth client settings...'
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'scripts\embed-oauth.ps1') -RootPath $root
if ($LASTEXITCODE -ne 0) {
    throw 'embed-oauth.ps1 failed.'
}

Write-Host '[3/6] Publishing CsvLauncher...'
$publishArgs = @(
    'publish',
    (Join-Path $root 'CsvLauncher\CsvLauncher.csproj'),
    '-c', 'Release',
    '-r', 'win-x64',
    '--self-contained', 'true',
    '-p:PublishSingleFile=true',
    '-p:EnableCompressionInSingleFile=true'
) + $publishSymbols + @(
    '-o', $distDir
)
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw 'dotnet publish failed.'
}

Write-Host '[4/6] Copying distribution support files...'
Copy-Item -Path (Join-Path $root 'scripts\setup.ps1') -Destination (Join-Path $distDir 'setup.ps1') -Force
Copy-Item -Path (Join-Path $root 'LICENSE') -Destination (Join-Path $distDir 'LICENSE') -Force
Copy-Item -Path (Join-Path $root 'THIRD_PARTY_NOTICES.md') -Destination (Join-Path $distDir 'THIRD_PARTY_NOTICES.md') -Force

Write-Host '[5/6] Cleaning distribution credentials.json...'
$distCredentialsPath = Join-Path $distDir 'credentials.json'
if (Test-Path $distCredentialsPath) {
    Remove-Item -Path $distCredentialsPath -Force
}

Write-Host '[6/6] Creating distribution zip...'
$zipName = "CsvLauncher-$appVersion.zip"
$zipPath = Join-Path $distDir $zipName
if (Test-Path $zipPath) {
    Remove-Item -Path $zipPath -Force
}
Compress-Archive -Path (Join-Path $distDir '*') -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host 'Build completed: dist/CsvLauncher.exe'
Write-Host "Package created: dist/$zipName"
