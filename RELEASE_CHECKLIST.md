# QuickForge Sync Release Checklist

Use this checklist before publishing any beta preview release.

## Version checks

- [ ] `Form1.cs` has the correct `AppVersion`.
- [ ] The app top bar shows the correct version.
- [ ] The window title shows the correct version.
- [ ] Git tag, release title, and ZIP name match.

## Build and CI checks

Run locally before release:

```powershell
cd "C:\Users\patri\source\repos\-verkun\exam test"
dotnet build
dotnet build -c Release

cd "C:\Users\patri\source\repos\-verkun"
dotnet test "QuickForge.Tests\QuickForge.Tests.csproj" --logger "console;verbosity=normal"
```

Release is blocked if Debug build, Release build, tests, or GitHub Actions fail.

## Manual smoke test

- [ ] App opens normally.
- [ ] Correct version is visible.
- [ ] Google login works.
- [ ] Existing cloud vault is detected.
- [ ] Vault unlock works with vault code.
- [ ] Vault unlock works with recovery key.
- [ ] Add, edit, delete, search, favorite, reveal, copy, lock, and logout warning work.

## Sync safety test

- [ ] `Sync` button is visible.
- [ ] `Refresh` button is visible.
- [ ] `Refresh` loads the latest encrypted vault from Google Drive.
- [ ] `Sync` checks cloud state before upload.
- [ ] Unsafe upload is blocked if the cloud vault changed on another device.
- [ ] Conflict warning recommends Refresh or encrypted backup.
- [ ] Last save and last load timestamps update correctly.

## Backup and restore test

- [ ] Export encrypted backup works.
- [ ] Backup file is not empty.
- [ ] Import backup works from unlock screen and backup dialog.
- [ ] Wrong vault code or recovery key gives a helpful error.
- [ ] Corrupted backup gives a helpful error.
- [ ] Valid backup restores the cloud vault.

## Multi-device test

- [ ] `MULTI_DEVICE_TEST.md` was followed.
- [ ] Device A creates and syncs a vault.
- [ ] Device B loads the vault.
- [ ] Device B changes appear on Device A after refresh/load.
- [ ] Conflict scenario was tested.
- [ ] No silent overwrite occurred.

## Documentation and ZIP

- [ ] `README.md` is current.
- [ ] `CHANGELOG.md` contains the new version.
- [ ] `TESTING.md`, `MULTI_DEVICE_TEST.md`, `RELEASE_CHECKLIST.md`, and `INSTALLER_SIGNING_NOTES.md` are included in the release ZIP.

## Do not remove beta warning until

- [ ] Long multi-device testing is complete.
- [ ] Backup and restore were tested repeatedly.
- [ ] Sync conflict handling was tested repeatedly.
- [ ] Fresh install restore works.
- [ ] External code/security review is complete.
- [ ] Installer/signing decision is made.


