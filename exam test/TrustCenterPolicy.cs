using System;

namespace exam_test
{
    public static class TrustCenterPolicy
    {
        public static bool HasPendingSync(
            bool hasUnsyncedLocalChanges,
            bool backgroundSyncRunning,
            bool backgroundSyncRequested)
        {
            return hasUnsyncedLocalChanges || backgroundSyncRunning || backgroundSyncRequested;
        }

        public static bool ShouldReviewRecoveryKey(
            DateTime lastRotatedAtUtc,
            bool rotationRequired,
            DateTime utcNow)
        {
            if (rotationRequired)
            {
                return true;
            }

            return (utcNow - lastRotatedAtUtc.ToUniversalTime()).TotalDays >= 180;
        }

        public static string GetRecoveryKeyStatus(
            DateTime lastRotatedAtUtc,
            bool rotationRequired,
            DateTime utcNow)
        {
            if (rotationRequired)
            {
                return "Rotation required";
            }

            int ageDays = Math.Max(0, (int)Math.Floor((utcNow - lastRotatedAtUtc.ToUniversalTime()).TotalDays));

            if (ageDays >= 180)
            {
                return "Review rotation - " + ageDays + " days old";
            }

            return "Available - rotated " + ageDays + " days ago";
        }

        public static string GetSyncStatus(
            DateTime? lastCloudLoadUtc,
            DateTime? lastCloudSaveUtc,
            bool hasPendingSync)
        {
            if (hasPendingSync)
            {
                return "Pending sync";
            }

            if (!lastCloudLoadUtc.HasValue && !lastCloudSaveUtc.HasValue)
            {
                return "No cloud activity recorded";
            }

            string loadText = lastCloudLoadUtc.HasValue
                ? "Last load: " + lastCloudLoadUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
                : "Last load: not yet";

            string saveText = lastCloudSaveUtc.HasValue
                ? "Last save: " + lastCloudSaveUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
                : "Last save: not yet";

            return loadText + " / " + saveText;
        }

        public static string GetSafetyWizardStatus(DateTime? completedAtUtc, DateTime? skippedAtUtc)
        {
            if (completedAtUtc.HasValue)
            {
                return "Safety Wizard completed";
            }

            if (skippedAtUtc.HasValue)
            {
                return "Safety Wizard skipped";
            }

            return "Safety Wizard not reviewed";
        }

        public static string GetOverallStatus(
            bool hasUntrustedDevices,
            bool recoveryNeedsReview,
            bool hasRecentBackup,
            bool hasPasswordIssues,
            bool hasPendingSync,
            bool authenticatorLockActive,
            bool safetyWizardCompleted)
        {
            if (hasUntrustedDevices ||
                recoveryNeedsReview ||
                !hasRecentBackup ||
                hasPasswordIssues ||
                hasPendingSync ||
                !authenticatorLockActive ||
                !safetyWizardCompleted)
            {
                return "Review recommended";
            }

            return "Candidate beta - no urgent warnings";
        }
    }
}
