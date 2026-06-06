# QuickForge Sync Multi-Device Test Checklist

Use this checklist before publishing a new beta preview release.

## Test setup

- Device A: first Windows PC
- Device B: second Windows PC or clean Windows user profile
- Same Google account on both devices
- Test data only
- Recovery key saved outside the app
- At least one encrypted backup exported

## 1. Fresh vault creation on Device A

- [ ] Open QuickForge Sync on Device A
- [ ] Sign in with Google
- [ ] Confirm the correct Google email is shown
- [ ] Create a new vault code
- [ ] Save the recovery key
- [ ] Confirm the app opens the empty vault
- [ ] Confirm sync status shows Active
- [ ] Confirm Last save updates after creating the vault

Expected result: Device A creates and uploads an encrypted vault.

## 2. Add test entries on Device A

- [ ] Add a test login entry
- [ ] Add a test game code or note
- [ ] Add one favorite entry
- [ ] Press Sync now
- [ ] Confirm Last save updates

Expected result: entries are saved locally and uploaded to Google Drive.

## 3. Load vault on Device B

- [ ] Open QuickForge Sync on Device B
- [ ] Sign in with the same Google account
- [ ] Confirm the app detects an existing cloud vault
- [ ] Unlock using the vault code or recovery key
- [ ] Confirm all Device A entries appear
- [ ] Confirm favorite status appears correctly
- [ ] Confirm sync status shows Active
- [ ] Confirm Last load updates

Expected result: Device B can load the encrypted cloud vault.

## 4. Manual sync from Device B

- [ ] Add a new test entry on Device B
- [ ] Press Sync now
- [ ] Confirm Last save updates
- [ ] Close QuickForge on Device B

Expected result: Device B uploads the updated encrypted vault.

## 5. Re-open on Device A

- [ ] Re-open QuickForge on Device A
- [ ] Unlock the vault
- [ ] Confirm Device B entry appears
- [ ] Confirm Last load updates

Expected result: Device A can read the latest cloud vault.

## 6. Backup export test

- [ ] Export encrypted backup from Device A
- [ ] Store the .qfvault file somewhere safe
- [ ] Confirm exported file is not empty
- [ ] Confirm the app explains that vault code or recovery key is still required

Expected result: encrypted backup can be created safely.

## 7. Backup import test

- [ ] Use Import Backup from unlock screen or Backup dialog
- [ ] Select the exported .qfvault file
- [ ] Try wrong vault code first
- [ ] Confirm the app shows a helpful restore/import error
- [ ] Try the correct vault code or recovery key
- [ ] Confirm import preview appears
- [ ] Confirm import replaces the cloud vault
- [ ] Confirm Last save updates after restore

Expected result: valid encrypted backup can restore the cloud vault.

## 8. Account switching test

- [ ] Click Logout
- [ ] Confirm warning appears before logout
- [ ] Click No
- [ ] Confirm logout is cancelled
- [ ] Click Logout again
- [ ] Click Yes
- [ ] Confirm app returns to login screen
- [ ] Sign in with a different Google account if available
- [ ] Confirm the app treats it as a different isolated vault

Expected result: account switching is clear and does not silently mix vaults.

## 9. Corrupted backup/cloud failure test

- [ ] Copy a backup file
- [ ] Corrupt the copy by deleting some text from it
- [ ] Try importing the corrupted file
- [ ] Confirm the app explains possible reasons
- [ ] Confirm the app suggests using another backup or recovery key

Expected result: corrupted backup/cloud-vault cases give useful recovery guidance.

## Release decision

Do not mark a beta as real-data ready unless:

- [ ] Multi-device sync works
- [ ] Backup export works
- [ ] Backup import works
- [ ] Wrong-code recovery messages are clear
- [ ] Corrupted backup recovery messages are clear
- [ ] Account switching warning works
- [ ] GitHub Actions pass
- [ ] Local Debug and Release builds pass
- [ ] All tests pass
