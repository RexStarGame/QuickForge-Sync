using System.Drawing;
using exam_test;
using Xunit;

namespace QuickForge.Tests
{
    public class DialogLayoutPolicyTests
    {
        [Fact]
        public void AuthenticatorUnlockDialog_IsTallEnoughForButtons()
        {
            Size size = DialogLayoutPolicy.GetAuthenticatorUnlockSize();

            Assert.True(size.Width >= 540);
            Assert.True(size.Height >= 330);
        }

        [Fact]
        public void SettingsDialog_IsLargerThanOldCompactLayout()
        {
            Size size = DialogLayoutPolicy.GetSettingsSize();

            Assert.True(size.Width >= 800);
            Assert.True(size.Height >= 700);
        }

        [Fact]
        public void TrustCenterDialog_FitsWithinWorkingArea()
        {
            var workingArea = new Rectangle(0, 0, 1024, 768);

            Size size = DialogLayoutPolicy.GetTrustCenterSize(workingArea);

            Assert.True(size.Width <= 944);
            Assert.True(size.Height <= 688);
        }

        [Fact]
        public void TrustCenterScrollArea_AllowsBottomActions()
        {
            Size size = DialogLayoutPolicy.GetTrustCenterScrollArea();

            Assert.True(size.Height >= 900);
        }
    }
}
