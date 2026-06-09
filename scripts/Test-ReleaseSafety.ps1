param(
    [string]$Version = "v0.2.1-dev-preview"
)

$ErrorActionPreference = "Stop"

$projectPath = "exam test\exam test.csproj"
$releaseRoot = "releases"
$releaseName = "QuickForge-Sync-$Version-win-x64"
$publishDir = Join-Path $releaseRoot $releaseName
$zipPath = Join-Path $releaseRoot "$releaseName.zip"

$blockedTrackedRegex = '(credentials\.json$|client_secret.*\.json$|token.*\.json$|\.qfvault$|\.qfbackup$)'
$blockedReleaseRegex = '(client_secret.*\.json$|token.*\.json$|\.pdb$|\.qfvault$|\.qfbackup$)'

Write-Host "`n=== QuickForge Release Safety Check ==="
Write-Host "Version: $Version"

$csproj = Get-Content $projectPath -Raw

if ($csproj -match 'credentials\.json' -or $csproj -match 'CopyToOutputDirectory') {
    throw "Project file may still copy credentials or private files into output."
}

if ($csproj -match 'OpenAI') {
    throw "Project file still references the unused OpenAI package."
}

Write-Host "Project file check: OK"

$trackedFiles = git ls-files

$badTracked = $trackedFiles | Where-Object {
    $_ -match $blockedTrackedRegex
}

if ($badTracked) {
    $badTracked
    throw "Remove blocked/private files from git tracking before release."
}

Write-Host "Git tracked file check: OK"

$openAiTracked = $trackedFiles | Where-Object {
    $_ -match 'OpenAITest\.cs$'
}

if ($openAiTracked) {
    $openAiTracked
    throw "Remove OpenAITest.cs before release."
}

Write-Host "Unused OpenAI test file check: OK"

if (Test-Path $publishDir) {
    $badReleaseFiles = Get-ChildItem $publishDir -Recurse -File | Where-Object {
        $_.Name -match $blockedReleaseRegex
    }

    if ($badReleaseFiles) {
        $badReleaseFiles | Select-Object FullName
        throw "Release folder contains blocked/private/debug files."
    }

    if (!(Test-Path (Join-Path $publishDir "QuickForge Sync.exe"))) {
        throw "Release folder does not contain QuickForge Sync.exe."
    }

    if (!(Test-Path (Join-Path $publishDir "credentials.json"))) {
        throw "Release folder does not contain credentials.json."
    }

    Write-Host "Release folder check: OK"
}
else {
    Write-Host "Release folder check: skipped, folder does not exist yet."
}

if (Test-Path $zipPath) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path $zipPath))

    try {
        $badZipEntries = $zip.Entries | Where-Object {
            $_.FullName -match $blockedReleaseRegex
        }

        if ($badZipEntries) {
            $badZipEntries | Select-Object FullName
            throw "ZIP contains blocked/private/debug files."
        }

        if (!($zip.Entries | Where-Object { $_.FullName -eq "QuickForge Sync.exe" })) {
            throw "ZIP does not contain QuickForge Sync.exe at the root."
        }

        if (!($zip.Entries | Where-Object { $_.FullName -eq "credentials.json" })) {
            throw "ZIP does not contain credentials.json at the root."
        }
    }
    finally {
        $zip.Dispose()
    }

    Write-Host "ZIP content check: OK"
}
else {
    Write-Host "ZIP check: skipped, ZIP does not exist yet."
}

Write-Host "`nRelease safety check passed."
