# Changelog

## QuickForge Sync Beta Preview v0.1.2

### Status

QuickForge Sync is still in **Beta Preview**.

This version is a security, recovery, and release-readiness update. Use test data only. Do not store real passwords yet.

### Added

- Strong vault code policy for new vaults.
- Strong vault code policy when changing vault code.
- PBKDF2-SHA256 iteration count increased to 600000 for new vault wrappers.
- Emergency backup guidance after creating a new vault.
- Improved encrypted backup export guidance.
- Corrupted/wrong cloud vault recovery message.
- Import encrypted backup option from the unlock/create screen.
- Manual restore test checklist.
- Real-data readiness checklist updates.
- GitHub Actions CI build/test workflow.
- Developer-only test vault reset button for Debug builds.

### Improved

- Failed unlock now explains wrong vault code, wrong recovery key, or corrupted cloud vault file.
- Users can start encrypted backup import directly after a failed unlock.
- Backup guidance reminds users to keep recovery key and backup file in separate safe places.
- Developer reset is hidden in Release builds.
- Release-readiness documentation is clearer.
- Automated test coverage has been expanded.

### Security and Testing

- Current automated test count: 19 tests.
- Vault code strength tests added.
- Strong vault code acceptance tests added.
- Weak vault code rejection tests added.
- KDF iteration test added.
- Existing crypto and backup tests still pass.
- GitHub Actions now runs build/test on push and pull request.


## QuickForge Sync Beta Preview v0.1.2

### Status

QuickForge Sync is still in **Beta Preview**.

This version is a security, recovery, and release-readiness update. Use test data only. Do not store real passwords yet.

### Added

- Strong vault code policy for new vaults.
- Strong vault code policy when changing vault code.
- PBKDF2-SHA256 iteration count increased to 600000 for new vault wrappers.
- Emergency backup guidance after creating a new vault.
- Improved encrypted backup export guidance.
- Corrupted/wrong cloud vault recovery message.
- Import encrypted backup option from the unlock/create screen.
- Manual restore test checklist.
- Real-data readiness checklist updates.
- GitHub Actions CI build/test workflow.
- Developer-only test vault reset button for Debug builds.

### Improved

- The app now explains that a failed unlock may be caused by wrong vault code, wrong recovery key, or corrupted cloud vault file.
- Users can start encrypted backup import directly after a failed unlock.
- Backup guidance now reminds users to keep recovery key and backup file in separate safe places.
- Developer reset is hidden in Release builds.
- Release-readiness documentation is clearer.
- Automated test coverage has been expanded.

### Security and Testing

- Current automated test count: 19 tests.
- Vault code strength tests added.
- Strong vault code acceptance tests added.
- Weak vault code rejection tests added.
- KDF iteration test added.
- Existing crypto and backup tests still pass.
- GitHub Actions now runs build/test on push and pull request.

### Completed real-data-readiness steps in this version

- Stronger vault code enforcement.
- Stronger KDF settings for new vault wrappers.
- Emergency backup guidance inside the app.
- Corrupted cloud vault recovery guidance.
- Import backup option from unlock screen.
- Manual restore checklist.
- CI build/test guard.

### Still required before removing the beta warning

- Longer multi-device testing with repeated sync changes.
- More manual restore testing across fresh installs.
- Better account switching polish.
- External code/security review.
- Installer/signing decision for Windows builds.

## QuickForge Sync Beta Preview v0.1.1

### Added

- Better first-run welcome screen.
- About/version dialog.
- Account identity safety text.
- Empty vault onboarding.
- Release build script.
- README download instructions.
- Manual release test notes.

### Improved

- App output renamed to QuickForge Sync.exe.
- Top bar layout cleaned up.
- About button visibility fixed.
- Google account/vault identity made clearer.
- Release ZIP packaging improved.

## QuickForge Sync Beta Preview v0.1.0

### Added

- Encrypted vault with Google Drive sync.
- Vault code and recovery key unlock support.
- First-time recovery key confirmation flow.
- Recovery key rotation.
- Vault code change support.
- QuickFill with Ctrl + Alt + Q.
- Password generator.
- Live password strength feedback.
- Duplicate password warning.
- Clipboard cleanup.
- Auto-lock setting.
- Performance setting for background animation.
- Security Center.
- Favorite logins.
- Search/filter for saved entries.
- Manual encrypted backup export/import.

### Notes

This is not a stable/final password manager release yet.
