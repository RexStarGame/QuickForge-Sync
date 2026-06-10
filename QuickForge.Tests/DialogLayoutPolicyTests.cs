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

            Assert.True(size.Width >= 640);
            Assert.True(size.Height >= 400);
        }

        [Fact]
        public void SettingsDialog_IsLargeEnoughForTabsAndButtons()
        {
            Size size = DialogLayoutPolicy.GetSettingsSize();

            Assert.True(size.Width >= 960);
            Assert.True(size.Height >= 800);
        }

        [Fact]
        public void TrustCenterDialog_FitsWithinWorkingArea()
        {
            var workingArea = new Rectangle(0, 0, 1024, 768);

            Size size = DialogLayoutPolicy.GetTrustCenterSize(workingArea);

            Assert.True(size.Width <= 1004);
            Assert.True(size.Height <= 728);
            Assert.True(size.Width >= 900);
            Assert.True(size.Height >= 640);
        }

        [Fact]
        public void TrustCenterScrollArea_AllowsBottomActions()
        {
            Size size = DialogLayoutPolicy.GetTrustCenterScrollArea();

            Assert.True(size.Width >= 1000);
            Assert.True(size.Height >= 950);
        }
    }
}
