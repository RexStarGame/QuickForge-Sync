# QuickForge Sync Beta Preview v0.1.0 Manual Release Test

## Release tested

- Release: QuickForge Sync Beta Preview v0.1.0
- Tag: v0.1.0-beta-preview
- Test status: Passed

## Passed checks

- Release ZIP downloads correctly
- ZIP extracts correctly
- App starts from extracted folder
- Bundled credentials.json works from the release folder
- Google login works on PC 1
- Google login works on PC 2
- Google login works with main and extra account on PC 2
- Account 1 and account 2 do not see each other’s vaults
- Vault data is isolated per Google account
- Google Drive appdata sync works per logged-in user
- 9 automated crypto/backup tests pass

## Notes

This confirms that the public beta preview release can be downloaded, extracted, started, and used with separate Google accounts without vault data mixing between users.

This does not make the app stable/final yet. Real passwords should still not be used during beta testing.
