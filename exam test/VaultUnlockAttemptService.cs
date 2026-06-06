using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace exam_test
{
    public class VaultUnlockAttemptState
    {
        public int FailedAttempts { get; set; } = 0;
        public int LockoutLevel { get; set; } = 0;
        public DateTime LockedUntilUtc { get; set; } = DateTime.MinValue;
    }

    public static class VaultUnlockAttemptService
    {
        public const int MaxFailedAttempts = 3;

        public static VaultUnlockAttemptState LoadState(string accountId)
        {
            string path = GetStatePath(accountId);

            if (!File.Exists(path))
            {
                return new VaultUnlockAttemptState();
            }

            try
            {
                string json = File.ReadAllText(path);

                return JsonSerializer.Deserialize<VaultUnlockAttemptState>(json)
                    ?? new VaultUnlockAttemptState();
            }
            catch
            {
                return new VaultUnlockAttemptState();
            }
        }

        public static void SaveState(string accountId, VaultUnlockAttemptState state)
        {
            string path = GetStatePath(accountId);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            string json = JsonSerializer.Serialize(
                state,
                new JsonSerializerOptions { WriteIndented = true }
            );

            File.WriteAllText(path, json);
        }

        public static void ResetAfterSuccessfulUnlock(string accountId)
        {
            SaveState(accountId, new VaultUnlockAttemptState());
        }

        public static VaultUnlockAttemptState RecordFailure(VaultUnlockAttemptState state, DateTime utcNow)
        {
            state.FailedAttempts++;

            if (state.FailedAttempts >= MaxFailedAttempts)
            {
                TimeSpan duration = GetLockoutDuration(state.LockoutLevel);

                state.FailedAttempts = 0;
                state.LockedUntilUtc = utcNow.Add(duration);
                state.LockoutLevel++;
            }

            return state;
        }

        public static bool IsLockedOut(VaultUnlockAttemptState state, DateTime utcNow)
        {
            return state.LockedUntilUtc > utcNow;
        }

        public static int RemainingAttempts(VaultUnlockAttemptState state)
        {
            return Math.Max(0, MaxFailedAttempts - state.FailedAttempts);
        }

        public static TimeSpan GetLockoutDuration(int lockoutLevel)
        {
            if (lockoutLevel <= 0)
            {
                return TimeSpan.FromMinutes(10);
            }

            if (lockoutLevel == 1)
            {
                return TimeSpan.FromMinutes(30);
            }

            return TimeSpan.FromHours(2);
        }

        private static string GetStatePath(string accountId)
        {
            string normalized = string.IsNullOrWhiteSpace(accountId)
                ? "unknown-google-account"
                : accountId.Trim().ToLowerInvariant();

            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
            string hash = Convert.ToHexString(hashBytes);

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "QuickForge",
                "UnlockAttempts",
                hash + ".json"
            );
        }
    }
}
