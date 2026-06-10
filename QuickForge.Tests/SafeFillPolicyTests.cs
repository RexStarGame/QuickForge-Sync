using exam_test;
using Xunit;

namespace QuickForge.Tests
{
    public class SafeFillPolicyTests
    {
        [Fact]
        public void BuildStepByStepPlan_DisablesFullAutoFillByDefault()
        {
            var entry = new VaultEntry
            {
                Website = "https://example.com",
                Username = "user@example.com",
                Secret = "secret"
            };

            SafeFillPlan plan = SafeFillPolicy.BuildStepByStepPlan(
                entry,
                streamerModeEnabled: true,
                deviceTrusted: true);

            Assert.True(plan.CanProceed);
            Assert.False(plan.FullAutoFillAllowed);
            Assert.Contains("step-by-step", plan.UserMessage);
        }

        [Fact]
        public void BuildStepByStepPlan_BlocksUntrustedDevice()
        {
            var entry = new VaultEntry
            {
                Website = "https://example.com",
                Username = "user@example.com",
                Secret = "secret"
            };

            SafeFillPlan plan = SafeFillPolicy.BuildStepByStepPlan(
                entry,
                streamerModeEnabled: true,
                deviceTrusted: false);

            Assert.False(plan.CanProceed);
            Assert.False(plan.FullAutoFillAllowed);
            Assert.Contains("approval", plan.UserMessage);
        }

        [Fact]
        public void BuildStepByStepPlan_WarnsWhenStreamerModeIsOff()
        {
            var entry = new VaultEntry
            {
                Website = "https://example.com",
                Username = "user@example.com",
                Secret = "secret"
            };

            SafeFillPlan plan = SafeFillPolicy.BuildStepByStepPlan(
                entry,
                streamerModeEnabled: false,
                deviceTrusted: true);

            Assert.True(plan.CanProceed);
            Assert.True(plan.ShouldWarnStreamerModeOff);
        }

        [Fact]
        public void BuildStepByStepPlan_RequiresUsernameAndPassword()
        {
            var entry = new VaultEntry
            {
                Website = "https://example.com",
                Username = "",
                Secret = "secret"
            };

            SafeFillPlan plan = SafeFillPolicy.BuildStepByStepPlan(
                entry,
                streamerModeEnabled: true,
                deviceTrusted: true);

            Assert.False(plan.CanProceed);
            Assert.Contains("username", plan.UserMessage);
        }

        [Fact]
        public void GetClipboardCountdownText_UsesSeconds()
        {
            Assert.Equal("Clipboard clears in 20 seconds.", SafeFillPolicy.GetClipboardCountdownText(20000));
        }
    }
}
