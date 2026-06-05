param(
    [string]$Version = "v0.1.1-beta-preview"
)

$ErrorActionPreference = "Stop"

$Repo = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $Repo "exam test\exam test.csproj"

$ReleaseRoot = Join-Path $env:USERPROFILE "Desktop\QuickForge-Release"
$PublishDir = Join-Path $ReleaseRoot "QuickForge-Sync-Beta-Preview-$Version"
$ZipPath = Join-Path $ReleaseRoot "QuickForge-Sync-Beta-Preview-$Version.zip"

$ProjectCredentials = Join-Path $Repo "exam test\credentials.json"
$AppDataCredentials = Join-Path $env:APPDATA "QuickForge\Google\credentials.json"

Write-Host "QuickForge Sync release builder"
Write-Host "Version: $Version"
Write-Host ""

if (Test-Path $ProjectCredentials) {
    $CredentialsSource = $ProjectCredentials
}
elseif (Test-Path $AppDataCredentials) {
    $CredentialsSource = $AppDataCredentials
}
else {
    throw "credentials.json was not found. Put the official QuickForge Sync credentials.json in exam test\credentials.json or install it through the app once."
}

Remove-Item -Recurse -Force $PublishDir -ErrorAction SilentlyContinue
Remove-Item -Force $ZipPath -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $PublishDir | Out-Null

cd $Repo

Write-Host "Running tests..."
dotnet test "QuickForge.Tests\QuickForge.Tests.csproj" --logger "console;verbosity=normal"

Write-Host ""
Write-Host "Publishing app..."
dotnet publish $Project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o $PublishDir

Write-Host ""
Write-Host "Copying official Google OAuth credentials..."
Copy-Item $CredentialsSource (Join-Path $PublishDir "credentials.json") -Force

@"
QuickForge Sync Beta Preview $Version

How to run:
1. Extract the ZIP first.
2. Open the extracted folder.
3. Run QuickForge Sync.exe.
4. Click Continue with Google.
5. Use test data only. Do not store real passwords yet.

Included:
- QuickForge Sync.exe
- Official QuickForge Sync Google OAuth configuration
- Encrypted Google Drive appdata sync
- Encrypted backup export/import

Important:
This is a beta preview release. Do not use it as your only password manager yet.
"@ | Set-Content -Encoding UTF8 (Join-Path $PublishDir "README-FIRST.txt")

Write-Host ""
Write-Host "Creating ZIP..."
Compress-Archive -Path (Join-Path $PublishDir "*") -DestinationPath $ZipPath -Force

Write-Host ""
Write-Host "Release ZIP created:"
Write-Host $ZipPath

Write-Host ""
Write-Host "Checking important files:"
Test-Path (Join-Path $PublishDir "QuickForge Sync.exe")
Test-Path (Join-Path $PublishDir "credentials.json")
Test-Path (Join-Path $PublishDir "README-FIRST.txt")
