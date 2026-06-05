using System;
using System.Collections.Generic;
using Xunit;
using exam_test;

namespace QuickForge.Tests
{
    public class VaultCryptoKdfTests
    {
        [Fact]
        public void NewEncryptedVault_UsesStrongerKdfIterations()
        {
            VaultData vaultData = new VaultData
            {
                Entries = new List<VaultEntry>(),
                Settings = new VaultSettings(),
                UpdatedAt = DateTime.UtcNow
            };

            string vaultCode = "River-Forge-72#Moon";
            string recoveryKey = VaultCryptoService.GenerateRecoveryKey();

            VaultCryptoService.CreateEncryptedVault(
                vaultData,
                vaultCode,
                recoveryKey,
                out byte[] dataKey,
                out EncryptedVaultFile encryptedVaultFile
            );

            Assert.True(encryptedVaultFile.MasterKeyWrapper.Iterations >= 600000);
            Assert.True(encryptedVaultFile.RecoveryKeyWrapper.Iterations >= 600000);
        }
    }
}
