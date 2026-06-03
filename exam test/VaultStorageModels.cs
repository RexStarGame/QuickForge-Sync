using System;
using System.Collections.Generic;

namespace exam_test
{
    public class VaultData
    {
        public List<VaultEntry> Entries { get; set; } = new List<VaultEntry>();
        public VaultSettings Settings { get; set; } = new VaultSettings();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class VaultSettings
    {
        // 0 = never, 30 = every 30 days, 90 = every 90 days
        public int RecoveryKeyReminderDays { get; set; } = 0;

        public DateTime LastRecoveryKeyRotatedAt { get; set; } = DateTime.UtcNow;
    }

    public class EncryptedVaultFile
    {
        public int Version { get; set; } = 2;

        public string VaultNonce { get; set; } = "";
        public string VaultTag { get; set; } = "";
        public string VaultCipherText { get; set; } = "";

        public KeyWrapper MasterKeyWrapper { get; set; } = new KeyWrapper();
        public KeyWrapper RecoveryKeyWrapper { get; set; } = new KeyWrapper();
    }

    public class KeyWrapper
    {
        public string Kdf { get; set; } = "PBKDF2-SHA256";
        public int Iterations { get; set; } = 200000;

        public string Salt { get; set; } = "";
        public string Nonce { get; set; } = "";
        public string Tag { get; set; } = "";
        public string EncryptedDataKey { get; set; } = "";
    }
}