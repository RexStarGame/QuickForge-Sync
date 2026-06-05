# QuickForge Sync Real Data Readiness Checklist

QuickForge Sync is moving toward real-data readiness, but it is still a beta preview.

## Completed

- Encrypted vault storage
- Google Drive appdata sync
- Per-Google-account vault isolation
- Recovery key support
- Vault code change support
- Recovery key rotation support
- Encrypted backup export/import
- Tampered backup rejection test
- Stronger vault code policy
- Stronger KDF iteration setting for new vault wrappers
- Manual release ZIP testing across multiple accounts
- Clear emergency backup guidance in the app
- Corrupted/wrong cloud vault recovery guidance
- Import encrypted backup option from the unlock screen
- GitHub Actions CI build/test workflow

## Still required before removing the beta warning

- More manual restore testing with real-world backup scenarios
- Longer multi-device testing with repeated sync changes
- Better account switching polish
- Clearer release notes for real-data readiness status
- External code/security review
- Installer/signing decision for Windows builds

## Current policy

Do not store real passwords yet.

The app is closer to real-data readiness, but it should remain a beta preview until the remaining checklist items are complete.
