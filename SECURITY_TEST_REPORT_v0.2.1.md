# QuickForge Sync v0.2.1 Security Test History Report

Version covered: `v0.1.x` through `v0.2.1-dev-preview`  
Current beta version: `v0.2.1-dev-preview`  
Current fixed source branch: `main`  
Important fixed source commit: `51d7d38` / `Require backup password during import`  
Status: beta-preview / controlled testing  
External security audit: not completed

> This document is not an external audit and does not claim that QuickForge Sync is professionally audited. It is a structured internal/manual security test history for what has been implemented, tested, broken, fixed, and retested during development.

---

## 1. Purpose

This report records the security and reliability test history for QuickForge Sync up to `v0.2.1-dev-preview`.

The goal is to document real proof instead of vague claims:

- what was tested,
- what passed,
- what failed,
- what was fixed,
- what still needs deeper testing,
- and what should not be claimed yet.

---

## 2. Honest safety status

QuickForge Sync is now a serious beta security project, but it is still not a mature audited password manager.

Recommended use right now:

- OK: fake/test data.
- OK: controlled beta testing.
- OK: careful low-risk personal testing after backups are verified.
- Not recommended yet: banking, main email, crypto wallets, business admin accounts, government accounts, or anything that cannot be lost.

Main reason: the app has strong beta features and many tests, but it has not received an external security audit, signed installer, or long-term broad real-world testing.

---

## 3. Current v0.2.1 security features tested

v0.2.1 includes and has been manually tested around:

- encrypted vault storage,
- Google Drive `appDataFolder` sync,
- vault code unlock,
- recovery key unlock,
- recovery key rotation,
- change vault code,
- vault-code lockout,
- encrypted backup export,
- encrypted backup restore,
- corrupted/tampered backup rejection,
- wrong backup password rejection after the v0.2.1 hotfix,
- Authenticator Lock with 6-digit authenticator app codes,
- vault code required before authenticator code,
- Authenticator Lock OFF -> ON -> OFF -> ON without forcing a new QR every time,
- broken/old/wrong authenticator QR recovery,
- Trust Center Authenticator card connected to the real settings flow,
- Optional / Active status with Set up / Manage actions,
- Device Trust detection for same-account multi-device use,
- untrusted-device restrictions,
- backup/export blocked on untrusted devices,
- Trust Center blocked on untrusted devices,
- current device cannot be forgotten by accident,
- Streamer / Privacy mode warning and feedback,
- safe-close protection while sync/refresh/background sync is running,
- auto-refresh behavior,
- faster settings feedback.

---

## 4. Automated test evidence

Current automated test status during v0.2.1 testing:

```text
30/30 tests passing
```

Covered automated areas include:

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

Latest important security fix verification for commit `51d7d38`:

- Debug build: passed.
- Release build: passed.
- Automated tests: 30/30 passed.
- Release safety script: passed.
- Fix committed and pushed to `main`.

---

## 5. Release ZIP and plaintext leakage self-test

A local attacker-style self-test was run using:

```text
scripts/Security-SelfTest-v0.2.1.ps1
```

Marker used:

```text
QF_ATTACK_TEST_SECRET_2026_DO_NOT_USE
```

Self-test summary:

```text
PASS: 8
FAIL: 0
WARN: 0
```

Passed checks:

- Release ZIP contains `QuickForge Sync.exe`.
- Release ZIP contains `credentials.json`.
- Release ZIP blocked-file scan passed.
- No `token*.json`, `client_secret*.json`, `.qfvault`, `.qfbackup`, or `.pdb` files were found in the release ZIP.
- Fake attack marker was not found in the release ZIP.
- Fake attack marker was not found in exported backup files.
- Fake attack marker was not found in local AppData files.
- Corrupted backup copy was created for manual import rejection testing.

Interpretation:

- This proves release/package safety checks and plaintext-marker leakage checks.
- This does not prove the whole app is secure.
- It is useful evidence that obvious plaintext secret leakage was not found in the tested ZIP, backup file, or local app data.

---

## 6. Backup and restore security testing

### 6.1 Encrypted backup export

Result: PASS

Expected:

- Backup is exported as an encrypted `.qfvault` file.
- Backup should still require the vault code or recovery key to restore.
- Backup file should not expose plaintext secrets casually.

Actual:

- Exported backup was created.
- Self-test did not find the fake attack marker in the backup file.

---

### 6.2 Corrupted/tampered backup import

Result: PASS

Expected:

- A corrupted/tampered backup copy must be rejected.

Actual:

- QuickForge rejected the corrupted backup copy.
- The app showed a restore failure message and did not import the corrupted backup.

Interpretation:

- Damaged/tampered backup data was not accepted.

---

### 6.3 Valid backup import with correct vault code / recovery key

Result: PASS

Expected:

- A valid encrypted backup should restore only when the correct vault code or recovery key is entered.

Actual:

- QuickForge verified the backup.
- QuickForge showed the backup preview.
- QuickForge restored the backup successfully with the correct secret.
- QuickForge uploaded/replaced the cloud vault after restore.

Note:

- One tested backup contained `0` saved entries, so it proved the import/decryption/restore flow. A future test should repeat this with a backup containing several fake entries.

---

### 6.4 Valid backup import with wrong vault code: bug found

Result before fix: FAIL

Expected:

- A valid encrypted backup with the wrong vault code/recovery key must be rejected.

Actual before fix:

- While the vault was already unlocked, the wrong typed code could still succeed if the backup matched the current in-memory data key.

Root cause:

- The restore flow first tried the typed vault code.
- Then it tried the typed recovery key.
- If both failed, and the app was already unlocked, it silently tried the current in-memory data key.
- That fallback let the already-unlocked session bypass the backup password prompt for a backup using the same data key.

Risk:

- The backup was still encrypted.
- But the restore prompt did not truly require the typed vault code/recovery key in that unlocked-session scenario.
- This was a real security/UX bug and was not acceptable for v0.2.1 release.

Fix:

- Removed the `DecryptVaultWithExistingDataKey` fallback from backup import.
- Backup import now requires the typed vault code or recovery key.
- Existing unlocked data key can no longer bypass the backup password prompt.

Fixed in:

```text
51d7d38 Require backup password during import
```

Verification after fix:

- PASS: code verification confirmed the existing-data-key fallback was removed from backup import.
- PASS: Debug build succeeded.
- PASS: Release build succeeded.
- PASS: Automated tests passed 30/30.
- PASS: Release safety script passed.
- PASS: fix was committed and pushed to `main`.

Retest requirement:

- Rebuilt v0.2.1 ZIP must be used.
- Old uploaded ZIP must be replaced.
- Valid backup + wrong vault code must be rejected in the rebuilt app.
- Valid backup + correct vault code/recovery key must still restore.

---

## 7. Authenticator Lock testing

Implemented and tested:

- optional 6-digit authenticator app codes,
- vault code required before authenticator code,
- setup/manage flow no longer freezes,
- OFF -> ON -> OFF -> ON behavior without forcing a new QR every time,
- QR setup confirmation before enabling,
- recovery path for broken, deleted, old, or wrong QR setups,
- recovery key remains emergency path if authenticator access is lost,
- ON/OFF UI made faster with safe background sync.

Manual test results:

- PASS: setup flow works.
- PASS: wrong authenticator code is rejected.
- PASS: vault remains locked after wrong authenticator code.
- PASS: correct current authenticator code unlocks.
- PASS: OFF -> ON -> OFF -> ON behavior works.
- PASS: broken/old QR recovery flow works.
- PASS: unlock order requires vault code before authenticator code.
- PASS: Authenticator Lock changes feel faster after final sync behavior fix.

Known limitation:

- Authenticator Lock still needs deeper automated test coverage around old-code replay, TOTP time-window behavior, and sync conflict edge cases.

---

## 8. Trust Center and Device Trust testing

Implemented and tested:

- Trust Center Authenticator card connected to the real Authenticator Lock settings flow,
- Optional / Active status wording,
- Set up / Manage actions,
- Trust Center opens quickly without blocking on cloud refresh,
- background refresh continues after UI opens,
- improved same-account multi-device detection,
- new/untrusted devices sync their registration automatically,
- trusted device can see and approve new device after refresh/background sync,
- untrusted devices cannot access sensitive actions,
- backup/export blocked on untrusted devices,
- Trust Center access blocked on untrusted devices,
- current device cannot be forgotten by accident,
- other non-current devices can still be forgotten.

Manual test results:

- PASS: Trust Center card opens correct Authenticator flow.
- PASS: Trust Center opens quickly after delay fix.
- PASS: UI updates correctly after Authenticator changes.
- PASS: new device detection restored after auto-sync fix.
- PASS: Trust Center and Device Trust open quickly after removing blocking refresh.
- PASS: untrusted device restrictions block sensitive actions.
- PASS: backup/export is blocked on untrusted devices.
- PASS: Trust Center is blocked on untrusted devices.
- PASS: same Google account alone is not enough to become trusted.
- PASS: current device cannot be forgotten.
- PASS: other devices can still be managed.

Known limitation:

- Device Trust still relies on cloud sync/refresh rather than live push between devices.

---

## 9. Streamer / Privacy mode testing

Implemented and tested:

- Streamer mode for safer screen sharing, screenshots, and recordings,
- clearer warning/feedback when Streamer mode is off and sensitive details may be visible,
- faster Streamer mode save feedback,
- Streamer mode changes queue background sync instead of showing annoying sync warning popups.

Manual test results:

- PASS: Streamer mode save feedback improved.
- PASS: annoying sync warning popup behavior removed.
- PASS: Streamer mode warning/feedback included in v0.2.1.

---

## 10. Settings feedback and sync testing

Implemented and tested:

- Auto-lock settings feedback improved,
- Auto-refresh settings feedback improved,
- Background animation settings feedback improved,
- Recovery reminder settings feedback improved,
- Streamer mode settings feedback improved,
- safe-close protection while sync/refresh/background sync is running,
- background sync queue and retry behavior,
- auto-refresh behavior restored after it was accidentally not running correctly,
- sync status color/wording improved to avoid constant scary red state when sync/load was recent,
- Authenticator and Device Trust changes update quickly while syncing safely.

Manual test results:

- PASS: Save actions close/update quickly instead of leaving user confused.
- PASS: settings return/refresh behavior improved.
- PASS: safe-close blocks closing during sync/refresh.
- PASS: auto-refresh restored.
- PASS: Authenticator UI no longer feels too slow.
- PASS: Device Trust UI no longer blocks on cloud refresh.

---

## 11. Manual test case summary

### Core vault

- PASS: create vault.
- PASS: unlock vault.
- PASS: wrong vault code rejected.
- PASS: vault-code lockout behavior.
- PASS: recovery key unlock.
- PASS: recovery key rotation.
- PASS: change vault code.

### Backup and restore

- PASS: encrypted backup export.
- PASS: encrypted backup restore with correct vault code/recovery key.
- PASS: corrupted/tampered backup rejection.
- PASS: backup file not plaintext in self-test marker scan.
- PASS after fix: wrong-code backup import bypass removed.
- PASS after fix: build/test/release-safety checks passed.
- Retest required with rebuilt ZIP: valid backup + wrong code should reject.

### Daily use

- PASS: add entry.
- PASS: edit entry.
- PASS: delete entry confirmation.
- PASS: search/filter entries.
- PASS: favorite entries.
- PASS: copy username.
- PASS: copy password/secret.
- PASS: open website.
- PASS: Open + Fill timing improvements.
- PASS: QuickFill timing improvements.

### Authenticator Lock

- PASS: setup with QR.
- PASS: vault code required first.
- PASS: authenticator code required after vault code.
- PASS: wrong authenticator code rejected.
- PASS: correct authenticator code accepted.
- PASS: OFF -> ON -> OFF -> ON without new QR.
- PASS: wrong/old QR recovery flow.
- PASS: Authenticator UI speed fix.

### Trust Center and Device Trust

- PASS: Trust Center Authenticator card opens real settings flow.
- PASS: Optional / Active status behavior.
- PASS: Device Trust detects same-account multi-device state.
- PASS: new device registration syncs automatically.
- PASS: untrusted-device restrictions.
- PASS: backup/export blocked on untrusted devices.
- PASS: Trust Center blocked on untrusted devices.
- PASS: current device cannot be forgotten.
- PASS: other non-current devices can be forgotten/managed.

### Streamer / Privacy mode

- PASS: Streamer mode setting works.
- PASS: Streamer mode warning/feedback improved.
- PASS: Streamer mode sync warning popup reduced/removed.
- PASS: sensitive UI visibility improved where possible.

### Release safety

- PASS: build and release build.
- PASS: 30/30 automated tests.
- PASS: publish win-x64 self-contained release.
- PASS: release folder safety check.
- PASS: ZIP content safety check.
- PASS: SHA256 generated during release build.
- PASS: README updated for v0.2.1.
- PASS: v0.2.1 branch merged into main.
- PASS: self-security script added.
- PASS: security test history report added.

---

## 12. Current known limitations

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
- long-term crash/offline/bad-network testing is still needed,
- backup restore should be retested with several fake entries after the wrong-code bypass fix.

---

## 13. What should be tested next

Before raising trust level for real-password use, test:

- rebuilt v0.2.1 ZIP after commit `51d7d38`,
- valid backup + wrong vault code after the fix,
- valid backup + correct vault code/recovery key after the fix,
- backup restore with several fake entries,
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
- backup import cannot fall back to current unlocked data key,
- security settings merge correctly across cloud refresh.

---

## 14. Conclusion

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
- self-security test script,
- security test history report,
- and 30/30 automated tests passing.

Most important result from attacker-style testing:

- A real backup import wrong-code bypass was discovered.
- It was fixed in commit `51d7d38`.
- Build, tests, and release safety checks passed after the fix.

Honest status:

- Good enough for controlled beta testing with fake/test data.
- Better security proof than before because failures were found, fixed, and documented.
- Promising for careful personal low-risk use after retesting the rebuilt ZIP.
- Not yet ready to replace a mature audited password manager for critical accounts.

This report should be updated after each release, after each structured tester feedback round, and after any future security bug is found and fixed.
