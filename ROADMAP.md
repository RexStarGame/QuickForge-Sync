# QuickForge Sync Roadmap

QuickForge Sync is currently in beta preview. The current public release is 0.1.2-beta-preview.

The project is moving carefully toward real-data readiness, but real password storage should not be recommended until longer testing, restore validation, and external review are complete.

## v0.1.3-beta-preview — Sync reliability and account polish

Planned focus: make sync status, account switching, and recovery behavior clearer for users.

### Planned

- Sync status panel.
- Manual sync button.
- Clear last sync / last save status.
- Account switching polish.
- Better restore/import error messages.
- Multi-device test checklist.

### Goal

Users should clearly understand:

- which Google account is connected
- whether the vault is synced
- when the vault was last saved
- how to recover if sync/import fails
- how the app behaves across two PCs

## v0.1.4-beta-preview — Release hardening

Planned focus: prepare the app for broader beta testing and external review.

### Planned

- Installer/signing research.
- More recovery testing.
- More restore/import failure testing.
- External review preparation.
- Documentation cleanup before wider testing.

### Goal

Prepare the project for a more serious security and usability review before any real-data candidate release.

## v0.2.0-real-data-candidate

Planned focus: only after long testing and review.

### Requirements before this version

- Longer multi-device testing.
- Successful restore tests across fresh installs.
- External code/security review.
- Clear installer/signing decision.
- No known critical recovery, sync, or encryption issues.
- Clear user documentation.

### Goal

This version may become the first real-data candidate, but only if the project has enough testing and review evidence.

## Not planned yet

These should not be prioritized before real-data readiness:

- Browser extension.
- Mobile app.
- Large UI redesign.
- Team vaults.
- Enterprise features.
- Removing the beta warning too early.
