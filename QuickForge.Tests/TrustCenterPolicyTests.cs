using System;
using exam_test;
using Xunit;

namespace QuickForge.Tests
{
    public class TrustCenterPolicyTests
    {
        [Fact]
        public void HasPendingSync_ReturnsTrue_WhenUnsyncedChangesExist()
        {
            Assert.True(TrustCenterPolicy.HasPendingSync(
                hasUnsyncedLocalChanges: true,
                backgroundSyncRunning: false,
                backgroundSyncRequested: false));
        }

        [Fact]
        public void ShouldReviewRecoveryKey_ReturnsTrue_WhenOld()
        {
            DateTime now = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);

            Assert.True(TrustCenterPolicy.ShouldReviewRecoveryKey(
                now.AddDays(-181),
                rotationRequired: false,
                now));
        }

        [Fact]
        public void GetSyncStatus_ShowsPendingSync()
        {
            string status = TrustCenterPolicy.GetSyncStatus(
                lastCloudLoadUtc: DateTime.UtcNow,
                lastCloudSaveUtc: DateTime.UtcNow,
                hasPendingSync: true);

            Assert.Equal("Pending sync", status);
        }

        [Fact]
        public void GetSafetyWizardStatus_ShowsSkipped()
        {
            string status = TrustCenterPolicy.GetSafetyWizardStatus(
                completedAtUtc: null,
                skippedAtUtc: DateTime.UtcNow);

            Assert.Contains("skipped", status);
        }

        [Fact]
        public void OverallStatus_RecommendsReview_WhenNoAuthenticator()
        {
            string status = TrustCenterPolicy.GetOverallStatus(
                hasUntrustedDevices: false,
                recoveryNeedsReview: false,
                hasRecentBackup: true,
                hasPasswordIssues: false,
                hasPendingSync: false,
                authenticatorLockActive: false,
                safetyWizardCompleted: true);

            Assert.Equal("Review recommended", status);
        }
    }
}
