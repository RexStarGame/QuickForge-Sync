param(
    [string]$Version = "v0.1.7-beta-preview"
)

$ErrorActionPreference = "Stop"

$projectPath = "exam test\exam test.csproj"
$testProjectPath = "QuickForge.Tests\QuickForge.Tests.csproj"
$releaseRoot = "release"
$publishDir = "$releaseRoot\QuickForge-Sync-$Version"
$zipPath = "$releaseRoot\QuickForge-Sync-$Version.zip"
$hashPath = "$zipPath.sha256.txt"
$safetyScriptPath = "scripts\Test-ReleaseSafety.ps1"

$blockedPatterns = @(
    "credentials.json",
    "client_secret*.json",
    "token*.json",
    "*.pdb",
    "*.qfvault",
    "*.qfbackup"
)

$requiredFiles = @(
    "QuickForge Sync.exe",
    "QuickForge Sync.dll",
    "QuickForge Sync.deps.json",
    "QuickForge Sync.runtimeconfig.json",
    "README.md",
    "CHANGELOG.md",
    "TESTING.md",
    "MULTI_DEVICE_TEST.md",
    "RELEASE_CHECKLIST.md",
    "INSTALLER_SIGNING_NOTES.md",
    "REAL_DATA_READINESS.md",
    "START_HERE.txt"
)

Write-Host "`n=== QuickForge Safe Release Builder ==="
Write-Host "Version: $Version"

Write-Host "`n--- Pre-flight safety check ---"
powershell -ExecutionPolicy Bypass -File $safetyScriptPath -Version $Version

Write-Host "`n--- Debug build ---"
dotnet build $projectPath

Write-Host "`n--- Release build ---"
dotnet build $projectPath -c Release

Write-Host "`n--- Automated tests ---"
dotnet test $testProjectPath --logger "console;verbosity=normal"

Write-Host "`n--- Clean release folder ---"
Remove-Item $releaseRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

Write-Host "`n--- Publish self-contained Windows x64 build ---"
dotnet publish $projectPath `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=false `
    -o $publishDir

Write-Host "`n--- Copy release docs ---"
Copy-Item README.md $publishDir -Force
Copy-Item CHANGELOG.md $publishDir -Force
Copy-Item TESTING.md $publishDir -Force
Copy-Item MULTI_DEVICE_TEST.md $publishDir -Force
Copy-Item RELEASE_CHECKLIST.md $publishDir -Force
Copy-Item INSTALLER_SIGNING_NOTES.md $publishDir -Force
Copy-Item REAL_DATA_READINESS.md $publishDir -Force

@"
QuickForge Sync Beta Preview $Version

Run:
QuickForge Sync.exe

Important:
- QuickForge Sync has passed local controlled real-data beta readiness tests.
- You may use QuickForge Sync for controlled personal real-data beta use, but it has not received an external security audit.
- This is a beta preview.
- Google Drive appdata sync is built into the app.
- Do not upload credentials.json publicly.
- If Google setup is missing, the app will ask you to choose your own Google OAuth Desktop credentials.json file.
"@ | Set-Content -Encoding UTF8 "$publishDir\START_HERE.txt"

Write-Host "`n--- Remove blocked/private/debug files ---"
foreach ($pattern in $blockedPatterns)
{
    Get-ChildItem $publishDir -Recurse -File -Include $pattern -ErrorAction SilentlyContinue |
        Remove-Item -Force -ErrorAction SilentlyContinue
}

Write-Host "`n--- Verify required release files ---"
foreach ($file in $requiredFiles)
{
    $fullPath = Join-Path $publishDir $file

    if (!(Test-Path $fullPath))
    {
        throw "Missing required release file: $file"
    }
}

Write-Host "Required file check: OK"

Write-Host "`n--- Verify blocked files are not in release folder ---"
$blockedFound = foreach ($pattern in $blockedPatterns)
{
    Get-ChildItem $publishDir -Recurse -File -Include $pattern -ErrorAction SilentlyContinue
}

if ($blockedFound)
{
    Write-Host "`nBLOCKED FILES FOUND:"
    $blockedFound | Select-Object FullName
    throw "Release folder still contains blocked/private files. ZIP not created."
}

Write-Host "Blocked file check: OK"

Write-Host "`n--- Create ZIP ---"
Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
Remove-Item $hashPath -Force -ErrorAction SilentlyContinue
Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -Force

Write-Host "`n--- Verify ZIP contents ---"
powershell -ExecutionPolicy Bypass -File $safetyScriptPath -Version $Version

Write-Host "`n--- Create SHA256 checksum ---"
$hash = Get-FileHash $zipPath -Algorithm SHA256
$hash.Hash | Set-Content -Encoding ASCII $hashPath

Write-Host "`nSAFE RELEASE ZIP CREATED:"
Get-ChildItem $zipPath | Select-Object FullName, Length, LastWriteTime

Write-Host "`nSHA256:"
Get-Content $hashPath

Write-Host "`nUPLOAD THESE FILES:"
Write-Host (Resolve-Path $zipPath)
Write-Host (Resolve-Path $hashPath)

Write-Host "`nRelease build completed safely."



