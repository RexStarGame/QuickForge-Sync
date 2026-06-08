param(
    [string]$Version = "v0.2.0-beta-preview"
)

$ErrorActionPreference = "Stop"

function Run-OrFail {
    param([string]$Command)

    Write-Host "`n> $Command" -ForegroundColor Cyan
    cmd /c $Command

    if ($LASTEXITCODE -ne 0) {
        throw "Command failed: $Command"
    }
}

$projectPath = "exam test\exam test.csproj"
$testProjectPath = "QuickForge.Tests\QuickForge.Tests.csproj"
$releaseRoot = "releases"
$releaseName = "QuickForge-Sync-$Version-win-x64"
$publishDir = Join-Path $releaseRoot $releaseName
$zipPath = Join-Path $releaseRoot "$releaseName.zip"
$hashPath = "$zipPath.sha256.txt"
$safetyScriptPath = "scripts\Test-ReleaseSafety.ps1"

$blockedReleasePatterns = @(
    "client_secret*.json",
    "token*.json",
    "*.pdb",
    "*.qfvault",
    "*.qfbackup"
)

Write-Host "`n=== QuickForge Safe Release Builder ==="
Write-Host "Version: $Version"

$status = git status --porcelain
$statusWithoutReleases = $status | Where-Object { $_ -notmatch '^\?\? releases/' }

if ($statusWithoutReleases) {
    $statusWithoutReleases
    throw "Repo has uncommitted changes outside releases/. Commit or fix them before release."
}

powershell -ExecutionPolicy Bypass -File $safetyScriptPath -Version $Version

Get-Process | Where-Object { $_.ProcessName -like "*QuickForge*" } |
    Stop-Process -Force -ErrorAction SilentlyContinue

Start-Sleep -Seconds 3

Run-OrFail "dotnet build `"$projectPath`""
Run-OrFail "dotnet build `"$projectPath`" -c Release"
Run-OrFail "dotnet test `"$testProjectPath`" --logger `"console;verbosity=normal`""

Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
Remove-Item $hashPath -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $releaseRoot | Out-Null

Run-OrFail "dotnet publish `"$projectPath`" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -o `"$publishDir`""

$credentialsCandidates = Get-ChildItem "." -Recurse -Filter "credentials.json" |
    Where-Object {
        $_.FullName -notmatch '\\bin\\' -and
        $_.FullName -notmatch '\\obj\\' -and
        $_.FullName -notmatch '\\release\\' -and
        $_.FullName -notmatch '\\releases\\'
    }

if ($credentialsCandidates.Count -eq 0) {
    throw "credentials.json was not found. Put the QuickForge Google OAuth Desktop credentials.json in the project folder before building the ZIP."
}

if ($credentialsCandidates.Count -gt 1) {
    $credentialsCandidates | Select-Object FullName
    throw "More than one credentials.json found. Keep only the correct QuickForge OAuth Desktop credentials file."
}

Copy-Item $credentialsCandidates[0].FullName -Destination (Join-Path $publishDir "credentials.json") -Force

$docs = @(
    "README.md",
    "CHANGELOG.md",
    "TESTING.md",
    "MULTI_DEVICE_TEST.md",
    "RELEASE_CHECKLIST.md",
    "INSTALLER_SIGNING_NOTES.md",
    "REAL_DATA_READINESS.md"
)

foreach ($doc in $docs) {
    if (Test-Path $doc) {
        Copy-Item $doc $publishDir -Force
    }
}

$releaseNotesPath = Join-Path $publishDir "RELEASE-NOTES-$Version.txt"

@"
QuickForge Sync $Version

Release type:
- Beta-preview release for controlled testing.

Main changes:
- Google Drive appDataFolder cloud vault storage.
- Encrypted-only vault and backup handling.
- Improved Backup Center, Security Center, Device Trust, recovery key, logout, delete-entry, and change-vault-code safety dialogs.
- Better cloud-vault-missing and restore guidance.
- Automated tests: 30/30 passing.

Google login:
- This build includes credentials.json for the QuickForge Google OAuth desktop client.
- It does not include user tokens, vault files, or backup files.
- Each tester signs in with their own Google account.

Important beta warning:
- Use fake/test data unless a controlled personal beta test was explicitly planned.
- This app has not received an external security audit.
- Keep your vault code and recovery key safe.
"@ | Set-Content -Encoding UTF8 $releaseNotesPath

foreach ($pattern in $blockedReleasePatterns) {
    Get-ChildItem $publishDir -Recurse -File -Include $pattern -ErrorAction SilentlyContinue |
        Remove-Item -Force -ErrorAction SilentlyContinue
}

$requiredFiles = @(
    "QuickForge Sync.exe",
    "credentials.json",
    "RELEASE-NOTES-$Version.txt"
)

foreach ($file in $requiredFiles) {
    $fullPath = Join-Path $publishDir $file

    if (!(Test-Path $fullPath)) {
        throw "Missing required release file: $file"
    }
}

$blockedFound = foreach ($pattern in $blockedReleasePatterns) {
    Get-ChildItem $publishDir -Recurse -File -Include $pattern -ErrorAction SilentlyContinue
}

if ($blockedFound) {
    $blockedFound | Select-Object FullName
    throw "Release folder still contains blocked/private/debug files. ZIP not created."
}

Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -Force

powershell -ExecutionPolicy Bypass -File $safetyScriptPath -Version $Version

$hash = Get-FileHash $zipPath -Algorithm SHA256
$hash.Hash | Set-Content -Encoding ASCII $hashPath

Write-Host "`nSAFE RELEASE ZIP CREATED:" -ForegroundColor Green
Get-ChildItem $zipPath | Select-Object FullName, Length, LastWriteTime

Write-Host "`nSHA256:"
Get-Content $hashPath
