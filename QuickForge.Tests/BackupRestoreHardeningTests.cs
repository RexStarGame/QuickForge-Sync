using System;
using System.Collections.Generic;
using Xunit;
using exam_test;

namespace QuickForge.Tests
{
    public class BackupRestoreHardeningTests
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
        public void EmptyEncryptedBackupContent_FailsSafely()
        {
            Assert.ThrowsAny<Exception>(() =>
            {
                VaultCryptoService.DecryptVault(
                    "",
                    "correct-vault-code",
                    out byte[] decryptedDataKey,
                    out EncryptedVaultFile decryptedEncryptedVaultFile
                );
            });
        }

        [Fact]
        public void RandomJsonBackupContent_FailsSafely()
        {
            string randomJson = "{\"not\":\"a quickforge encrypted backup\"}";

            Assert.ThrowsAny<Exception>(() =>
            {
                VaultCryptoService.DecryptVault(
                    randomJson,
                    "correct-vault-code",
                    out byte[] decryptedDataKey,
                    out EncryptedVaultFile decryptedEncryptedVaultFile
                );
            });
        }

        [Fact]
        public void EncryptedBackupRestore_PreservesVaultSettings()
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

            VaultData restoredVault = VaultCryptoService.DecryptVault(
                encryptedJson,
                vaultCode,
                out byte[] restoredDataKey,
                out EncryptedVaultFile restoredEncryptedVaultFile
            );

            Assert.NotNull(restoredVault.Settings);
            Assert.Equal(originalVault.Settings.RecoveryKeyReminderDays, restoredVault.Settings.RecoveryKeyReminderDays);
            Assert.Equal(originalVault.Settings.LastRecoveryKeyRotatedAt, restoredVault.Settings.LastRecoveryKeyRotatedAt);
            Assert.Equal(originalVault.Settings.BackgroundAnimationEnabled, restoredVault.Settings.BackgroundAnimationEnabled);
            Assert.Equal(originalVault.Settings.AutoLockMinutes, restoredVault.Settings.AutoLockMinutes);
        }

        [Fact]
        public void EncryptedBackupRestore_PreservesEntryDetails()
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

            VaultData restoredVault = VaultCryptoService.DecryptVault(
                encryptedJson,
                vaultCode,
                out byte[] restoredDataKey,
                out EncryptedVaultFile restoredEncryptedVaultFile
            );

            Assert.Single(restoredVault.Entries);

            VaultEntry restoredEntry = restoredVault.Entries[0];

            Assert.Equal("SteamTest123", restoredEntry.Platform);
            Assert.Equal("fake@example.com", restoredEntry.Username);
            Assert.Equal("MyFakePassword123!", restoredEntry.Secret);
            Assert.Equal("https://example.com", restoredEntry.Website);
            Assert.Equal("only test", restoredEntry.Note);
            Assert.True(restoredEntry.IsFavorite);
        }

        [Fact]
        public void EncryptedBackupJson_DoesNotContainVaultCodeOrRecoveryKey()
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

            Assert.DoesNotContain(vaultCode, encryptedJson);
            Assert.DoesNotContain(recoveryKey, encryptedJson);
            Assert.DoesNotContain("correct-vault-code", encryptedJson);
        }
    }
}
