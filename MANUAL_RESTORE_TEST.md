# QuickForge Sync Manual Restore Test Checklist

Use this checklist before every beta release that changes vault, backup, import, sync, or recovery behavior.

## Test goal

Confirm that a user can recover their vault from an encrypted backup if the cloud vault is deleted, reset, damaged, or unavailable.

## Test environment

- Windows PC
- QuickForge Sync debug or release build
- Google account connected
- Test data only
- At least one exported `.qfvault` backup file

## Test 1: Export backup

Steps:

1. Log in with Google.
2. Unlock or create a vault.
3. Add at least 2 test entries.
4. Include:
   - one normal login
   - one favorite login
   - one entry with website/app link
5. Click `Backup`.
6. Click `Export encrypted backup`.
7. Save the `.qfvault` file somewhere outside the app folder.

Expected result:

- Backup file is created.
- App shows backup export success message.
- App reminds user that the backup still requires vault code or recovery key.

Status:

- [ ] Passed
- [ ] Failed

## Test 2: Reset cloud vault

Steps:

1. Return to the unlock/create screen.
2. Use `Reset Test Vault` if using the developer account.
3. Confirm reset.
4. Type `RESET`.

Expected result:

- Cloud vault is deleted from Google Drive app data.
- App returns to `Create Vault Code`.
- No crash occurs.

Status:

- [ ] Passed
- [ ] Failed

## Test 3: Import encrypted backup from unlock screen

Steps:

1. Stay logged in with Google.
2. Click `Import Backup` from the unlock/create screen.
3. Choose the exported `.qfvault` file.
4. Enter the vault code or recovery key for that backup.
5. Confirm import.

Expected result:

- Backup is verified.
- App shows backup preview.
- Import replaces the cloud vault.
- Vault opens successfully.
- Saved entries appear again.

Status:

- [ ] Passed
- [ ] Failed

## Test 4: Wrong backup code rejection

Steps:

1. Click `Import Backup`.
2. Choose a valid `.qfvault` file.
3. Enter a wrong vault code.

Expected result:

- Import fails.
- App does not replace the cloud vault.
- App does not crash.

Status:

- [ ] Passed
- [ ] Failed

## Test 5: Corrupted/wrong unlock recovery message

Steps:

1. Try to unlock an existing vault with a wrong code.
2. Read the recovery dialog.
3. Choose `No`.
4. Try again and choose `Yes`.

Expected result:

- App explains possible wrong vault code/recovery key.
- App explains possible corrupted cloud vault.
- App offers encrypted backup import.
- Choosing `Yes` opens backup import flow.

Status:

- [ ] Passed
- [ ] Failed


## Test 6: Fresh install restore from Google appDataFolder

Steps:

1. Use a clean Windows profile, second PC, VM, or deleted local QuickForge app data folder.
2. Start QuickForge Sync.
3. Confirm there are no local vault files available.
4. Sign in with the same Google account.
5. Confirm QuickForge detects the app-managed Google Drive appDataFolder vault.
6. Unlock with the vault code.
7. Lock and unlock again using the recovery key.
8. Confirm saved entries, favorites, websites, and settings are restored.

Expected result:

- QuickForge restores from the encrypted cloud vault without needing local files.
- Passwords are only visible after vault unlock.
- If the cloud vault is missing, QuickForge clearly offers create vault or import encrypted backup.
- No plaintext vault data appears in local folders.

Status:

- [ ] Passed
- [ ] Failed
## Release decision

Only release the next beta if all required restore tests pass.

Final result:

- [ ] Ready for beta release
- [ ] Not ready

