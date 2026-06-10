namespace exam_test
{
    public static class BackgroundSyncPolicy
    {
        public static bool ShouldBlockClose(
            bool hasUnsyncedLocalChanges,
            bool backgroundSyncRunning,
            bool backgroundSyncRequested,
            bool refreshRunning,
            bool deviceTrustRefreshRunning)
        {
            return hasUnsyncedLocalChanges ||
                   backgroundSyncRequested;
        }

        public static string GetCloseBlockedStatus()
        {
            return "Close blocked - unsynced changes pending";
        }

        public static string GetCloseBlockedMessage()
        {
            return MicrocopyPolicy.FormatDialog(
                "Close blocked.",
                "QuickForge still has encrypted local changes that have not finished syncing.",
                "Wait until Sync shows Active, or export an encrypted backup before closing."
            );
        }

        public static string GetQueuedStatus(bool isDeleteSync, bool alreadyRunningOrQueued)
        {
            if (isDeleteSync)
            {
                return alreadyRunningOrQueued
                    ? "Delete sync already running - queued latest request"
                    : "Delete pending";
            }

            return alreadyRunningOrQueued
                ? "Sync already running - queued latest request"
                : "Sync pending";
        }

        public static string GetRunningStatus(bool isDeleteSync)
        {
            return isDeleteSync
                ? "Delete syncing in background..."
                : "Syncing in background...";
        }
    }
}

