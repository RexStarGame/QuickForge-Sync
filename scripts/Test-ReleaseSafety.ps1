param(
    [string]$Version = "v0.1.6-beta-preview"
)

$ErrorActionPreference = "Stop"

$projectPath = "exam test\exam test.csproj"
$releaseRoot = "release"
$publishDir = "$releaseRoot\QuickForge-Sync-$Version"
$zipPath = "$releaseRoot\QuickForge-Sync-$Version.zip"

$blockedRegex = '(credentials\.json$|client_secret.*\.json$|token.*\.json$|\.pdb$|\.qfvault$|\.qfbackup$)'

Write-Host "`n=== QuickForge Release Safety Check ==="

# Check project file does not copy credentials into output
if (Test-Path $projectPath)
{
    $csproj = Get-Content $projectPath -Raw

    if ($csproj -match 'credentials\.json' -or $csproj -match 'CopyToOutputDirectory')
    {
        throw "Project file may still copy credentials or private files into output. Check $projectPath."
    }

    Write-Host "Project file check: OK"
}
else
{
    throw "Project file not found: $projectPath"
}

# Check tracked git files
$trackedFiles = git ls-files

$badTracked = $trackedFiles | Where-Object {
    $_ -match $blockedRegex
}

if ($badTracked)
{
    Write-Host "`nBlocked files are tracked by git:"
    $badTracked
    throw "Remove blocked/private files from git tracking before release."
}

Write-Host "Git tracked file check: OK"

# Check release folder if it exists
if (Test-Path $publishDir)
{
    $badReleaseFiles = Get-ChildItem $publishDir -Recurse -File | Where-Object {
        $_.Name -match $blockedRegex
    }

    if ($badReleaseFiles)
    {
        Write-Host "`nBlocked files found in release folder:"
        $badReleaseFiles | Select-Object FullName
        throw "Release folder contains blocked/private files."
    }

    Write-Host "Release folder check: OK"
}
else
{
    Write-Host "Release folder check: skipped, folder does not exist yet."
}

# Check ZIP if it exists
if (Test-Path $zipPath)
{
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path $zipPath))

    try
    {
        $badZipEntries = $zip.Entries | Where-Object {
            $_.FullName -match $blockedRegex
        }

        if ($badZipEntries)
        {
            Write-Host "`nBlocked files found inside ZIP:"
            $badZipEntries | Select-Object FullName
            throw "ZIP contains blocked/private files."
        }

        $exeEntry = $zip.Entries | Where-Object {
            $_.FullName -eq "QuickForge Sync.exe"
        }

        if (!$exeEntry)
        {
            throw "ZIP does not contain QuickForge Sync.exe at the root."
        }
    }
    finally
    {
        $zip.Dispose()
    }

    Write-Host "ZIP content check: OK"
}
else
{
    Write-Host "ZIP check: skipped, ZIP does not exist yet."
}

Write-Host "`nRelease safety check passed."

