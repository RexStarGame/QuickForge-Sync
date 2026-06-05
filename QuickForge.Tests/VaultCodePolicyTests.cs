using Xunit;
using exam_test;

namespace QuickForge.Tests
{
    public class VaultCodePolicyTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("123456")]
        [InlineData("password123")]
        [InlineData("quickforge123!")]
        [InlineData("aaaaaaaaaaaa")]
        [InlineData("lowercaseonly")]
        public void WeakVaultCodes_AreRejected(string vaultCode)
        {
            bool result = VaultCodePolicy.IsStrongEnough(vaultCode, out string message);

            Assert.False(result);
            Assert.False(string.IsNullOrWhiteSpace(message));
        }

        [Theory]
        [InlineData("River-Forge-72#Moon")]
        [InlineData("BlueHorse91!Stone")]
        [InlineData("Pixel-Cloud-48?Wolf")]
        public void StrongVaultCodes_AreAccepted(string vaultCode)
        {
            bool result = VaultCodePolicy.IsStrongEnough(vaultCode, out string message);

            Assert.True(result);
            Assert.Equal("Vault code is strong enough.", message);
        }
    }
}
