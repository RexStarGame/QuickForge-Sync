namespace exam_test
{
    public class SafetyWizardReadiness
    {
        public bool StrongVaultCodeConfirmed { get; set; }
        public bool RecoveryKeyConfirmed { get; set; }
        public bool AuthenticatorLockEnabled { get; set; }
        public bool DeviceTrusted { get; set; }
        public bool AutoLockEnabled { get; set; }
        public bool BackupReminderEnabled { get; set; }
        public bool SafeFillExplained { get; set; }

        public int CompletedCount()
        {
            int count = 0;

            if (StrongVaultCodeConfirmed) count++;
            if (RecoveryKeyConfirmed) count++;
            if (AuthenticatorLockEnabled) count++;
            if (DeviceTrusted) count++;
            if (AutoLockEnabled) count++;
            if (BackupReminderEnabled) count++;
            if (SafeFillExplained) count++;

            return count;
        }
    }

    public static class SafetyWizardPolicy
    {
        public const int TotalSteps = 7;

        public static bool ShouldShowFirstRunWizard(VaultSettings settings)
        {
            if (settings == null)
            {
                return false;
            }

            return !settings.SafetyWizardCompletedAtUtc.HasValue &&
                   !settings.SafetyWizardSkippedAtUtc.HasValue;
        }

        public static string GetReadinessStatus(SafetyWizardReadiness readiness)
        {
            if (readiness == null)
            {
                return "Safety setup not checked";
            }

            int completed = readiness.CompletedCount();

            if (completed >= TotalSteps)
            {
                return "Ready for careful beta testing";
            }

            if (readiness.StrongVaultCodeConfirmed &&
                readiness.RecoveryKeyConfirmed &&
                readiness.DeviceTrusted &&
                readiness.AutoLockEnabled)
            {
                return "Core safety ready - optional steps remain";
            }

            return "Setup recommended before storing real passwords";
        }

        public static string GetStepText(bool completed, string title)
        {
            return (completed ? "[OK] " : "[Review] ") + title;
        }
    }
}
