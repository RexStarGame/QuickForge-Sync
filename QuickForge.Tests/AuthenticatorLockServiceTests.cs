using System;
using exam_test;
using OtpNet;
using Xunit;

namespace QuickForge.Tests
{
    public class AuthenticatorLockServiceTests
    {
        [Fact]
        public void VerifyCode_RejectsReusedTotpTimeWindow()
        {
            byte[] secretBytes = KeyGeneration.GenerateRandomKey(20);
            string secretBase32 = Base32Encoding.ToString(secretBytes);

            var settings = new VaultSettings
            {
                AuthenticatorLockEnabled = true,
                AuthenticatorSecretBase32 = secretBase32
            };

            DateTime now = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);
            var totp = new Totp(secretBytes, step: 30);
            string code = totp.ComputeTotp(now);

            var firstResult = AuthenticatorLockService.VerifyCode(settings, code, now);
            var replayResult = AuthenticatorLockService.VerifyCode(settings, code, now);

            Assert.True(firstResult.Success);
            Assert.False(replayResult.Success);
            Assert.True(replayResult.ReplayRejected);
        }

        [Fact]
        public void VerifyCode_RateLimitsWrongAuthenticatorCodes()
        {
            byte[] secretBytes = KeyGeneration.GenerateRandomKey(20);
            string secretBase32 = Base32Encoding.ToString(secretBytes);

            var settings = new VaultSettings
            {
                AuthenticatorLockEnabled = true,
                AuthenticatorSecretBase32 = secretBase32
            };

            DateTime now = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);
            var totp = new Totp(secretBytes, step: 30);
            string realCode = totp.ComputeTotp(now);
            string wrongCode = realCode == "000000" ? "111111" : "000000";

            AuthenticatorLockService.VerifyCode(settings, wrongCode, now);
            AuthenticatorLockService.VerifyCode(settings, wrongCode, now.AddSeconds(1));
            var thirdWrong = AuthenticatorLockService.VerifyCode(settings, wrongCode, now.AddSeconds(2));

            Assert.False(thirdWrong.Success);
            Assert.True(thirdWrong.RateLimited);
            Assert.NotNull(settings.AuthenticatorLockoutUntilUtc);
        }

        [Fact]
        public void VerifyCode_BlocksAttemptsDuringAuthenticatorLockout()
        {
            byte[] secretBytes = KeyGeneration.GenerateRandomKey(20);
            string secretBase32 = Base32Encoding.ToString(secretBytes);

            DateTime now = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);

            var settings = new VaultSettings
            {
                AuthenticatorLockEnabled = true,
                AuthenticatorSecretBase32 = secretBase32,
                AuthenticatorLockoutUntilUtc = now.AddMinutes(5)
            };

            var result = AuthenticatorLockService.VerifyCode(settings, "123456", now);

            Assert.False(result.Success);
            Assert.True(result.RateLimited);
            Assert.Equal(0, result.RemainingAttempts);
        }

        [Fact]
        public void VerifyCode_UpdatesLastAuthenticatorTimeWindowUsedAfterSuccess()
        {
            byte[] secretBytes = KeyGeneration.GenerateRandomKey(20);
            string secretBase32 = Base32Encoding.ToString(secretBytes);

            var settings = new VaultSettings
            {
                AuthenticatorLockEnabled = true,
                AuthenticatorSecretBase32 = secretBase32
            };

            DateTime now = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);
            var totp = new Totp(secretBytes, step: 30);
            string code = totp.ComputeTotp(now);

            var result = AuthenticatorLockService.VerifyCode(settings, code, now);

            Assert.True(result.Success);
            Assert.NotNull(settings.LastAuthenticatorTimeWindowUsed);
            Assert.Equal(result.TimeWindowUsed, settings.LastAuthenticatorTimeWindowUsed);
            Assert.Equal(0, settings.AuthenticatorFailedAttempts);
            Assert.Null(settings.AuthenticatorLockoutUntilUtc);
        }
    }
}
