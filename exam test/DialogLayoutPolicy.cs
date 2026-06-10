using System;
using System.Drawing;

namespace exam_test
{
    public static class DialogLayoutPolicy
    {
        public static Size GetAuthenticatorUnlockSize()
        {
            return new Size(560, 350);
        }

        public static Size GetSettingsSize()
        {
            return new Size(820, 720);
        }

        public static Size GetTrustCenterSize(Rectangle workingArea)
        {
            int width = Math.Min(940, Math.Max(860, workingArea.Width - 80));
            int height = Math.Min(900, Math.Max(640, workingArea.Height - 80));

            return new Size(width, height);
        }

        public static Size GetTrustCenterScrollArea()
        {
            return new Size(900, 930);
        }
    }
}

