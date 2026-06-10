using System;
using OtpNet;

namespace exam_test
{
    public class AuthenticatorLockVerificationResult
    {
        public bool Success { get; set; }
        public bool ReplayRejected { get; set; }
        public bool RateLimited { get; set; }
        public string UserMessage { get; set; } = "";
        public long? TimeWindowUsed { get; set; }
        public int RemainingAttempts { get; set; }
        public DateTime? LockoutUntilUtc { get; set; }
    }

    public static class AuthenticatorLockService
    {
        public const int MaxFailedAttempts = 3;
        public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(10);

        public static AuthenticatorLockVerificationResult VerifyCode(
            VaultSettings settings,
            string authenticatorCode,
            DateTime utcNow)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            utcNow = EnsureUtc(utcNow);

            if (!settings.AuthenticatorLockEnabled)
            {
                return new AuthenticatorLockVerificationResult
                {
                    Success = true,
                    UserMessage = "Authenticator Lock is not enabled."
                };
            }

            if (settings.AuthenticatorLockoutUntilUtc.HasValue &&
                settings.AuthenticatorLockoutUntilUtc.Value > utcNow)
            {
                return new AuthenticatorLockVerificationResult
                {
                    Success = false,
                    RateLimited = true,
                    UserMessage = "Authenticator temporarily locked. Too many wrong codes were entered. Try again later.",
                    RemainingAttempts = 0,
                    LockoutUntilUtc = settings.AuthenticatorLockoutUntilUtc
                };
            }

            if (string.IsNullOrWhiteSpace(settings.AuthenticatorSecretBase32))
            {
                return new AuthenticatorLockVerificationResult
                {
                    Success = false,
                    UserMessage = "Authenticator Lock is enabled, but no authenticator secret is configured."
                };
            }

            if (string.IsNullOrWhiteSpace(authenticatorCode))
            {
                return RecordWrongCode(settings, utcNow, "Authenticator code required.");
            }

            string normalizedCode = authenticatorCode.Trim().Replace(" ", "");

            if (normalizedCode.Length != 6)
            {
                return RecordWrongCode(settings, utcNow, "Authenticator code must be 6 digits.");
            }

            byte[] secretBytes;

            try
            {
                secretBytes = Base32Encoding.ToBytes(settings.AuthenticatorSecretBase32);
            }
            catch
            {
                return new AuthenticatorLockVerificationResult
                {
                    Success = false,
                    UserMessage = "Authenticator setup is invalid. Use recovery to reset Authenticator Lock."
                };
            }

            var totp = new Totp(secretBytes, step: 30);
            bool valid = totp.VerifyTotp(
                utcNow,
                normalizedCode,
                out long timeWindowUsed,
                new VerificationWindow(previous: 1, future: 1));

            if (!valid)
            {
                return RecordWrongCode(settings, utcNow, "Wrong authenticator code.");
            }

            if (settings.LastAuthenticatorTimeWindowUsed.HasValue &&
                timeWindowUsed <= settings.LastAuthenticatorTimeWindowUsed.Value)
            {
                return new AuthenticatorLockVerificationResult
                {
                    Success = false,
                    ReplayRejected = true,
                    UserMessage = "Authenticator code already used. Wait for a new code and try again.",
                    TimeWindowUsed = timeWindowUsed,
                    RemainingAttempts = RemainingAttempts(settings),
                    LockoutUntilUtc = settings.AuthenticatorLockoutUntilUtc
                };
            }

            settings.LastAuthenticatorTimeWindowUsed = timeWindowUsed;
            settings.AuthenticatorFailedAttempts = 0;
            settings.AuthenticatorLockoutUntilUtc = null;

            return new AuthenticatorLockVerificationResult
            {
                Success = true,
                UserMessage = "Authenticator code accepted.",
                TimeWindowUsed = timeWindowUsed,
                RemainingAttempts = MaxFailedAttempts
            };
        }

        public static long GetTimeWindow(DateTime utcNow)
        {
            utcNow = EnsureUtc(utcNow);
            return new DateTimeOffset(utcNow).ToUnixTimeSeconds() / 30;
        }

        private static AuthenticatorLockVerificationResult RecordWrongCode(
            VaultSettings settings,
            DateTime utcNow,
            string message)
        {
            settings.AuthenticatorFailedAttempts++;

            if (settings.AuthenticatorFailedAttempts >= MaxFailedAttempts)
            {
                settings.AuthenticatorFailedAttempts = 0;
                settings.AuthenticatorLockoutUntilUtc = utcNow.Add(LockoutDuration);

                return new AuthenticatorLockVerificationResult
                {
                    Success = false,
                    RateLimited = true,
                    UserMessage = "Authenticator temporarily locked. Too many wrong codes were entered. Try again later.",
                    RemainingAttempts = 0,
                    LockoutUntilUtc = settings.AuthenticatorLockoutUntilUtc
                };
            }

            return new AuthenticatorLockVerificationResult
            {
                Success = false,
                UserMessage = message + " Attempts left: " + RemainingAttempts(settings) + ".",
                RemainingAttempts = RemainingAttempts(settings),
                LockoutUntilUtc = settings.AuthenticatorLockoutUntilUtc
            };
        }

        private static int RemainingAttempts(VaultSettings settings)
        {
            return Math.Max(0, MaxFailedAttempts - settings.AuthenticatorFailedAttempts);
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
            {
                return value;
            }

            if (value.Kind == DateTimeKind.Local)
            {
                return value.ToUniversalTime();
            }

            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }
}
