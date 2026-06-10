using exam_test;
using Xunit;

namespace QuickForge.Tests
{
    public class ResponsivenessFeedbackPolicyTests
    {
        [Fact]
        public void GetQueuedSyncStatus_ShowsQueuedWhenNoSyncRunning()
        {
            Assert.Equal("Sync queued", ResponsivenessFeedbackPolicy.GetQueuedSyncStatus(false));
        }

        [Fact]
        public void GetQueuedSyncStatus_ShowsAlreadyRunningWhenSyncRunning()
        {
            Assert.Contains("queued", ResponsivenessFeedbackPolicy.GetQueuedSyncStatus(true));
        }

        [Fact]
        public void GetRefreshStartedStatus_IsBackgroundFocused()
        {
            Assert.Contains("background", ResponsivenessFeedbackPolicy.GetRefreshStartedStatus());
        }

        [Fact]
        public void GetButtonBusyText_AddsEllipsis()
        {
            Assert.Equal("Refreshing...", ResponsivenessFeedbackPolicy.GetButtonBusyText("Refreshing"));
        }
    }
}
