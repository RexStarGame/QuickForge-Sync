# QuickForge Sync Installer and Signing Notes

QuickForge Sync is currently distributed as a ZIP file through GitHub Releases.

## Current status

- The app is not code-signed.
- The ZIP release is for beta testing only.
- Windows may show SmartScreen or unknown publisher warnings.
- Users should use test data only.
- The app should not be marketed as production-ready.

## Why signing matters

Code signing helps users verify that the app came from the expected publisher and was not modified after signing.

Without signing:

- Windows may warn users before running the app.
- Some users may not trust the download.
- Business or enterprise users may block the app.

## Options to research

### GitHub ZIP for beta

Good for early private testing, but not ideal for non-technical users.

### Inno Setup installer

A simple Windows installer option for later beta releases.

### MSIX installer

A more modern Windows packaging option, but needs more setup and testing.

### Code signing certificate

Required before serious public release, but it costs money and may require identity or business verification.

### Microsoft Store

Possible future distribution option if the app becomes mature enough.

## Recommended path

For now:

- Continue using GitHub Releases ZIP.
- Clearly label every release as beta preview.
- Keep warning users to use test data only.
- Include release checklist and test documentation.

Before real-data candidate:

- Decide installer format.
- Decide signing approach.
- Complete external code/security review.
- Complete repeated multi-device sync tests.
- Complete backup and restore tests on fresh installs.

## Current decision

QuickForge Sync should not be advertised as production-ready while it is unsigned, unreviewed, and still marked as beta.

## Beta ZIP packaging rule

For the current beta ZIP, QuickForge needs the Google OAuth Desktop client configuration so testers can sign in with Google.

OK to include:

- `credentials.json`
- `QuickForge Sync.exe`
- release notes / test notes

Never include:

- `token*.json`
- `*.qfvault`
- `*.qfbackup`
- user vault files
- user backup files
- user Google session tokens

Reason:

- `credentials.json` identifies the QuickForge OAuth desktop client.
- User tokens are created locally after each tester signs in with their own Google account.
- Each user vault is stored in that user's Google Drive appDataFolder.
- Vault and backup files must stay encrypted and user-controlled.

