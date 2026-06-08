# QuickForge Sync

[![QuickForge Tests](https://github.com/RexStarGame/QuickForge-Sync/actions/workflows/dotnet-tests.yml/badge.svg)](https://github.com/RexStarGame/QuickForge-Sync/actions/workflows/dotnet-tests.yml)

**QuickForge Sync** is a Windows encrypted vault for passwords, private codes, license keys, recovery notes, and personal snippets.

It focuses on being fast to use, easy to understand, and safer than storing secrets in plain text files.

> **Beta Preview notice:** QuickForge Sync is currently a beta-preview project. It has local automated crypto/backup tests and controlled personal beta testing, but it has **not** received an external security audit yet. Use fake/test data unless you are doing a controlled personal beta test.

---

## Download Beta Preview

The easiest way to test QuickForge Sync is through GitHub Releases:

[Download QuickForge Sync Beta Preview](https://github.com/RexStarGame/QuickForge-Sync/releases)

For normal testers:

1. Download the latest `QuickForge-Sync-v0.2.0-beta-preview-win-x64.zip`.
2. Extract the ZIP first.
3. Open the extracted folder.
4. Run `QuickForge Sync.exe`.
5. Click `Continue with Google`.
6. Create or unlock your encrypted vault.

The beta ZIP includes the app Google OAuth desktop configuration file, `credentials.json`, so testers can sign in with their own Google account.

The beta ZIP must **not** include:

- Google user tokens
- `.qfvault` backup files
- user vault files
- private test data
- debug `.pdb` files

---

## What QuickForge Sync Does

QuickForge Sync helps you save and use:

- Passwords
- Game codes
- License keys
- Recovery notes
- Private snippets
- Website/app login details

Your vault is encrypted before it is saved or synced.

---

## Main Features

### Vault and Encryption

- Encrypted local/cloud vault
- Google Drive `appDataFolder` cloud storage
- Vault code unlock
- Recovery key unlock
- Recovery key rotation
- Change vault code flow
- Lockout after repeated wrong unlock attempts
- Auto-lock for safety

### Google Drive Sync

- App-managed Google Drive `appDataFolder` storage
- Background cloud sync
- Manual sync
- Cloud-vault-missing recovery guidance
- Safer logout flow
- Same-account multi-device sync support

### Backup and Restore

- Manual encrypted backup export
- Manual encrypted backup restore
- Friendly backup filenames
- Backup restore warning
- Wrong-code backup rejection
- Corrupted/random backup rejection

Example backup filename:

`QuickForge-Encrypted-Backup-07-June-2026_at_23h36.qfvault`

Backup files are still encrypted. They require your vault code or recovery key before import.

### Daily Use

- QuickFill with `Ctrl + Alt + Q`
- Search/filter saved entries
- Favorite saved entries
- Copy username
- Copy password/secret
- Open website
- Open and fill
- Clipboard cleanup after copying secrets

### Password Safety

- Password generator
- Live password strength feedback
- Weak password warnings
- Reused password warnings
- Security Center overview
- Device Trust overview
- Clear feedback for which saved accounts need attention

### User Safety Dialogs

v0.2.0 improves safety and affordance around:

- Delete entry confirmation
- Backup restore confirmation
- Change vault code
- Recovery key creation/rotation
- Logout
- Cloud vault missing
- Device trust warnings

---

## Why QuickForge Sync?

Many small password tools are either too simple, too technical, or uncomfortable for daily use.

QuickForge Sync focuses on:

- Security without confusing the user
- Clear user feedback
- Fast access with QuickFill
- Encrypted cloud sync
- Manual encrypted backups
- Better recovery guidance
- Low-friction daily use

---

## Security Model

QuickForge Sync does **not** store your vault as plain text.

Your saved entries are encrypted before being stored locally or uploaded to Google Drive. Manual backup files are encrypted too.

Important:

- Do not lose your vault code.
- Save your recovery key somewhere safe.
- Do not share exported backup files.
- Do not store recovery keys in public folders.
- If you do not recognize a trusted device, untrust it and rotate your vault code/recovery key.
- This beta has not received an external security audit.

---

## Google Drive Storage

QuickForge Sync uses Google Drive `appDataFolder`.

This means the cloud vault is app-managed and is not meant to be opened directly by the user.

Vault files are not meant to be opened manually. Use QuickForge to:

- Unlock
- Export backup
- Import backup
- Restore
- Sync

---

## QuickFill

Press `Ctrl + Alt + Q`.

QuickFill opens a small window where you can quickly search, copy, or fill saved logins.

Favorite entries appear first.

---

## Testing

Before sharing the app, follow:

[QuickForge Sync Test Checklist](TESTING.md)

For multi-device sync and restore testing:

[QuickForge Sync Multi-Device Test Checklist](MULTI_DEVICE_TEST.md)

Before publishing a beta release:

[QuickForge Sync Release Checklist](RELEASE_CHECKLIST.md)

Installer/signing planning notes:

[Installer and Signing Notes](INSTALLER_SIGNING_NOTES.md)

Before using real passwords:

[Real-Data Readiness Checklist](REAL_DATA_READINESS.md)

---

## Current Beta Status

Current development version: `v0.2.0-beta-preview`

Current local automated test result: `30/30 tests passing`

Passed local readiness areas:

- Account isolation
- Same-account multi-device sync
- Backup export/import
- Fresh-install restore
- Corrupted/random backup rejection
- Vault-code lockout
- Recovery-key unlock and rotation
- Change vault code
- Delete entry safety
- Device trust visibility
- Sync conflict merge
- Background cloud sync
- Safe-close warning while sync is still running
- Faster Open + Fill and QuickFill timing
- Reduced animation load
- Reduced Google Drive roundtrips
- Release safety cleanup

---

## Project Status

QuickForge Sync is an active beta-preview project.

Current focus:

- Final v0.2.0 beta packaging
- Manual release testing
- Controlled tester feedback
- Cleaner release notes
- Future installer/code-signing planning

UI styling is paused unless a real bug is found.

---

## Disclaimer

QuickForge Sync is a learning/prototype project and beta-preview password vault.

Do not rely on it as your only password manager until the code has been reviewed, tested more widely, and externally audited.

Use fake/test data unless a controlled personal beta test was explicitly planned.
