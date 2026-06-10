using Microsoft.Win32;

namespace exam_test
{
    public static class LocalHardeningPolicy
    {
        public static bool ShouldLockForSessionSwitch(
            bool isVaultUnlocked,
            SessionSwitchReason reason)
        {
            if (!isVaultUnlocked)
            {
                return false;
            }

            return reason == SessionSwitchReason.SessionLock ||
                   reason == SessionSwitchReason.SessionLogoff ||
                   reason == SessionSwitchReason.ConsoleDisconnect ||
                   reason == SessionSwitchReason.RemoteDisconnect;
        }

        public static bool ShouldLockForPowerModeChange(
            bool isVaultUnlocked,
            PowerModes mode)
        {
            return isVaultUnlocked && mode == PowerModes.Suspend;
        }

        public static string GetSessionLockMessage(SessionSwitchReason reason)
        {
            return reason switch
            {
                SessionSwitchReason.SessionLock => "Vault locked because Windows was locked.",
                SessionSwitchReason.SessionLogoff => "Vault locked because Windows is logging off.",
                SessionSwitchReason.ConsoleDisconnect => "Vault locked because this Windows session disconnected.",
                SessionSwitchReason.RemoteDisconnect => "Vault locked because the remote session disconnected.",
                _ => "Vault locked because Windows changed session state."
            };
        }

        public static string GetPowerModeLockMessage(PowerModes mode)
        {
            return mode == PowerModes.Suspend
                ? "Vault locked because Windows is going to sleep."
                : "Vault locked because Windows power state changed.";
        }

        public static string GetCompromisedPcRiskStatement()
        {
            return "QuickForge encrypts your vault before cloud sync, but a compromised PC can still be dangerous while the vault is unlocked.";
        }
    }
}
