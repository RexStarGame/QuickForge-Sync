# QuickForge Sync Test Checklist



Use this checklist before sharing the app with anyone.



## Test Rules



- Use test data only.

- Do not use real passwords.

- Do not use real recovery keys.

- Close the app fully before rebuilding.



## 1. Build Test



Run this command:



cd "C:\\Users\\patri\\source\\repos\-verkun\\exam test"

dotnet build



Expected result:



- Build succeeded

- No compile errors



## 2. Vault Setup



Test steps:



- Sign in with Google

- Create a vault code

- Confirm vault code

- Save/copy/download recovery key

- Confirm that the vault opens



Expected result:



- Vault is created

- Recovery key confirmation is required

- App does not continue if recovery key is not confirmed



## 3. Unlock Existing Vault



Test steps:



- Close and reopen app

- Sign in with Google

- Unlock using vault code

- Lock vault

- Unlock using recovery key



Expected result:



- Vault code works

- Recovery key works

- Wrong code/recovery key is rejected



## 4. Saved Entries



Test steps:



- Add a test login

- Edit it

- Reveal it

- Copy username/password

- Delete it and cancel first

- Delete it and confirm second time



Expected result:



- Entry saves and syncs

- Correct entry is edited

- Reveal hides again after timer

- Clipboard cleanup runs

- Delete confirmation protects the entry



## 5. Password Safety



Test steps:



- Save a weak password

- Save a reused password

- Generate a password

- Open Security Center



Expected result:



- Weak password is detected

- Reused password is detected

- Generator works

- Security Center updates correctly



## 6. QuickFill



Test steps:



- Press Ctrl + Alt + Q

- Search for a saved login

- Copy password

- Fill password

- Add favorite and reopen QuickFill



Expected result:



- QuickFill opens

- Search works

- Favorite entries appear first

- Fill targets the intended text field



## 7. Backup



Test steps:



- Export encrypted backup

- Import encrypted backup

- Try wrong code

- Try correct code



Expected result:



- Backup file is created

- Backup is not plain readable password data

- Wrong code fails

- Correct code imports entries



## 8. Auto-Lock / Close / Logout



Test steps:



- Set auto-lock

- Wait until vault locks

- Close app and reopen

- Logout and reopen



Expected result:



- Auto-lock locks vault

- Closing app keeps Google connected but vault locked

- Logout disconnects Google account



## Current Alpha Verdict



Only share with testers if:



- Build succeeds

- All checklist items pass

- Testers use fake data only

- No real passwords are stored


