using exam_test;
using Xunit;

namespace QuickForge.Tests
{
    public class MicrocopyPolicyTests
    {
        [Fact]
        public void FormatDialog_IncludesWhatWhyAndNext()
        {
            string message = MicrocopyPolicy.FormatDialog(
                "Sync blocked.",
                "Another device changed the vault.",
                "Refresh first or export a backup before resolving.");

            Assert.Contains("Sync blocked.", message);
            Assert.Contains("Why:", message);
            Assert.Contains("Next:", message);
        }

        [Fact]
        public void FormatDialog_CleansExtraWhitespace()
        {
            string message = MicrocopyPolicy.FormatDialog(
                "  Vault   locked. ",
                "  Windows   was locked. ",
                "  Unlock again. ");

            Assert.DoesNotContain("  Vault", message);
            Assert.Contains("Vault locked.", message);
        }

        [Fact]
        public void LooksActionable_ReturnsTrue_WhenNextStepExists()
        {
            Assert.True(MicrocopyPolicy.LooksActionable(
                "Close blocked. Next: export an encrypted backup before closing."));
        }

        [Fact]
        public void LooksActionable_ReturnsFalse_ForGenericError()
        {
            Assert.False(MicrocopyPolicy.LooksActionable("Something failed."));
        }
    }
}
