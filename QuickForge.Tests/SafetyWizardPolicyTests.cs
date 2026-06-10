using System;
using exam_test;
using Xunit;

namespace QuickForge.Tests
{
    public class SafetyWizardPolicyTests
    {
        [Fact]
        public void ShouldShowFirstRunWizard_WhenNotCompletedOrSkipped()
        {
            var settings = new VaultSettings();

            Assert.True(SafetyWizardPolicy.ShouldShowFirstRunWizard(settings));
        }

        [Fact]
        public void ShouldShowFirstRunWizard_ReturnsFalse_WhenCompleted()
        {
            var settings = new VaultSettings
            {
                SafetyWizardCompletedAtUtc = DateTime.UtcNow
            };

            Assert.False(SafetyWizardPolicy.ShouldShowFirstRunWizard(settings));
        }

        [Fact]
        public void ReadinessStatus_IsCarefulWhenOptionalStepsRemain()
        {
            var readiness = new SafetyWizardReadiness
            {
                StrongVaultCodeConfirmed = true,
                RecoveryKeyConfirmed = true,
                DeviceTrusted = true,
                AutoLockEnabled = true,
                SafeFillExplained = true
            };

            string status = SafetyWizardPolicy.GetReadinessStatus(readiness);

            Assert.Contains("optional", status);
        }

        [Fact]
        public void GetStepText_ShowsReviewForIncompleteStep()
        {
            string text = SafetyWizardPolicy.GetStepText(false, "Enable Authenticator Lock");

            Assert.Contains("Review", text);
            Assert.Contains("Authenticator", text);
        }
    }
}
