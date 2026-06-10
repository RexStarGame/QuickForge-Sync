using exam_test;
using Xunit;

namespace QuickForge.Tests
{
    public class BackgroundSyncPolicyTests
    {
        [Fact]
        public void ShouldBlockClose_BlocksWhenUnsyncedLocalChangesExist()
        {
            Assert.True(BackgroundSyncPolicy.ShouldBlockClose(
                hasUnsyncedLocalChanges: true,
                backgroundSyncRunning: false,
                backgroundSyncRequested: false,
                refreshRunning: false,
                deviceTrustRefreshRunning: false));
        }

        [Fact]
        public void ShouldBlockClose_BlocksWhenBackgroundSyncIsRequested()
        {
            Assert.True(BackgroundSyncPolicy.ShouldBlockClose(
                hasUnsyncedLocalChanges: false,
                backgroundSyncRunning: false,
                backgroundSyncRequested: true,
                refreshRunning: false,
                deviceTrustRefreshRunning: false));
        }

        [Fact]
        public void ShouldBlockClose_DoesNotBlockForRefreshOnly()
        {
            Assert.False(BackgroundSyncPolicy.ShouldBlockClose(
                hasUnsyncedLocalChanges: false,
                backgroundSyncRunning: false,
                backgroundSyncRequested: false,
                refreshRunning: true,
                deviceTrustRefreshRunning: true));
        }

        [Fact]
        public void GetQueuedStatus_ShowsAlreadyRunningWhenSyncIsAlreadyQueued()
        {
            string status = BackgroundSyncPolicy.GetQueuedStatus(
                isDeleteSync: false,
                alreadyRunningOrQueued: true);

            Assert.Contains("already running", status);
            Assert.Contains("queued", status);
        }
    }
}
