using System;
using System.Drawing;

namespace exam_test
{
    public static class DialogLayoutPolicy
    {
        public static Size GetAuthenticatorUnlockSize()
        {
            return new Size(660, 430);
        }

        public static Size GetSettingsSize()
        {
            return new Size(980, 820);
        }

        public static Size GetTrustCenterSize(Rectangle workingArea)
        {
            int width = Math.Min(1180, Math.Max(900, workingArea.Width - 20));
            int height = Math.Min(980, Math.Max(640, workingArea.Height - 40));

            return new Size(width, height);
        }

        public static Size GetTrustCenterScrollArea()
        {
            return new Size(1040, 980);
        }
    }
}
