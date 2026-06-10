using exam_test;
using Microsoft.Win32;
using Xunit;

namespace QuickForge.Tests
{
    public class LocalHardeningPolicyTests
    {
        [Fact]
        public void ShouldLockForSessionSwitch_LocksUnlockedVaultOnWindowsLock()
        {
            Assert.True(LocalHardeningPolicy.ShouldLockForSessionSwitch(
                isVaultUnlocked: true,
                SessionSwitchReason.SessionLock));
        }

        [Fact]
        public void ShouldLockForSessionSwitch_DoesNotLockWhenVaultAlreadyLocked()
        {
            Assert.False(LocalHardeningPolicy.ShouldLockForSessionSwitch(
                isVaultUnlocked: false,
                SessionSwitchReason.SessionLock));
        }

        [Fact]
        public void ShouldLockForPowerModeChange_LocksUnlockedVaultOnSleep()
        {
            Assert.True(LocalHardeningPolicy.ShouldLockForPowerModeChange(
                isVaultUnlocked: true,
                PowerModes.Suspend));
        }

        [Fact]
        public void RiskStatement_IsHonestAboutCompromisedPc()
        {
            string statement = LocalHardeningPolicy.GetCompromisedPcRiskStatement();

            Assert.Contains("compromised PC", statement);
            Assert.Contains("unlocked", statement);
        }
    }
}
