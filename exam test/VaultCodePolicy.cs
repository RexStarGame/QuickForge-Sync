using System;
using System.Linq;

namespace exam_test
{
    public static class VaultCodePolicy
    {
        public const int MinimumLength = 12;

        public static bool IsStrongEnough(string vaultCode, out string message)
        {
            if (string.IsNullOrWhiteSpace(vaultCode))
            {
                message = "Vault code cannot be empty.";
                return false;
            }

            if (vaultCode.Length < MinimumLength)
            {
                message = "Use at least 12 characters.";
                return false;
            }

            string lower = vaultCode.ToLowerInvariant();

            string[] blockedWords =
            {
                "password",
                "passw0rd",
                "123456",
                "qwerty",
                "admin",
                "quickforge",
                "vaultcode",
                "letmein",
                "secret",
                "google"
            };

            if (blockedWords.Any(word => lower.Contains(word)))
            {
                message = "Avoid common words, app names, or obvious secrets.";
                return false;
            }

            int groups = 0;

            if (vaultCode.Any(char.IsLower)) groups++;
            if (vaultCode.Any(char.IsUpper)) groups++;
            if (vaultCode.Any(char.IsDigit)) groups++;
            if (vaultCode.Any(ch => !char.IsLetterOrDigit(ch))) groups++;

            if (groups < 3)
            {
                message = "Mix at least 3 types: lowercase, uppercase, numbers, and symbols.";
                return false;
            }

            if (vaultCode.Distinct().Count() < 6)
            {
                message = "Use more varied characters.";
                return false;
            }

            message = "Vault code is strong enough.";
            return true;
        }
    }
}
