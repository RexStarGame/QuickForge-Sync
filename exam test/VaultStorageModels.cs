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
        public int RecoveryKeyReminderDays { get; set; } = 0;
        public DateTime LastRecoveryKeyRotatedAt { get; set; } = DateTime.UtcNow;
        public bool RecoveryKeyRotationRequired { get; set; } = false;

        public bool BackgroundAnimationEnabled { get; set; } = true;
        public int AutoLockMinutes { get; set; } = 10;

        public List<KnownVaultDevice> KnownDevices { get; set; } = new List<KnownVaultDevice>();
        public List<VaultSafetyEvent> SafetyTimeline { get; set; } = new List<VaultSafetyEvent>();

        public string LastChangedByDeviceId { get; set; } = "";
        public string LastChangedByDeviceName { get; set; } = "";
        public DateTime? LastChangedAtUtc { get; set; } = null;

        public DateTime? LastBackupAtUtc { get; set; } = null;
    }

    public class KnownVaultDevice
    {
        public string DeviceId { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public DateTime FirstSeenAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;
        public int SyncCount { get; set; } = 0;
    }

    public class VaultSafetyEvent
    {
        public DateTime EventAtUtc { get; set; } = DateTime.UtcNow;
        public string DeviceId { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public string Action { get; set; } = "";
        public string Detail { get; set; } = "";
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

