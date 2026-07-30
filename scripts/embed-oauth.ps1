param(
    [string]$RootPath = "."
)

$ErrorActionPreference = 'Stop'

$root = (Resolve-Path -LiteralPath $RootPath).Path
$credentialsPath = Join-Path $root 'credentials.json'

if (-not (Test-Path -LiteralPath $credentialsPath)) {
    throw 'credentials.json was not found in the repository root.'
}

$json = Get-Content -LiteralPath $credentialsPath -Raw | ConvertFrom-Json
$oauth = if ($json.installed) { $json.installed } elseif ($json.web) { $json.web } else { throw 'Unsupported credentials.json format.' }

if ([string]::IsNullOrWhiteSpace($oauth.client_id) -or [string]::IsNullOrWhiteSpace($oauth.client_secret)) {
    throw 'client_id or client_secret is missing in credentials.json.'
}

$clientId = [string]$oauth.client_id
$clientSecret = [string]$oauth.client_secret

# Escape for C# string literal.
$clientIdEscaped = $clientId.Replace('\\', '\\\\').Replace('"', '\\"')
$clientSecretEscaped = $clientSecret.Replace('\\', '\\\\').Replace('"', '\\"')

$outPath = Join-Path $root 'CsvLauncher\EmbeddedGoogleOAuth.cs'
$content = @(
    'namespace CsvLauncher;',
    '',
    'internal static class EmbeddedGoogleOAuth',
    '{',
    ('    public const string ClientId = "{0}";' -f $clientIdEscaped),
    ('    public const string ClientSecret = "{0}";' -f $clientSecretEscaped),
    '}'
) -join [Environment]::NewLine

Set-Content -LiteralPath $outPath -Value $content -Encoding UTF8
Write-Host ('Using OAuth settings from: ' + $credentialsPath)
