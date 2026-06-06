# QuickForge Sync Real-Data Readiness Checklist

QuickForge Sync is still a beta preview.

Do not use real passwords yet.

This checklist must be completed before removing the beta warning or calling the app real-data ready.

## Current status

Real-data status: Not ready yet.

QuickForge Sync has improved security and recovery features, but it still needs repeated reliability testing and review.

## Already completed

- [x] Strong vault code policy for new vaults.
- [x] Live vault code strength feedback.
- [x] PBKDF2-SHA256 600000 iterations for new vault wrappers.
- [x] Recovery key support.
- [x] Recovery key rotation.
- [x] Encrypted backup export.
- [x] Encrypted backup import.
- [x] Better backup import/restore error messages.
- [x] Manual Sync button.
- [x] Refresh from cloud button.
- [x] Cloud conflict detection.
- [x] Unsafe upload blocking if cloud changed.
- [x] Vault self-check button.
- [x] Show/hide toggles for sensitive input fields.
- [x] Multi-device test checklist.
- [x] Release checklist.
- [x] Installer/signing planning notes.

## Required before real data

Do not use real passwords until all of these are complete:

- [ ] 3+ successful multi-device test cycles.
- [ ] 3+ successful fresh-install restore tests.
- [ ] 3+ successful encrypted backup export/import tests.
- [ ] 3+ successful corrupted-backup tests.
- [ ] 3+ successful sync conflict tests.
- [ ] GitHub Actions green on the release commit.
- [ ] Local Debug build passes.
- [ ] Local Release build passes.
- [ ] All automated tests pass.
- [ ] External code/security review completed.
- [ ] Installer/signing decision made.
- [ ] Recovery guidance reviewed.
- [ ] User-facing beta warnings reviewed.

## Fresh install restore test

Use a clean Windows user profile or another Windows PC.

- [ ] Install or extract QuickForge Sync fresh.
- [ ] Open the app.
- [ ] Confirm correct version is visible.
- [ ] Log in with Google.
- [ ] Confirm existing cloud vault is detected.
- [ ] Unlock with vault code.
- [ ] Lock and unlock again with recovery key.
- [ ] Confirm entries appear correctly.
- [ ] Export encrypted backup.
- [ ] Import encrypted backup.
- [ ] Confirm restore succeeds.
- [ ] Confirm sync status is active.

## Multi-device test

Use Device A and Device B.

- [ ] Device A creates or opens vault.
- [ ] Device A adds test entry.
- [ ] Device A presses Sync.
- [ ] Device B opens same Google account.
- [ ] Device B loads vault.
- [ ] Device B adds test entry.
- [ ] Device B presses Sync.
- [ ] Device A presses Refresh.
- [ ] Device A sees Device B entry.
- [ ] No data loss occurs.

## Conflict test

- [ ] Device A loads vault.
- [ ] Device B loads vault.
- [ ] Device B adds entry and syncs.
- [ ] Device A tries to sync old local state.
- [ ] App blocks unsafe upload.
- [ ] Conflict warning appears.
- [ ] App recommends Refresh or encrypted backup.
- [ ] User can Refresh safely.

## Backup failure test

- [ ] Export encrypted backup.
- [ ] Copy the backup file.
- [ ] Corrupt the copy manually.
- [ ] Try importing corrupted copy.
- [ ] App shows helpful error.
- [ ] App recommends another backup or recovery key.

## External review preparation

Before real-data candidate:

- [ ] Review cryptography flow.
- [ ] Review Google Drive appDataFolder usage.
- [ ] Review local secret handling.
- [ ] Review clipboard cleanup behavior.
- [ ] Review backup/restore flow.
- [ ] Review sync conflict flow.
- [ ] Review installer/signing plan.

## Decision

QuickForge Sync can only become real-data candidate after testing proves:

- Recovery works.
- Backup works.
- Multi-device sync works.
- Conflict protection works.
- Fresh install restore works.
- External review found no blocking issues.





