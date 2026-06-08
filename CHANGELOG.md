# Changelog

## v0.2.0-beta-preview (development branch)

### Focus

- Vault hardening
- Restore safety
- Backup clarity
- Security/status wording cleanup
- Broader automated backup/restore testing

### Improved

- Added clearer Google Drive appDataFolder vault storage wording.
- Added clearer cloud-vault-missing recovery screen.
- Improved encrypted backup export folder and filename.
- Improved encrypted backup export success warning.
- Improved security dialog spacing.
- Improved About and README wording for the current test count and v0.2.0 branch.

### Testing

- Automated test suite expanded to 30 passing tests.
- Added backup/restore hardening tests for empty backup content, random JSON, preserved settings, preserved entry details, and no vault-code/recovery-key leakage.

### Status

- Active development branch.
- Not released as a ZIP yet.
- Keep collecting feedback before publishing v0.2.0.

## v0.1.7-beta-preview

### Added

- Background cloud sync for vault edits
- Safe-close warning when sync is still running
- Safe logout/account-switch warning while sync is pending
- Faster recovery-key unlock path using QF- recovery-key detection
- Combined Google Drive vault download + metadata flow to reduce roundtrips

### Improved

- Save, edit, favorite, and delete now update the UI immediately
- Cloud sync now continues in the background after local changes
- Open + Fill timing reduced and status feedback improved
- QuickFill timing reduced and status feedback improved
- Generated-password fill timing reduced
- Background animation load reduced
- Security Center wording updated for controlled personal beta use

### Verified

- Debug build passed
- Release build passed
- 25/25 automated tests passed
- Safe-close warning confirmed manually
- Controlled personal beta readiness wording updated

### Security notice

QuickForge Sync encrypts vault data before syncing. Keep your vault code and recovery key safe. Losing both may permanently lock you out. This beta has not received an external security audit.


## QuickForge Sync Beta Preview v0.1.5

### Status

QuickForge Sync is still in **Beta Preview**.

This version focuses on usability and security feedback polish. You may use QuickForge Sync for controlled personal real-data beta use, but it has not received an external security audit.

### Added

- Vault self-check button in the Security Center.
- Show/hide visibility toggles for sensitive input fields.
- Visibility toggle for the vault code field.
- Visibility toggle for the confirm vault code field.
- Visibility toggle for the saved password/secret field.

### Improved

- The Security Center self-check is now reachable from the UI.
- Vault creation is easier because users can verify both vault code fields before creating the vault.
- Vault unlock is easier because users can verify their vault code or recovery key before unlocking.
- The confirm vault code visibility toggle now hides correctly during normal vault unlock.
- Sensitive fields still start hidden by default.

### Security and Testing

- Current automated test count: 19 tests.
- Debug build passes locally.
- Release build passes locally.
- All automated tests pass locally.
- App is still not real-data ready until longer testing and external review are complete.

### Still required before removing the beta warning

- Repeated multi-device sync testing.
- Repeated backup/restore testing on fresh installs.
- Repeated sync conflict testing.
- External code/security review.
- Installer/signing decision.
- Longer real-world beta testing with test data only.


## QuickForge Sync Beta Preview v0.1.4

### Status

QuickForge Sync is still in **Beta Preview**.

This version focuses on safer multi-device sync, conflict prevention, release cleanup, and installer/signing planning. You may use QuickForge Sync for controlled personal real-data beta use, but it has not received an external security audit.

### Added

- Safe sync conflict detection using Google Drive cloud metadata.
- Cloud fingerprint tracking for the encrypted vault file.
- Upload blocking when the cloud vault changed on another device.
- `Refresh` button to load the latest encrypted vault from Google Drive.
- Conflict recovery guidance explaining how to avoid overwriting newer cloud changes.
- Release checklist documentation.
- Installer and signing planning notes.
- `release/` ignored in `.gitignore`.

### Improved

- `Sync` now checks cloud state before upload.
- `Refresh` gives users a safer way to load newer cloud data before syncing.
- Sync conflict messages now recommend refreshing or exporting an encrypted backup first.
- Logout and developer test reset now clear cloud fingerprint tracking.
- Release process is better documented for future beta uploads.

### Security and Testing

- Current automated test count: 19 tests.
- Debug build passes locally.
- Release build passes locally.
- GitHub Actions should pass before release upload.
- Multi-device conflict testing is still required before real-data readiness.

### Still required before removing the beta warning

- Repeated multi-device sync testing.
- Repeated backup/restore testing on fresh installs.
- External code/security review.
- Installer/signing decision.
- Longer real-world beta testing with test data only.

## QuickForge Sync Beta Preview v0.1.3

### Status

QuickForge Sync is still in **Beta Preview**.

This version is a usability, sync-status, restore-guidance, and multi-device testing update. You may use QuickForge Sync for controlled personal real-data beta use, but it has not received an external security audit.

### Added

- Sync status panel with active state and last save/load timestamps.
- Manual `Sync now` button.
- Multi-device test checklist.
- Live vault code strength feedback on the first-time Create Vault Code screen.
- Better account switching warning before logout.
- Better encrypted backup import/restore error messages.
- Better restore success message after importing an encrypted backup.

### Improved

- Sync status no longer permanently says the last action as if it just happened.
- Sync status now shows stable `Active` state after save/load actions.
- Create Vault Code screen now warns about weak vault codes while typing.
- Create Vault Code subtitle now wraps properly inside the panel.
- Backup import now explains wrong code/key, corrupted backup, invalid file, and Google Drive restore problems.
- Account switching now explains that vaults are isolated per Google account.
- Manual sync button layout cleaned up.

### Security and Testing

- Current automated test count: 19 tests.
- Debug build passes locally.
- Release build passes locally.
- Multi-device manual checklist added for release testing.
- App is still not real-data ready until longer testing and review are complete.

### Completed real-data-readiness steps in this version

- Sync status transparency.
- Manual sync control.
- Account switching warning.
- Better restore/import guidance.
- Multi-device test checklist.
- Live first-time vault code strength feedback.

### Still required before removing the beta warning

- Complete repeated multi-device testing.
- Test backup import/export across fresh installs.
- External code/security review.
- Installer/signing decision for Windows builds.
- Longer real-world beta testing with test data only.
## QuickForge Sync Beta Preview v0.1.2

### Status

QuickForge Sync is still in **Beta Preview**.

This version is a security, recovery, and release-readiness update. You may use QuickForge Sync for controlled personal real-data beta use, but it has not received an external security audit.

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









---

## v0.2.0 Final Development Notes

- Improved Backup Center layout and restore wording.
- Improved Security Center overview, password health feedback, and device-trust readability.
- Improved recovery-key creation and rotation dialogs.
- Improved vault unlock/setup layout for both existing users and new users.
- Improved delete-entry confirmation safety.
- Improved change-vault-code dialog with field visibility buttons, live strength feedback, and copy-before-change protection.
- Improved logout confirmation wording and removed misleading direct switch-account wording.
- Fixed mojibake text issues in logout/change-vault-code UI strings.
- UI polish is paused unless a real bug is found.
- Automated tests pass: 30/30.
