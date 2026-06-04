# Changelog

## QuickForge Sync Beta Preview

### Status

QuickForge Sync is now in **Beta Preview**.

This version is still for test data only. Do not store real passwords yet. The app has automated crypto and backup tests, but it still needs more real-world testing before stable release.

### Added

- Encrypted vault with Google Drive sync
- Vault code and recovery key unlock support
- First-time recovery key confirmation flow
- Recovery key rotation
- Vault code change support
- QuickFill with Ctrl + Alt + Q
- Password generator
- Live password strength feedback
- Duplicate password warning
- Clipboard cleanup
- Auto-lock setting
- Performance setting for background animation
- Security Center
- Favorite logins
- Search/filter for saved entries
- Manual encrypted backup export/import
- Better empty-state messages
- GitHub Actions test workflow
- README test status badge

### Security and Testing

- Added automated crypto tests
- Added vault code decrypt test
- Added recovery key decrypt test
- Added wrong vault code rejection test
- Added plaintext secret check for encrypted JSON
- Added recovery key rotation test
- Added vault code change test
- Added encrypted backup import test with vault code
- Added encrypted backup import test with recovery key
- Added tampered/corrupted backup rejection test
- Current automated test count: 9 tests

### Improved

- App close behavior no longer logs out of Google automatically
- Delete confirmation added before removing entries
- QuickFill labels made more user-friendly
- Long secrets display better in preview
- Recovery key download as plain text was removed
- Recovery key copy flow now clears clipboard after delay
- Failed unlock clears vault code input fields

### Notes

This is not a stable/final password manager release yet.

Before stable release, QuickForge Sync still needs:

- More manual testing with fake data
- Testing on another Windows PC
- Fresh install and Google sync restore testing
- Installer/release packaging
- External code/security review
