using System;

namespace exam_test
{
    public class SafeFillPlan
    {
        public bool CanProceed { get; set; }
        public bool FullAutoFillAllowed { get; set; }
        public bool ShouldWarnStreamerModeOff { get; set; }
        public string UserMessage { get; set; } = "";
        public string NextAction { get; set; } = "";
    }

    public static class SafeFillPolicy
    {
        public const int DefaultClipboardClearDelayMs = 20000;

        public static SafeFillPlan BuildStepByStepPlan(
            VaultEntry? entry,
            bool streamerModeEnabled,
            bool deviceTrusted)
        {
            if (!deviceTrusted)
            {
                return new SafeFillPlan
                {
                    CanProceed = false,
                    FullAutoFillAllowed = false,
                    UserMessage = "This device needs approval before Safe Fill can copy credentials.",
                    NextAction = "Approve this device from a trusted device first."
                };
            }

            if (entry == null)
            {
                return new SafeFillPlan
                {
                    CanProceed = false,
                    FullAutoFillAllowed = false,
                    UserMessage = "Select an entry first.",
                    NextAction = "Choose a saved login and try Safe Fill again."
                };
            }

            if (string.IsNullOrWhiteSpace(entry.Website))
            {
                return new SafeFillPlan
                {
                    CanProceed = false,
                    FullAutoFillAllowed = false,
                    UserMessage = "This entry has no website link.",
                    NextAction = "Add a website link or copy credentials manually."
                };
            }

            if (string.IsNullOrWhiteSpace(entry.Username))
            {
                return new SafeFillPlan
                {
                    CanProceed = false,
                    FullAutoFillAllowed = false,
                    UserMessage = "This entry has no username/email.",
                    NextAction = "Add a username/email or copy the password manually."
                };
            }

            if (string.IsNullOrWhiteSpace(entry.Secret))
            {
                return new SafeFillPlan
                {
                    CanProceed = false,
                    FullAutoFillAllowed = false,
                    UserMessage = "This entry has no password/code.",
                    NextAction = "Add a password/code before using Safe Fill."
                };
            }

            return new SafeFillPlan
            {
                CanProceed = true,
                FullAutoFillAllowed = false,
                ShouldWarnStreamerModeOff = !streamerModeEnabled,
                UserMessage = "Safe Fill uses step-by-step copy. QuickForge will not auto-paste credentials.",
                NextAction = "Step 1: username is copied first. Paste it yourself, then use Copy Password when ready."
            };
        }

        public static string GetClipboardCountdownText(int delayMs)
        {
            int seconds = Math.Max(1, delayMs / 1000);
            return "Clipboard clears in " + seconds + " seconds.";
        }
    }
}
