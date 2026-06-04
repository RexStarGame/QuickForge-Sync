using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using exam_test;

namespace QuickForge.Tests
{
    public class VaultCryptoServiceTests
    {
        private static VaultData CreateSampleVault()
        {
            return new VaultData
            {
                Entries = new List<VaultEntry>
                {
                    new VaultEntry
                    {
                        Platform = "SteamTest123",
                        Username = "fake@example.com",
                        Secret = "MyFakePassword123!",
                        Website = "https://example.com",
                        Note = "only test",
                        CreatedAt = new DateTime(2026, 1, 1),
                        IsFavorite = true
                    }
                },
                Settings = new VaultSettings
                {
                    RecoveryKeyReminderDays = 90,
                    LastRecoveryKeyRotatedAt = new DateTime(2026, 1, 1),
                    BackgroundAnimationEnabled = true,
                    AutoLockMinutes = 10
                },
                UpdatedAt = new DateTime(2026, 1, 1)
            };
        }

        [Fact]
        public void CorrectVaultCode_DecryptsVault()
        {
            string vaultCode = "correct-vault-code";
            string recoveryKey = VaultCryptoService.GenerateRecoveryKey();

            VaultData originalVault = CreateSampleVault();

            string encryptedJson = VaultCryptoService.CreateEncryptedVault(
                originalVault,
                vaultCode,
                recoveryKey,
                out byte[] dataKey,
                out EncryptedVaultFile encryptedVaultFile
            );

            VaultData decryptedVault = VaultCryptoService.DecryptVault(
                encryptedJson,
                vaultCode,
                out byte[] decryptedDataKey,
                out EncryptedVaultFile decryptedEncryptedVaultFile
            );

            Assert.Single(decryptedVault.Entries);
            Assert.Equal("SteamTest123", decryptedVault.Entries[0].Platform);
            Assert.Equal("fake@example.com", decryptedVault.Entries[0].Username);
            Assert.Equal("MyFakePassword123!", decryptedVault.Entries[0].Secret);
            Assert.True(decryptedVault.Entries[0].IsFavorite);
        }

        [Fact]
        public void RecoveryKey_DecryptsVault()
        {
            string vaultCode = "correct-vault-code";
            string recoveryKey = VaultCryptoService.GenerateRecoveryKey();

            VaultData originalVault = CreateSampleVault();

            string encryptedJson = VaultCryptoService.CreateEncryptedVault(
                originalVault,
                vaultCode,
                recoveryKey,
                out byte[] dataKey,
                out EncryptedVaultFile encryptedVaultFile
            );

            VaultData decryptedVault = VaultCryptoService.DecryptVault(
                encryptedJson,
                recoveryKey,
                out byte[] decryptedDataKey,
                out EncryptedVaultFile decryptedEncryptedVaultFile
            );

            Assert.Single(decryptedVault.Entries);
            Assert.Equal("SteamTest123", decryptedVault.Entries[0].Platform);
            Assert.Equal("MyFakePassword123!", decryptedVault.Entries[0].Secret);
        }

        [Fact]
        public void WrongVaultCode_Fails()
        {
            string vaultCode = "correct-vault-code";
            string wrongCode = "wrong-vault-code";
            string recoveryKey = VaultCryptoService.GenerateRecoveryKey();

            VaultData originalVault = CreateSampleVault();

            string encryptedJson = VaultCryptoService.CreateEncryptedVault(
                originalVault,
                vaultCode,
                recoveryKey,
                out byte[] dataKey,
                out EncryptedVaultFile encryptedVaultFile
            );

            Assert.ThrowsAny<Exception>(() =>
            {
                VaultCryptoService.DecryptVault(
                    encryptedJson,
                    wrongCode,
                    out byte[] decryptedDataKey,
                    out EncryptedVaultFile decryptedEncryptedVaultFile
                );
            });
        }

        [Fact]
        public void EncryptedJson_DoesNotContainPlaintextSecrets()
        {
            string vaultCode = "correct-vault-code";
            string recoveryKey = VaultCryptoService.GenerateRecoveryKey();

            VaultData originalVault = CreateSampleVault();

            string encryptedJson = VaultCryptoService.CreateEncryptedVault(
                originalVault,
                vaultCode,
                recoveryKey,
                out byte[] dataKey,
                out EncryptedVaultFile encryptedVaultFile
            );

            Assert.DoesNotContain("SteamTest123", encryptedJson);
            Assert.DoesNotContain("fake@example.com", encryptedJson);
            Assert.DoesNotContain("MyFakePassword123!", encryptedJson);
            Assert.DoesNotContain("only test", encryptedJson);
        }

        [Fact]
        public void RotateRecoveryKey_NewRecoveryKeyWorks_OldRecoveryKeyFails()
        {
            string vaultCode = "correct-vault-code";
            string oldRecoveryKey = VaultCryptoService.GenerateRecoveryKey();
            string newRecoveryKey = VaultCryptoService.GenerateRecoveryKey();

            VaultData originalVault = CreateSampleVault();

            string encryptedJson = VaultCryptoService.CreateEncryptedVault(
                originalVault,
                vaultCode,
                oldRecoveryKey,
                out byte[] dataKey,
                out EncryptedVaultFile encryptedVaultFile
            );

            VaultData decryptedWithOldKeyBeforeRotation = VaultCryptoService.DecryptVault(
                encryptedJson,
                oldRecoveryKey,
                out byte[] oldKeyDataKey,
                out EncryptedVaultFile oldKeyEncryptedVaultFile
            );

            Assert.Equal("MyFakePassword123!", decryptedWithOldKeyBeforeRotation.Entries[0].Secret);

            VaultCryptoService.RotateRecoveryKey(
                encryptedVaultFile,
                dataKey,
                newRecoveryKey
            );

            string rotatedEncryptedJson = JsonSerializer.Serialize(encryptedVaultFile);

            VaultData decryptedWithNewKey = VaultCryptoService.DecryptVault(
                rotatedEncryptedJson,
                newRecoveryKey,
                out byte[] newKeyDataKey,
                out EncryptedVaultFile newKeyEncryptedVaultFile
            );

            Assert.Equal("MyFakePassword123!", decryptedWithNewKey.Entries[0].Secret);

            Assert.ThrowsAny<Exception>(() =>
            {
                VaultCryptoService.DecryptVault(
                    rotatedEncryptedJson,
                    oldRecoveryKey,
                    out byte[] failedOldKeyDataKey,
                    out EncryptedVaultFile failedOldKeyEncryptedVaultFile
                );
            });
        }

        [Fact]
        public void ChangeVaultCode_NewVaultCodeWorks_OldVaultCodeFails()
        {
            string oldVaultCode = "old-vault-code";
            string newVaultCode = "new-vault-code";
            string recoveryKey = VaultCryptoService.GenerateRecoveryKey();

            VaultData originalVault = CreateSampleVault();

            string encryptedJson = VaultCryptoService.CreateEncryptedVault(
                originalVault,
                oldVaultCode,
                recoveryKey,
                out byte[] dataKey,
                out EncryptedVaultFile encryptedVaultFile
            );

            VaultData decryptedWithOldCodeBeforeChange = VaultCryptoService.DecryptVault(
                encryptedJson,
                oldVaultCode,
                out byte[] oldCodeDataKey,
                out EncryptedVaultFile oldCodeEncryptedVaultFile
            );

            Assert.Equal("MyFakePassword123!", decryptedWithOldCodeBeforeChange.Entries[0].Secret);

            VaultCryptoService.ChangeVaultCode(
                encryptedVaultFile,
                dataKey,
                newVaultCode
            );

            string changedCodeEncryptedJson = JsonSerializer.Serialize(encryptedVaultFile);

            VaultData decryptedWithNewCode = VaultCryptoService.DecryptVault(
                changedCodeEncryptedJson,
                newVaultCode,
                out byte[] newCodeDataKey,
                out EncryptedVaultFile newCodeEncryptedVaultFile
            );

            Assert.Equal("MyFakePassword123!", decryptedWithNewCode.Entries[0].Secret);

            Assert.ThrowsAny<Exception>(() =>
            {
                VaultCryptoService.DecryptVault(
                    changedCodeEncryptedJson,
                    oldVaultCode,
                    out byte[] failedOldCodeDataKey,
                    out EncryptedVaultFile failedOldCodeEncryptedVaultFile
                );
            });
        }

        [Fact]
        public void EncryptedBackupFile_CanBeImportedWithVaultCode()
        {
            string vaultCode = "correct-vault-code";
            string recoveryKey = VaultCryptoService.GenerateRecoveryKey();

            string backupPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "QuickForge-Backup-" + Guid.NewGuid().ToString("N") + ".qfvault"
            );

            try
            {
                VaultData originalVault = CreateSampleVault();

                string encryptedJson = VaultCryptoService.CreateEncryptedVault(
                    originalVault,
                    vaultCode,
                    recoveryKey,
                    out byte[] dataKey,
                    out EncryptedVaultFile encryptedVaultFile
                );

                System.IO.File.WriteAllText(backupPath, encryptedJson);

                Assert.True(System.IO.File.Exists(backupPath));

                string importedBackupJson = System.IO.File.ReadAllText(backupPath);

                Assert.DoesNotContain("SteamTest123", importedBackupJson);
                Assert.DoesNotContain("fake@example.com", importedBackupJson);
                Assert.DoesNotContain("MyFakePassword123!", importedBackupJson);
                Assert.DoesNotContain("only test", importedBackupJson);

                VaultData importedVault = VaultCryptoService.DecryptVault(
                    importedBackupJson,
                    vaultCode,
                    out byte[] importedDataKey,
                    out EncryptedVaultFile importedEncryptedVaultFile
                );

                Assert.Single(importedVault.Entries);
                Assert.Equal("SteamTest123", importedVault.Entries[0].Platform);
                Assert.Equal("fake@example.com", importedVault.Entries[0].Username);
                Assert.Equal("MyFakePassword123!", importedVault.Entries[0].Secret);
            }
            finally
            {
                if (System.IO.File.Exists(backupPath))
                {
                    System.IO.File.Delete(backupPath);
                }
            }
        }

        [Fact]
        public void EncryptedBackupFile_CanBeImportedWithRecoveryKey()
        {
            string vaultCode = "correct-vault-code";
            string recoveryKey = VaultCryptoService.GenerateRecoveryKey();

            string backupPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "QuickForge-Backup-" + Guid.NewGuid().ToString("N") + ".qfvault"
            );

            try
            {
                VaultData originalVault = CreateSampleVault();

                string encryptedJson = VaultCryptoService.CreateEncryptedVault(
                    originalVault,
                    vaultCode,
                    recoveryKey,
                    out byte[] dataKey,
                    out EncryptedVaultFile encryptedVaultFile
                );

                System.IO.File.WriteAllText(backupPath, encryptedJson);

                string importedBackupJson = System.IO.File.ReadAllText(backupPath);

                VaultData importedVault = VaultCryptoService.DecryptVault(
                    importedBackupJson,
                    recoveryKey,
                    out byte[] importedDataKey,
                    out EncryptedVaultFile importedEncryptedVaultFile
                );

                Assert.Single(importedVault.Entries);
                Assert.Equal("SteamTest123", importedVault.Entries[0].Platform);
                Assert.Equal("MyFakePassword123!", importedVault.Entries[0].Secret);
            }
            finally
            {
                if (System.IO.File.Exists(backupPath))
                {
                    System.IO.File.Delete(backupPath);
                }
            }
        }

        [Fact]
        public void TamperedEncryptedBackupFile_FailsToImport()
        {
            string vaultCode = "correct-vault-code";
            string recoveryKey = VaultCryptoService.GenerateRecoveryKey();

            VaultData originalVault = CreateSampleVault();

            string encryptedJson = VaultCryptoService.CreateEncryptedVault(
                originalVault,
                vaultCode,
                recoveryKey,
                out byte[] dataKey,
                out EncryptedVaultFile encryptedVaultFile
            );

            byte[] cipherBytes = Convert.FromBase64String(encryptedVaultFile.VaultCipherText);

            Assert.True(cipherBytes.Length > 0);

            cipherBytes[0] = (byte)(cipherBytes[0] ^ 0xFF);

            encryptedVaultFile.VaultCipherText = Convert.ToBase64String(cipherBytes);

            string tamperedEncryptedJson = JsonSerializer.Serialize(encryptedVaultFile);

            Assert.ThrowsAny<Exception>(() =>
            {
                VaultCryptoService.DecryptVault(
                    tamperedEncryptedJson,
                    vaultCode,
                    out byte[] tamperedDataKey,
                    out EncryptedVaultFile tamperedEncryptedVaultFile
                );
            });
        }
    }
}


