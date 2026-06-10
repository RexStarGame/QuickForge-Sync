using System;
using System.Text.RegularExpressions;

namespace exam_test
{
    public static class MicrocopyPolicy
    {
        public static string FormatDialog(string whatHappened, string why, string nextAction)
        {
            return Clean(whatHappened) +
                   Environment.NewLine + Environment.NewLine +
                   "Why: " + Clean(why) +
                   Environment.NewLine + Environment.NewLine +
                   "Next: " + Clean(nextAction);
        }

        public static string FormatStatus(string whatHappened, string why, string nextAction)
        {
            return Clean(whatHappened) + " " + Clean(why) + " " + Clean(nextAction);
        }

        public static bool LooksActionable(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            string normalized = message.ToLowerInvariant();

            return normalized.Contains("next:") ||
                   normalized.Contains("try ") ||
                   normalized.Contains("open ") ||
                   normalized.Contains("click ") ||
                   normalized.Contains("wait ") ||
                   normalized.Contains("export ") ||
                   normalized.Contains("unlock ") ||
                   normalized.Contains("connect ");
        }

        private static string Clean(string value)
        {
            string cleaned = Regex.Replace(value ?? "", @"\s+", " ").Trim();

            if (cleaned.Length == 0)
            {
                return "Not specified.";
            }

            return cleaned;
        }
    }
}
