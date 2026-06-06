using System;
using Xunit;
using exam_test;

namespace QuickForge.Tests
{
    public class VaultUnlockAttemptServiceTests
    {
        [Fact]
        public void ThreeFailures_CreateFirstTenMinuteLockout()
        {
            DateTime now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            VaultUnlockAttemptState state = new VaultUnlockAttemptState();

            state = VaultUnlockAttemptService.RecordFailure(state, now);
            state = VaultUnlockAttemptService.RecordFailure(state, now);
            state = VaultUnlockAttemptService.RecordFailure(state, now);

            Assert.Equal(0, state.FailedAttempts);
            Assert.Equal(1, state.LockoutLevel);
            Assert.Equal(now.AddMinutes(10), state.LockedUntilUtc);
            Assert.True(VaultUnlockAttemptService.IsLockedOut(state, now.AddMinutes(9)));
        }

        [Fact]
        public void SecondLockout_UsesThirtyMinutes()
        {
            DateTime now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            VaultUnlockAttemptState state = new VaultUnlockAttemptState();

            state = VaultUnlockAttemptService.RecordFailure(state, now);
            state = VaultUnlockAttemptService.RecordFailure(state, now);
            state = VaultUnlockAttemptService.RecordFailure(state, now);

            DateTime later = now.AddMinutes(11);

            state = VaultUnlockAttemptService.RecordFailure(state, later);
            state = VaultUnlockAttemptService.RecordFailure(state, later);
            state = VaultUnlockAttemptService.RecordFailure(state, later);

            Assert.Equal(2, state.LockoutLevel);
            Assert.Equal(later.AddMinutes(30), state.LockedUntilUtc);
        }

        [Fact]
        public void ThirdAndLaterLockout_UsesTwoHours()
        {
            Assert.Equal(TimeSpan.FromMinutes(10), VaultUnlockAttemptService.GetLockoutDuration(0));
            Assert.Equal(TimeSpan.FromMinutes(30), VaultUnlockAttemptService.GetLockoutDuration(1));
            Assert.Equal(TimeSpan.FromHours(2), VaultUnlockAttemptService.GetLockoutDuration(2));
            Assert.Equal(TimeSpan.FromHours(2), VaultUnlockAttemptService.GetLockoutDuration(99));
        }
    }
}
