namespace exam_test
{
    public static class ResponsivenessFeedbackPolicy
    {
        public static string GetQueuedSyncStatus(bool alreadyRunning)
        {
            return alreadyRunning
                ? "Sync already running - queued latest request"
                : "Sync queued";
        }

        public static string GetRefreshStartedStatus()
        {
            return "Refreshing in background";
        }

        public static string GetButtonBusyText(string action)
        {
            return string.IsNullOrWhiteSpace(action)
                ? "Working..."
                : action.Trim() + "...";
        }

        public static string GetBackgroundRefreshPreview()
        {
            return "Refresh started in the background. QuickForge will update the vault when Google Drive responds.";
        }
    }
}
