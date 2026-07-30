param(
    [string]$ExePath,
    [string]$FolderId,
    [switch]$Uninstall
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($ExePath)) {
    $exePath = Join-Path $scriptDir 'CsvLauncher.exe'
}
else {
    $exePath = $ExePath
}

$progId = 'CsvLauncher.csv'
$extensionKeyPath = 'Registry::HKEY_CURRENT_USER\Software\Classes\.csv'
$progIdKeyPath = "Registry::HKEY_CURRENT_USER\Software\Classes\\$progId"
$commandKeyPath = "$progIdKeyPath\\shell\\open\\command"

if ($Uninstall) {
    $currentDefault = (Get-ItemProperty -Path $extensionKeyPath -Name '(default)' -ErrorAction SilentlyContinue).'(default)'

    if ($currentDefault -eq $progId) {
        Remove-ItemProperty -Path $extensionKeyPath -Name '(default)' -ErrorAction SilentlyContinue
    }

    if (Test-Path -LiteralPath $progIdKeyPath) {
        Remove-Item -LiteralPath $progIdKeyPath -Recurse -Force
    }

    Write-Host 'CSV file association removed for current user.'
    exit 0
}

if (-not (Test-Path -LiteralPath $exePath -PathType Leaf)) {
    throw "CsvLauncher.exe was not found: $exePath"
}

$resolvedExePath = (Resolve-Path -LiteralPath $exePath).Path
$folderArg = ''
if (-not [string]::IsNullOrWhiteSpace($FolderId)) {
    $folderArg = (' --folder-id={0}' -f $FolderId)
}

$commandValue = ('"{0}"{1} "%1"' -f $resolvedExePath, $folderArg)

New-Item -Path $progIdKeyPath -Force | Out-Null
New-Item -Path "$progIdKeyPath\\shell\\open\\command" -Force | Out-Null
Set-ItemProperty -Path $progIdKeyPath -Name '(default)' -Value 'CSV Launcher' | Out-Null
Set-ItemProperty -Path $commandKeyPath -Name '(default)' -Value $commandValue | Out-Null
Set-ItemProperty -Path $extensionKeyPath -Name '(default)' -Value $progId | Out-Null

Write-Host 'CSV file association registered for current user.'
Write-Host "Command: $commandValue"
