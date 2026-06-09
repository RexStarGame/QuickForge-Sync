param(
    [string]$Marker = "QF_ATTACK_TEST_SECRET_2026_DO_NOT_USE",
    [string]$ReleaseZip = "releases\QuickForge-Sync-v0.2.1-dev-preview-win-x64.zip",
    [string]$BackupFolder = "$env:USERPROFILE\Downloads",
    [string]$ReportPath = "SECURITY_SELF_TEST_RESULTS_v0.2.1.md"
)

$ErrorActionPreference = "Stop"
$Results = @()

function Add-Result {
    param(
        [string]$Name,
        [string]$Expected,
        [string]$Actual,
        [string]$Result
    )

    $script:Results += [pscustomobject]@{
        Test = $Name
        Expected = $Expected
        Actual = $Actual
        Result = $Result
    }
}

function Test-BytesContainMarker {
    param(
        [byte[]]$Bytes,
        [string]$Marker
    )

    $utf8 = [System.Text.Encoding]::UTF8.GetBytes($Marker)
    $unicode = [System.Text.Encoding]::Unicode.GetBytes($Marker)

    function Contains-Sequence {
        param([byte[]]$Haystack, [byte[]]$Needle)

        if ($Needle.Length -eq 0 -or $Haystack.Length -lt $Needle.Length) {
            return $false
        }

        for ($i = 0; $i -le $Haystack.Length - $Needle.Length; $i++) {
            $match = $true

            for ($j = 0; $j -lt $Needle.Length; $j++) {
                if ($Haystack[$i + $j] -ne $Needle[$j]) {
                    $match = $false
                    break
                }
            }

            if ($match) {
                return $true
            }
        }

        return $false
    }

    return (Contains-Sequence -Haystack $Bytes -Needle $utf8) -or
           (Contains-Sequence -Haystack $Bytes -Needle $unicode)
}

Write-Host "`n=== QuickForge v0.2.1 Legal Self-Security Test ===" -ForegroundColor Cyan
Write-Host "Marker: $Marker"
Write-Host "Release ZIP: $ReleaseZip"
Write-Host "Backup folder: $BackupFolder"
Write-Host ""

# 1. Release ZIP safety checks
if (Test-Path $ReleaseZip) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path $ReleaseZip))

    try {
        $entryNames = $zip.Entries | ForEach-Object { $_.FullName }

        $required = @("QuickForge Sync.exe", "credentials.json")
        foreach ($item in $required) {
            if ($entryNames -contains $item) {
                Add-Result "Release ZIP contains $item" "File exists in ZIP root" "Found" "PASS"
            }
            else {
                Add-Result "Release ZIP contains $item" "File exists in ZIP root" "Missing" "FAIL"
            }
        }

        $blockedRegex = '(token.*\.json$|client_secret.*\.json$|\.qfvault$|\.qfbackup$|\.pdb$)'
        $blocked = $entryNames | Where-Object { $_ -match $blockedRegex }

        if ($blocked) {
            Add-Result "Release ZIP blocked-file scan" "No token/client_secret/vault/backup/pdb files" ($blocked -join ", ") "FAIL"
        }
        else {
            Add-Result "Release ZIP blocked-file scan" "No token/client_secret/vault/backup/pdb files" "No blocked files found" "PASS"
        }

        $markerFoundInZip = $false
        foreach ($entry in $zip.Entries) {
            if ($entry.Length -le 0 -or $entry.Length -gt 20MB) {
                continue
            }

            $stream = $entry.Open()
            try {
                $memory = New-Object System.IO.MemoryStream
                $stream.CopyTo($memory)
                if (Test-BytesContainMarker -Bytes $memory.ToArray() -Marker $Marker) {
                    $markerFoundInZip = $true
                    break
                }
            }
            finally {
                $stream.Dispose()
            }
        }

        if ($markerFoundInZip) {
            Add-Result "Release ZIP plaintext marker scan" "Fake secret marker should not appear in release ZIP" "Marker found" "FAIL"
        }
        else {
            Add-Result "Release ZIP plaintext marker scan" "Fake secret marker should not appear in release ZIP" "Marker not found" "PASS"
        }
    }
    finally {
        $zip.Dispose()
    }
}
else {
    Add-Result "Release ZIP exists" "Release ZIP should exist" "Missing: $ReleaseZip" "FAIL"
}

# 2. Backup plaintext scan
$backupFiles = @()
if (Test-Path $BackupFolder) {
    $backupFiles = Get-ChildItem $BackupFolder -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '\.(qfvault|qfbackup)$' -or $_.Name -match 'QuickForge.*Backup' }
}

if ($backupFiles.Count -eq 0) {
    Add-Result "Backup file discovery" "At least one exported QuickForge backup should exist for testing" "No backup found in $BackupFolder" "WARN"
}
else {
    foreach ($file in $backupFiles) {
        $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
        $containsMarker = Test-BytesContainMarker -Bytes $bytes -Marker $Marker

        if ($containsMarker) {
            Add-Result "Backup plaintext marker scan: $($file.Name)" "Fake secret marker should not appear in backup file" "Marker found" "FAIL"
        }
        else {
            Add-Result "Backup plaintext marker scan: $($file.Name)" "Fake secret marker should not appear in backup file" "Marker not found" "PASS"
        }
    }
}

# 3. Local AppData plaintext scan
$appDataPath = Join-Path $env:APPDATA "QuickForge Sync"

if (Test-Path $appDataPath) {
    $localFiles = Get-ChildItem $appDataPath -Recurse -File -ErrorAction SilentlyContinue
    $localMarkerHits = @()

    foreach ($file in $localFiles) {
        if ($file.Length -gt 25MB) {
            continue
        }

        $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
        if (Test-BytesContainMarker -Bytes $bytes -Marker $Marker) {
            $localMarkerHits += $file.FullName
        }
    }

    if ($localMarkerHits.Count -gt 0) {
        Add-Result "Local AppData plaintext marker scan" "Fake secret marker should not appear in local app files" ($localMarkerHits -join "`n") "FAIL"
    }
    else {
        Add-Result "Local AppData plaintext marker scan" "Fake secret marker should not appear in local app files" "Marker not found" "PASS"
    }
}
else {
    Add-Result "Local AppData folder exists" "QuickForge app data folder may exist after app use" "Not found: $appDataPath" "WARN"
}

# 4. Create a corrupted backup copy for manual restore testing
if ($backupFiles.Count -gt 0) {
    $sourceBackup = $backupFiles[0]
    $corruptPath = Join-Path $sourceBackup.DirectoryName ("CORRUPTED_TEST_COPY_" + $sourceBackup.Name)

    Copy-Item $sourceBackup.FullName $corruptPath -Force

    $bytes = [System.IO.File]::ReadAllBytes($corruptPath)
    if ($bytes.Length -gt 32) {
        $bytes[10] = $bytes[10] -bxor 0xFF
        $bytes[20] = $bytes[20] -bxor 0xAA
        [System.IO.File]::WriteAllBytes($corruptPath, $bytes)

        Add-Result "Corrupted backup copy created" "Create safe corrupted copy for manual import rejection test" $corruptPath "PASS"
    }
    else {
        Add-Result "Corrupted backup copy created" "Backup should be large enough to corrupt safely" "Backup too small" "WARN"
    }
}

# 5. Write Markdown report
$now = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$passCount = ($Results | Where-Object { $_.Result -eq "PASS" }).Count
$failCount = ($Results | Where-Object { $_.Result -eq "FAIL" }).Count
$warnCount = ($Results | Where-Object { $_.Result -eq "WARN" }).Count

$lines = @()
$lines += "# QuickForge Sync v0.2.1 Self-Security Test Results"
$lines += ""
$lines += "Date: $now"
$lines += "Marker used: ``$Marker``"
$lines += "Release ZIP: ``$ReleaseZip``"
$lines += "Backup folder scanned: ``$BackupFolder``"
$lines += ""
$lines += "Summary:"
$lines += ""
$lines += "- PASS: $passCount"
$lines += "- FAIL: $failCount"
$lines += "- WARN: $warnCount"
$lines += ""
$lines += "| Test | Expected | Actual | Result |"
$lines += "|---|---|---|---|"

foreach ($r in $Results) {
    $safeActual = ($r.Actual -replace "`r?`n", "<br>")
    $lines += "| $($r.Test) | $($r.Expected) | $safeActual | $($r.Result) |"
}

$lines += ""
$lines += "Manual follow-up required:"
$lines += ""
$lines += "- Try importing the corrupted backup copy. Expected result: QuickForge rejects it."
$lines += "- Try restoring a valid backup with the wrong vault code. Expected result: rejected."
$lines += "- Try restoring a valid backup with the correct vault code or recovery key. Expected result: accepted."
$lines += "- Try untrusted-device actions. Expected result: sensitive actions blocked."
$lines += "- Try Authenticator Lock wrong/old codes. Expected result: rejected."
$lines += ""

$lines | Set-Content -Encoding UTF8 $ReportPath

Write-Host "`n=== Results ===" -ForegroundColor Cyan
$Results | Format-Table -AutoSize

Write-Host "`nReport written to: $ReportPath" -ForegroundColor Green

if ($failCount -gt 0) {
    Write-Host "`nFAIL detected. Do not claim this test passed until investigated." -ForegroundColor Red
    exit 1
}

Write-Host "`nNo FAIL results found. Review WARN items and complete manual follow-up tests." -ForegroundColor Green
