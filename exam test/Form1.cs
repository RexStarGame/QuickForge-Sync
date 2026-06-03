using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace exam_test
{
    public partial class Form1 : Form
    {
        private readonly System.Windows.Forms.Timer animationTimer = new System.Windows.Forms.Timer();

        private float time = 0f;

        private const int TreeDepth = 9;
        private const float BaseLength = 120f;
        private const float BaseSpread = 28f;
        private const float BranchMovement = 9f;

        public Form1()
        {
            InitializeComponent();

            DoubleBuffered = true;
            BackColor = Color.FromArgb(8, 10, 18);

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true
            );

            animationTimer.Interval = 16;
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();

            _ = TestGoogleLoginAsync();
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            // Empty method, safe for Windows Forms Designer.
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            time += 0.016f;

            // Forces the form to redraw smoothly.
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            DrawBackground(g);

            // Static tree base.
            // The tree stays in the same place, but the branches move.
            float startX = ClientSize.Width / 2f;
            float startY = ClientSize.Height * 0.90f;

            DrawOrganicBranchRecursive(
                g,
                startX,
                startY,
                BaseLength,
                -90f,
                TreeDepth
            );
        }

        private void DrawBackground(Graphics g)
        {
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(8, 10, 18)))
            {
                g.FillRectangle(brush, ClientRectangle);
            }

            // Dynamic background circles.
            DrawSoftCircle(
                g,
                ClientSize.Width * 0.25f + (float)Math.Sin(time * 0.35f) * 25f,
                ClientSize.Height * 0.35f + (float)Math.Cos(time * 0.25f) * 18f,
                170f,
                20,
                0.2f
            );

            DrawSoftCircle(
                g,
                ClientSize.Width * 0.75f + (float)Math.Sin(time * 0.22f + 2f) * 30f,
                ClientSize.Height * 0.45f + (float)Math.Cos(time * 0.30f + 1f) * 22f,
                220f,
                14,
                1.4f
            );

            DrawSoftCircle(
                g,
                ClientSize.Width * 0.50f + (float)Math.Sin(time * 0.18f + 4f) * 20f,
                ClientSize.Height * 0.70f + (float)Math.Cos(time * 0.20f + 3f) * 16f,
                260f,
                12,
                2.8f
            );
        }

        private void DrawSoftCircle(
            Graphics g,
            float x,
            float y,
            float radius,
            int alpha,
            float phase)
        {
            float pulse = (float)Math.Sin(time * 0.8f + phase) * 18f;
            float finalRadius = radius + pulse;

            RectangleF rect = new RectangleF(
                x - finalRadius / 2f,
                y - finalRadius / 2f,
                finalRadius,
                finalRadius
            );

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(alpha, 80, 120, 255)))
            {
                g.FillEllipse(brush, rect);
            }
        }
        private async Task TestGoogleLoginAsync()
        {
            try
            {
                var driveService = await GoogleAuthService.LoginAsync();

                MessageBox.Show("Google login virker. Drive service er klar.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Google login fejl: " + ex.Message);
            }
        }
        private void DrawOrganicBranchRecursive(
            Graphics g,
            float x,
            float y,
            float length,
            float angle,
            int currentDepth)
        {
            if (currentDepth <= 0 || length < 2f)
            {
                return;
            }

            // This makes the trunk almost static.
            // The smaller branches move more than the big main branch.
            float branchLevel = (TreeDepth - currentDepth) / (float)TreeDepth;

            float wave =
                (float)Math.Sin(time * 1.6f + currentDepth * 0.75f) *
                BranchMovement *
                branchLevel;

            float animatedAngle = angle + wave;

            float radians = animatedAngle * (float)Math.PI / 180f;

            float endX = x + length * (float)Math.Cos(radians);
            float endY = y + length * (float)Math.Sin(radians);

            int alpha = Math.Min(230, 45 + currentDepth * 22);
            int red = Math.Min(255, 80 + currentDepth * 12);
            int green = Math.Min(255, 135 + currentDepth * 8);
            int blue = 220;

            float thickness = Math.Max(1f, currentDepth * 0.5f);

            using (Pen pen = new Pen(Color.FromArgb(alpha, red, green, blue), thickness))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                g.DrawLine(pen, x, y, endX, endY);
            }

            float nextLength = length * 0.73f;

            // Static spread with a small smooth movement.
            // This makes the branches feel alive without moving the whole tree.
            float spreadMovement = (float)Math.Sin(time * 1.2f + currentDepth) * 4f * branchLevel;
            float nextSpread = BaseSpread + spreadMovement;

            DrawOrganicBranchRecursive(
                g,
                endX,
                endY,
                nextLength,
                animatedAngle - nextSpread,
                currentDepth - 1
            );

            DrawOrganicBranchRecursive(
                g,
                endX,
                endY,
                nextLength,
                animatedAngle + nextSpread,
                currentDepth - 1
            );

            // Small middle branch on every second level.
            // This keeps the shape simple but prettier.
            if (currentDepth % 2 == 0)
            {
                DrawOrganicBranchRecursive(
                    g,
                    endX,
                    endY,
                    nextLength * 0.62f,
                    animatedAngle,
                    currentDepth - 2
                );
            }
        }
    }
}