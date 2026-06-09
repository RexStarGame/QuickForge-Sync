# QuickForge Sync Security Test History Report

Version covered: `v0.1.x` through `v0.2.1-dev-preview`  
Current public beta version: `v0.2.1-dev-preview`  
Release ZIP: `QuickForge-Sync-v0.2.1-dev-preview-win-x64.zip`  
Release ZIP SHA256: `CE7B20532BEB16C386F7D58B0FC32ECD6A65E448DF046B3723309A39FDA2B679`  
Release branch used for the ZIP: `v0.2.1-dev-auth-settings`  
Release code commit used for the ZIP: `76c54fe` / `Make Authenticator Lock changes feel faster`  
Main merge commit after v0.2.1 merge: `dbaafa5`  
Status: beta-preview / controlled testing  
External security audit: not completed

> This document is not an external audit and does not claim that QuickForge Sync is professionally audited. It is a structured proof/history document for what has been implemented, manually tested, and checked during development.

---

## 1. Purpose of this report

This report records the security and reliability testing history of QuickForge Sync from the early beta versions up to `v0.2.1-dev-preview`.

The goal is to turn the development and manual testing work into clear evidence:

- what was tested,
- what was fixed,
- what passed,
- what is still limited,
- and what should be tested next before trusting the app with high-risk real passwords.

---

## 2. Current honest safety status

QuickForge Sync currently has enough functionality for controlled beta testing with fake/test data and careful personal testing.

It should not yet be treated as a fully mature replacement for professional password managers such as Bitwarden, 1Password, KeePassXC, or Proton Pass.

Current recommended use:

- OK: fake/test data
- OK: controlled personal beta testing
- OK: low-risk personal secrets if the user keeps backups and understands the risk
- Not recommended yet: banking, main email, crypto wallets, business admin credentials, government accounts, or anything that cannot be lost

Main reason: the app has strong beta features and passing automated tests, but it has not received an external security audit or long-term real-world testing.

---

## 3. Automated test evidence

Current local automated test result:

```text
30/30 tests passing
```

Covered areas include:

- vault code policy checks,
- weak vault code rejection,
- vault-code lockout timing,
- encryption/decryption behavior,
- wrong vault code rejection,
- recovery key unlock,
- recovery key rotation,
- change vault code,
- encrypted backup import with vault code,
- encrypted backup import with recovery key,
- wrong backup/key rejection,
- corrupted/random backup rejection,
- backup restore preserves entries,
- backup restore preserves vault settings,
- encrypted JSON does not expose plaintext secrets,
- tampered backup file fails to import,
- stronger KDF iteration check.

Release build evidence for `v0.2.1-dev-preview`:

- Debug build: passed
- Release build: passed
- Automated tests: 30/30 passed
- Publish win-x64 self-contained build: passed
- Release folder check: passed
- ZIP content check: passed
- Release safety check: passed

---

## 4. Release ZIP safety evidence

The v0.2.1 release builder created:

```text
releases/QuickForge-Sync-v0.2.1-dev-preview-win-x64.zip
```

SHA256:

```text
CE7B20532BEB16C386F7D58B0FC32ECD6A65E448DF046B3723309A39FDA2B679
```

Release safety checks confirmed:

- project file check passed,
- git tracked file check passed,
- unused OpenAI test file check passed,
- release folder check passed,
- ZIP content check passed.

The release ZIP is expected to include:

- `QuickForge Sync.exe`,
- `credentials.json`,
- documentation/release notes.

The release ZIP must not include:

- Google user tokens,
- `.qfvault` vault/backup files,
- `.qfbackup` files,
- private test data,
- debug `.pdb` files,
- `token*.json`,
- `client_secret*.json`.

---

## 5. Version history and proof notes

### Early v0.1.x beta foundation

Main focus:

- encrypted local/cloud vault,
- Google Drive sync,
- vault code unlock,
- recovery key unlock,
- recovery key rotation,
- change vault code,
- backup export/import,
- basic Security Center / safety checks,
- password generator and password strength feedback,
- QuickFill and daily-use actions.

Evidence and testing performed:

- created and unlocked vaults,
- tested wrong vault code rejection,
- tested recovery key behavior,
- tested backup export and restore,
- tested corrupted/random backup rejection,
- tested that encrypted backup data does not expose plaintext secrets,
- tested save, copy, reveal, search, favorite, and open/fill daily-use behavior.

Result:

- PASS for controlled beta use.
- Not enough proof yet for real-password main use.

---

### v0.1.5 beta-preview hardening

Main focus:

- Security Center self-check button,
- show/hide toggles for sensitive fields,
- sensitive fields hidden by default,
- release safety guard,
- `.gitignore` cleanup for private files,
- release ZIP safety direction.

Evidence and testing performed:

- Debug/Release builds passed,
- automated test set passed at the time,
- release safety script added to block private files,
- verified that private credentials/tokens/vault/backup/debug files should not be shipped accidentally.

Result:

- PASS as an improvement to beta release safety and daily usability.

---

### v0.1.7 / v0.2.0 preparation

Main focus:

- cleaner security wording,
- real-data readiness checklist,
- release checklist,
- installer/signing planning notes,
- better backup export/restore guidance,
- multi-device test checklist,
- safer user safety dialogs.

Evidence and testing performed:

- backup/restore guidance reviewed,
- readiness docs prepared,
- release checklists prepared,
- manual testing around backup restore and wrong-code/corrupted backup behavior.

Result:

- PASS for better release process and documentation maturity.

---

### v0.2.0 beta-preview

Main focus:

- safer logout flow,
- safer close behavior during sync,
- Device Trust visibility,
- same-account multi-device sync,
- sync conflict merge protection,
- background cloud sync,
- reduced Google Drive roundtrips,
- faster Open + Fill and QuickFill timing,
- release cleanup.

Evidence and testing performed:

- tested same-account multi-device sync,
- tested backup export/import,
- tested fresh-install restore,
- tested corrupted/random backup rejection,
- tested vault-code lockout,
- tested recovery-key unlock and rotation,
- tested change vault code,
- tested delete entry safety,
- tested Device Trust visibility,
- tested sync conflict merge,
- tested background cloud sync,
- tested safe-close warning while sync was running.

Result:

- PASS for controlled beta testing.
- Still required stronger Authenticator Lock and Device Trust behavior before wider use.

---

### v0.2.1-dev-preview

Main focus:

- optional Authenticator Lock,
- Trust Center Authenticator integration,
- stronger Device Trust rules,
- untrusted-device restrictions,
- Streamer / Privacy mode polish,
- better Settings feedback,
- safer sync/refresh behavior,
- current-device forget prevention,
- release ZIP safety.

#### Authenticator Lock work

Implemented and tested:

- optional 6-digit authenticator app codes,
- vault code required before authenticator code,
- setup/manage flow no longer freezes,
- OFF -> ON -> OFF -> ON behavior without forcing a new QR every time,
- QR setup confirmation before enabling,
- recovery path for broken, deleted, old, or wrong QR setups,
- recovery key remains emergency path if authenticator access is lost,
- ON/OFF UI made faster with safe background sync,
- authenticator changes no longer feel blocked by slow Google Drive operations.

Manual test result:

- PASS: setup flow works.
- PASS: wrong/old QR recovery flow works.
- PASS: OFF -> ON -> OFF -> ON behavior works.
- PASS: unlock order requires vault code before authenticator code.
- PASS: authenticator changes feel faster after final sync behavior fix.

Known limitation:

- Authenticator Lock still needs deeper automated test coverage around TOTP replay, old-code rejection, and sync conflict edge cases.

#### Trust Center work

Implemented and tested:

- Trust Center Authenticator card connected to the real Authenticator Lock settings flow,
- Optional / Active status wording,
- Set up / Manage actions,
- Trust Center opens quickly without blocking on cloud refresh,
- background refresh continues after UI opens.

Manual test result:

- PASS: Trust Center card opens correct Authenticator flow.
- PASS: Trust Center opens quickly after delay fix.
- PASS: UI updates correctly after Authenticator changes.

#### Device Trust work

Implemented and tested:

- improved same-account multi-device detection,
- new/untrusted devices sync their registration automatically,
- trusted device can see and approve new device after refresh/background sync,
- untrusted devices cannot access sensitive actions,
- backup/export blocked on untrusted devices,
- Trust Center access blocked on untrusted devices,
- current device cannot be forgotten by accident,
- other non-current devices can still be forgotten.

Manual test result:

- PASS: new device detection restored after auto-sync fix.
- PASS: Trust Center and Device Trust open quickly after removing blocking refresh.
- PASS: untrusted device restrictions block sensitive actions.
- PASS: backup/export is blocked on untrusted devices.
- PASS: Trust Center is blocked on untrusted devices.
- PASS: current device cannot be forgotten.
- PASS: other devices can still be managed.

Known limitation:

- Device Trust still relies on cloud sync/refresh rather than live push between devices.

#### Streamer / Privacy mode work

Implemented and tested:

- Streamer mode for safer screen sharing, screenshots, and recordings,
- clearer warning/feedback when Streamer mode is off and sensitive details may be visible,
- faster Streamer mode save feedback,
- Streamer mode changes queue background sync instead of showing annoying sync warning popups.

Manual test result:

- PASS: Streamer mode save feedback improved.
- PASS: annoying sync warning popup behavior removed.
- PASS: Streamer mode warning/feedback included in v0.2.1.

#### Settings feedback work

Implemented and tested:

- Auto-lock settings feedback improved,
- Auto-refresh settings feedback improved,
- Background animation settings feedback improved,
- Recovery reminder settings feedback improved,
- Streamer mode settings feedback improved.

Manual test result:

- PASS: Save actions close/update quickly instead of leaving user confused.
- PASS: settings return/refresh behavior improved.

#### Sync / refresh / close behavior

Implemented and tested:

- safe-close protection while sync/refresh/background sync is running,
- background sync queue and retry behavior,
- auto-refresh behavior restored after it was accidentally not running correctly,
- sync status color/wording improved to avoid constant scary red state when sync/load was recent,
- Authenticator and Device Trust changes update quickly while syncing safely.

Manual test result:

- PASS: safe-close blocks closing during sync/refresh.
- PASS: auto-refresh restored.
- PASS: Authenticator UI no longer feels too slow.
- PASS: Device Trust UI no longer blocks on cloud refresh.

---

## 6. Manual test cases completed or partially completed

### Core vault

- PASS: create vault
- PASS: unlock vault
- PASS: wrong vault code rejected
- PASS: vault-code lockout behavior
- PASS: recovery key unlock
- PASS: recovery key rotation
- PASS: change vault code

### Backup and restore

- PASS: encrypted backup export
- PASS: encrypted backup restore
- PASS: wrong-code backup rejection
- PASS: corrupted/random backup rejection
- PASS: backup restore preserves entries
- PASS: backup restore preserves vault settings

### Daily use

- PASS: add entry
- PASS: edit entry
- PASS: delete entry confirmation
- PASS: search/filter entries
- PASS: favorite entries
- PASS: copy username
- PASS: copy password/secret
- PASS: open website
- PASS: Open + Fill timing improvements
- PASS: QuickFill timing improvements

### Authenticator Lock

- PASS: setup with QR
- PASS: vault code required first
- PASS: authenticator code required after vault code
- PASS: OFF -> ON -> OFF -> ON without new QR
- PASS: wrong/old QR recovery flow
- PASS: Authenticator UI speed fix

### Trust Center and Device Trust

- PASS: Trust Center Authenticator card opens real settings flow
- PASS: Optional / Active status behavior
- PASS: Device Trust detects same-account multi-device state
- PASS: new device registration syncs automatically
- PASS: untrusted device restrictions
- PASS: backup/export blocked on untrusted devices
- PASS: Trust Center blocked on untrusted devices
- PASS: current device cannot be forgotten
- PASS: other non-current devices can be forgotten/managed

### Streamer / Privacy mode

- PASS: Streamer mode setting works
- PASS: Streamer mode warning/feedback improved
- PASS: Streamer mode sync warning popup reduced/removed
- PASS: sensitive UI visibility improved where possible

### Release safety

- PASS: build and release build
- PASS: 30/30 automated tests
- PASS: publish win-x64 self-contained release
- PASS: release folder safety check
- PASS: ZIP content safety check
- PASS: SHA256 generated
- PASS: README updated for v0.2.1
- PASS: v0.2.1 branch merged into main

---

## 7. Current known limitations

QuickForge Sync is still beta. Known limitations:

- no external security audit yet,
- no signed installer yet,
- release is ZIP-based,
- Device Trust and Authenticator status sync through Google Drive refresh/background sync, not live push,
- app cannot protect against malware/keyloggers already running on the PC,
- app cannot protect against a compromised Google account,
- app cannot protect if the attacker has the vault code or recovery key,
- Authenticator/Device Trust logic needs more automated unit/integration tests,
- real-world beta feedback is still limited,
- long-term crash/offline/bad-network testing is still needed.

---

## 8. What should be tested next

Before raising trust level for real-password use, test:

- one-week two-device soak test,
- bad internet/offline-then-online sync,
- interrupted sync while closing,
- restore from backup on a clean Windows user profile,
- Authenticator ON/OFF while the second device is open,
- Device Trust approval from PC1 for PC2,
- untrusted-device blocked actions,
- manual restore after accidental local state issue,
- more tester feedback from external users.

Recommended next automated tests:

- valid TOTP passes,
- wrong TOTP fails,
- old TOTP replay fails,
- Authenticator OFF/ON keeps same secret,
- broken QR replacement works,
- untrusted device cannot export backup,
- untrusted device cannot open Trust Center,
- current device cannot be forgotten,
- other device can be forgotten,
- security settings merge correctly across cloud refresh.

---

## 9. Conclusion

QuickForge Sync v0.2.1-dev-preview has moved from a simple encrypted vault beta into a more serious controlled beta security project.

The app now has:

- encrypted vault storage,
- Google Drive appDataFolder sync,
- recovery key support,
- backup/restore hardening,
- vault-code lockout,
- Authenticator Lock,
- Trust Center integration,
- Device Trust restrictions,
- current-device forget prevention,
- Streamer / Privacy mode,
- safe-close protection,
- release safety checks,
- and 30/30 automated tests passing.

Honest status:

- Good enough for controlled beta testing with fake/test data.
- Promising for careful personal low-risk use after more soak testing.
- Not yet ready to replace a mature audited password manager for critical accounts.

This report should be updated after each release and after each structured tester feedback round.
