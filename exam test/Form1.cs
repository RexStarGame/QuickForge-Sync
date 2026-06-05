using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Apis.Drive.v3;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Security.Cryptography;

namespace exam_test
{
    public partial class Form1 : Form
    {
        private const string AppName = "QuickForge Sync";
        private const string AppStatus = "Beta Preview";
        private const string AppVersion = "v0.1.1-beta-preview";
        private const string AppDisplayName = AppName + " " + AppStatus;


        private readonly System.Windows.Forms.Timer animationTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer hideRevealTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer autoLockTimer = new System.Windows.Forms.Timer();

        private float time = 0f;

        private const int TreeDepth = 9;
        private const float BaseLength = 120f;
        private const float BaseSpread = 28f;
        private const float BranchMovement = 9f;

        private string vaultCode = "";
        private readonly List<VaultEntry> vaultEntries = new List<VaultEntry>();
        private DriveService? currentDriveService;
        private bool cloudVaultExists = false;
        private byte[]? currentDataKey;
        private EncryptedVaultFile? currentEncryptedVaultFile;
        private bool isVaultUnlocked = false;
        private const int SecretAccessMinutes = 10;
        private DateTime secretAccessValidUntilUtc = DateTime.MinValue;
        private VaultSettings currentVaultSettings = new VaultSettings();
        private bool hasShownRecoveryReminderThisSession = false;

        private readonly NotifyIcon trayIcon = new NotifyIcon();
        private readonly ContextMenuStrip trayMenu = new ContextMenuStrip();

        private Form? quickFillForm;
        private TextBox? quickFillSearchBox;
        private ListBox? quickFillListBox;
        private Label? quickFillStatusLabel;

        private const int QuickFillHotkeyId = 9001;
        private const int WmHotkey = 0x0312;

        private const uint ModAlt = 0x0001;
        private const uint ModControl = 0x0002;
        private const uint VkQ = 0x51;

        private IntPtr quickFillTargetWindow = IntPtr.Zero;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        // Top bar
        private readonly Panel topBarPanel = new Panel();
        private readonly Label appTitleLabel = new Label();
        private readonly Label appSubtitleLabel = new Label();
        private readonly Label accountStatusLabel = new Label();
        private readonly Button aboutButton = new Button();
        private readonly Button logoutButton = new Button();

        // Google login card
        private readonly Panel loginCard = new Panel();
        private readonly Label googleIconLabel = new Label();
        private readonly Label googleTitleLabel = new Label();
        private readonly Label googleSubtitleLabel = new Label();

        // Vault unlock/create panel
        private readonly Panel vaultAccessPanel = new Panel();
        private readonly Label vaultAccessTitleLabel = new Label();
        private readonly Label vaultAccessSubtitleLabel = new Label();
        private readonly Label vaultCodeLabel = new Label();
        private readonly TextBox vaultCodeTextBox = new TextBox();
        private readonly Label confirmVaultCodeLabel = new Label();
        private readonly TextBox confirmVaultCodeTextBox = new TextBox();
        private readonly Button createVaultButton = new Button();

        // Vault workspace
        private readonly Panel vaultPanel = new Panel();
        private readonly Label vaultTitleLabel = new Label();
        private readonly Label vaultSubtitleLabel = new Label();

        private readonly Label platformLabel = new Label();
        private readonly TextBox platformTextBox = new TextBox();

        private readonly Label usernameLabel = new Label();
        private readonly TextBox usernameTextBox = new TextBox();

        private readonly Label secretLabel = new Label();
        private readonly TextBox secretTextBox = new TextBox();
        private readonly Label passwordStrengthLabel = new Label();
        private readonly Panel passwordStrengthTrack = new Panel();
        private readonly Panel passwordStrengthFill = new Panel();
        private readonly Label websiteLabel = new Label();
        private readonly TextBox websiteTextBox = new TextBox();
        private readonly Label noteLabel = new Label();
        private readonly TextBox noteTextBox = new TextBox();

        private readonly Button saveEntryButton = new Button();
        private readonly Button clearButton = new Button();
        private readonly Button createPasswordButton = new Button();

        private readonly Button editEntryButton = new Button();
        private readonly Button saveChangesButton = new Button();
        private readonly Button cancelEditButton = new Button();

        private VaultEntry? editingEntry = null;
        private int editingEntryIndex = -1;

        private readonly Button openSiteButton = new Button();
        private readonly Button openAndFillButton = new Button();

        private readonly TextBox vaultSearchTextBox = new TextBox();
        private readonly ListBox vaultListBox = new ListBox();
        private readonly TextBox selectedPreviewLabel = new TextBox();

        private readonly List<VaultEntry> visibleVaultEntries = new List<VaultEntry>();

        private readonly Button revealButton = new Button();
        private readonly Button copySecretButton = new Button();
        private readonly Button copyUsernameButton = new Button();
        private readonly Button deleteEntryButton = new Button();
        private readonly Button favoriteButton = new Button();
        private readonly Button lockVaultButton = new Button();

        private readonly Button changeVaultCodeButton = new Button();
        private readonly Button backupButton = new Button();
        private readonly Button securityCenterButton = new Button();
        private readonly Label securitySettingsLabel = new Label();
        private readonly Label recoveryReminderLabel = new Label();
        private readonly ComboBox recoveryReminderComboBox = new ComboBox();
        private readonly Button rotateRecoveryKeyButton = new Button();

        private readonly Label performanceSettingsLabel = new Label();
        private readonly CheckBox animationEnabledCheckBox = new CheckBox();
        private readonly Label autoLockLabel = new Label();
        private readonly ComboBox autoLockComboBox = new ComboBox();

        private DateTime lastVaultActivityUtc = DateTime.UtcNow;
        // Colors
        private readonly Color backgroundColor = Color.FromArgb(8, 10, 18);
        private readonly Color panelColor = Color.FromArgb(18, 22, 36);
        private readonly Color cardColor = Color.FromArgb(30, 35, 55);
        private readonly Color cardHoverColor = Color.FromArgb(42, 50, 78);
        private readonly Color borderColor = Color.FromArgb(100, 160, 255);
        private readonly Color successColor = Color.FromArgb(120, 255, 170);
        private readonly Color dangerColor = Color.FromArgb(255, 140, 140);
        private readonly Color softTextColor = Color.FromArgb(180, 190, 210);

        public Form1()
        {
            InitializeComponent();
            ClientSize = new Size(800, 720);
            Text = AppDisplayName + " " + AppVersion;
            MinimumSize = new Size(800, 720);
            DoubleBuffered = true;
            BackColor = backgroundColor;

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true
            );

            CreateTopBarUi();
            CreateLoginUi();
            CreateVaultAccessUi();
            CreateVaultUi();
            CreateTrayIcon();

            ShowLoggedOutUi();

            RegisterHotKey(
                Handle,
                QuickFillHotkeyId,
                ModControl | ModAlt,
                VkQ
            );

            animationTimer.Interval = 16;
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();

            hideRevealTimer.Interval = 15000;
            hideRevealTimer.Tick += HideRevealTimer_Tick;

            autoLockTimer.Interval = 30000;
            autoLockTimer.Tick += AutoLockTimer_Tick;
            autoLockTimer.Start();
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            // Empty method, safe for Windows Forms Designer.
        }

        private void CreateTrayIcon()
        {
            trayMenu.Items.Add("Open QuickForge", null, (s, e) =>
            {
                ShowMainWindow();
            });

            trayMenu.Items.Add("QuickFill", null, (s, e) =>
            {
                ShowQuickFill();
            });

            trayMenu.Items.Add("Lock vault", null, (s, e) =>
            {
                if (isVaultUnlocked)
                {
                    LockVaultButton_Click(this, EventArgs.Empty);
                }
            });

            trayMenu.Items.Add("Exit", null, (s, e) =>
            {
                Close();
            });

            trayIcon.Text = AppDisplayName + " " + AppVersion;
            trayIcon.Icon = SystemIcons.Shield;
            trayIcon.Visible = true;
            trayIcon.ContextMenuStrip = trayMenu;

            trayIcon.DoubleClick += (s, e) =>
            {
                ShowMainWindow();
            };
        }

        private void ShowMainWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();

            MarkVaultActivity();
            UpdateAnimationState();
        }

        private void CreateTopBarUi()
        {
            topBarPanel.Left = 18;
            topBarPanel.Top = 16;
            topBarPanel.Width = 760;
            topBarPanel.Height = 82;
            topBarPanel.BackColor = panelColor;

            appTitleLabel.Text = AppDisplayName + " " + AppVersion;
            appTitleLabel.ForeColor = Color.White;
            appTitleLabel.BackColor = Color.Transparent;
            appTitleLabel.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            appTitleLabel.AutoSize = true;
            appTitleLabel.Left = 18;
            appTitleLabel.Top = 13;

            appSubtitleLabel.Text = AppVersion + " — use test data only. Do not store real passwords yet.";
            appSubtitleLabel.ForeColor = softTextColor;
            appSubtitleLabel.BackColor = Color.Transparent;
            appSubtitleLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            appSubtitleLabel.AutoSize = true;
            appSubtitleLabel.Left = 18;
            appSubtitleLabel.Top = 42;

            accountStatusLabel.Text = "Not connected";
            accountStatusLabel.ForeColor = softTextColor;
            accountStatusLabel.BackColor = Color.Transparent;
            accountStatusLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            accountStatusLabel.AutoSize = false;
            accountStatusLabel.TextAlign = ContentAlignment.MiddleRight;
            accountStatusLabel.Left = 335;
            accountStatusLabel.Top = 22;
            accountStatusLabel.Width = 305;
            accountStatusLabel.Height = 30;

            aboutButton.Text = "About";
            aboutButton.Width = 80;
            aboutButton.Height = 32;
            aboutButton.Left = 560;
            aboutButton.Top = 22;
            aboutButton.FlatStyle = FlatStyle.Flat;
            aboutButton.ForeColor = Color.White;
            aboutButton.BackColor = Color.FromArgb(35, 40, 60);
            aboutButton.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);
            aboutButton.Click += AboutButton_Click;

            logoutButton.Text = "Logout";
            logoutButton.Width = 88;
            logoutButton.Height = 32;
            logoutButton.Left = 655;
            logoutButton.Top = 22;
            logoutButton.Enabled = false;
            logoutButton.FlatStyle = FlatStyle.Flat;
            logoutButton.ForeColor = Color.White;
            logoutButton.BackColor = Color.FromArgb(35, 40, 60);
            logoutButton.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);
            logoutButton.Click += LogoutButton_Click;

            topBarPanel.Controls.Add(appTitleLabel);
            topBarPanel.Controls.Add(appSubtitleLabel);
            topBarPanel.Controls.Add(accountStatusLabel);
            topBarPanel.Controls.Add(logoutButton);

            Controls.Add(topBarPanel);
            topBarPanel.BringToFront();
        }

        private void AboutButton_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                AppDisplayName + " " + AppVersion + Environment.NewLine + Environment.NewLine +
                "Encrypted Windows vault with Google Drive appdata sync." + Environment.NewLine + Environment.NewLine +
                "Status: Beta Preview" + Environment.NewLine +
                "Use test data only. Do not store real passwords yet." + Environment.NewLine + Environment.NewLine +
                "Tested:" + Environment.NewLine +
                "- Public release ZIP works" + Environment.NewLine +
                "- Google login works on separate accounts" + Environment.NewLine +
                "- Vault data is isolated per Google account" + Environment.NewLine +
                "- 9 automated crypto/backup tests pass",
                "About QuickForge Sync",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        private void CreateLoginUi()
        {
            loginCard.Left = 175;
            loginCard.Top = 145;
            loginCard.Width = 450;
            loginCard.Height = 275;
            loginCard.BackColor = cardColor;
            loginCard.Cursor = Cursors.Hand;
            loginCard.Paint += Card_Paint;
            loginCard.Click += GoogleLoginCard_Click;
            loginCard.MouseEnter += LoginCard_MouseEnter;
            loginCard.MouseLeave += LoginCard_MouseLeave;

            Label welcomeTitleLabel = new Label();
            welcomeTitleLabel.Text = "Welcome to QuickForge Sync Beta Preview";
            welcomeTitleLabel.ForeColor = Color.White;
            welcomeTitleLabel.BackColor = Color.Transparent;
            welcomeTitleLabel.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            welcomeTitleLabel.AutoSize = true;
            welcomeTitleLabel.Left = 22;
            welcomeTitleLabel.Top = 20;
            welcomeTitleLabel.Cursor = Cursors.Hand;
            welcomeTitleLabel.Click += GoogleLoginCard_Click;

            Label welcomeSubtitleLabel = new Label();
            welcomeSubtitleLabel.Text = "Encrypted vault sync for passwords, codes, notes and private snippets.";
            welcomeSubtitleLabel.ForeColor = softTextColor;
            welcomeSubtitleLabel.BackColor = Color.Transparent;
            welcomeSubtitleLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            welcomeSubtitleLabel.AutoSize = true;
            welcomeSubtitleLabel.Left = 22;
            welcomeSubtitleLabel.Top = 52;
            welcomeSubtitleLabel.Cursor = Cursors.Hand;
            welcomeSubtitleLabel.Click += GoogleLoginCard_Click;

            Label bulletLabel = new Label();
            bulletLabel.Text =
                "• Your vault is encrypted before sync" + Environment.NewLine +
                "• Your data stays in your own Google Drive app data" + Environment.NewLine +
                "• Use test data only during beta" + Environment.NewLine +
                "• Save your recovery key safely";
            bulletLabel.ForeColor = softTextColor;
            bulletLabel.BackColor = Color.Transparent;
            bulletLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            bulletLabel.Left = 22;
            bulletLabel.Top = 88;
            bulletLabel.Width = 400;
            bulletLabel.Height = 92;
            bulletLabel.Cursor = Cursors.Hand;
            bulletLabel.Click += GoogleLoginCard_Click;

            Label betaWarningLabel = new Label();
            betaWarningLabel.Text = "Beta Preview: do not store real passwords yet.";
            betaWarningLabel.ForeColor = Color.FromArgb(255, 190, 90);
            betaWarningLabel.BackColor = Color.Transparent;
            betaWarningLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            betaWarningLabel.AutoSize = true;
            betaWarningLabel.Left = 22;
            betaWarningLabel.Top = 178;
            betaWarningLabel.Cursor = Cursors.Hand;
            betaWarningLabel.Click += GoogleLoginCard_Click;

            Panel googleButtonPanel = new Panel();
            googleButtonPanel.Left = 22;
            googleButtonPanel.Top = 215;
            googleButtonPanel.Width = 400;
            googleButtonPanel.Height = 42;
            googleButtonPanel.BackColor = Color.FromArgb(45, 90, 160);
            googleButtonPanel.Cursor = Cursors.Hand;
            googleButtonPanel.Click += GoogleLoginCard_Click;

            googleIconLabel.Text = "G";
            googleIconLabel.ForeColor = Color.White;
            googleIconLabel.BackColor = Color.FromArgb(66, 133, 244);
            googleIconLabel.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            googleIconLabel.TextAlign = ContentAlignment.MiddleCenter;
            googleIconLabel.Left = 14;
            googleIconLabel.Top = 5;
            googleIconLabel.Width = 32;
            googleIconLabel.Height = 32;
            googleIconLabel.Cursor = Cursors.Hand;
            googleIconLabel.Click += GoogleLoginCard_Click;

            googleTitleLabel.Text = "Continue with Google";
            googleTitleLabel.ForeColor = Color.White;
            googleTitleLabel.BackColor = Color.Transparent;
            googleTitleLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            googleTitleLabel.AutoSize = true;
            googleTitleLabel.Left = 60;
            googleTitleLabel.Top = 6;
            googleTitleLabel.Cursor = Cursors.Hand;
            googleTitleLabel.Click += GoogleLoginCard_Click;

            googleSubtitleLabel.Text = "Sync your encrypted vault to your own Google account";
            googleSubtitleLabel.ForeColor = Color.FromArgb(220, 230, 255);
            googleSubtitleLabel.BackColor = Color.Transparent;
            googleSubtitleLabel.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            googleSubtitleLabel.AutoSize = true;
            googleSubtitleLabel.Left = 60;
            googleSubtitleLabel.Top = 24;
            googleSubtitleLabel.Cursor = Cursors.Hand;
            googleSubtitleLabel.Click += GoogleLoginCard_Click;

            googleButtonPanel.Controls.Add(googleIconLabel);
            googleButtonPanel.Controls.Add(googleTitleLabel);
            googleButtonPanel.Controls.Add(googleSubtitleLabel);

            loginCard.Controls.Add(welcomeTitleLabel);
            loginCard.Controls.Add(welcomeSubtitleLabel);
            loginCard.Controls.Add(bulletLabel);
            loginCard.Controls.Add(betaWarningLabel);
            loginCard.Controls.Add(googleButtonPanel);

            Controls.Add(loginCard);
            loginCard.BringToFront();
        }
        private void CreateVaultAccessUi()
        {
            vaultAccessPanel.Left = 175;
            vaultAccessPanel.Top = 135;
            vaultAccessPanel.Width = 450;
            vaultAccessPanel.Height = 250;
            vaultAccessPanel.BackColor = Color.FromArgb(16, 20, 34);

            vaultAccessTitleLabel.Text = "Create Vault Code";
            vaultAccessTitleLabel.ForeColor = Color.White;
            vaultAccessTitleLabel.BackColor = Color.Transparent;
            vaultAccessTitleLabel.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            vaultAccessTitleLabel.AutoSize = true;
            vaultAccessTitleLabel.Left = 24;
            vaultAccessTitleLabel.Top = 20;

            vaultAccessSubtitleLabel.Text = "This code protects reveal/copy actions inside this session.";
            vaultAccessSubtitleLabel.ForeColor = softTextColor;
            vaultAccessSubtitleLabel.BackColor = Color.Transparent;
            vaultAccessSubtitleLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            vaultAccessSubtitleLabel.AutoSize = true;
            vaultAccessSubtitleLabel.Left = 24;
            vaultAccessSubtitleLabel.Top = 55;

            vaultCodeLabel.Text = "Vault code";
            vaultCodeLabel.ForeColor = Color.White;
            vaultCodeLabel.BackColor = Color.Transparent;
            vaultCodeLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            vaultCodeLabel.AutoSize = true;
            vaultCodeLabel.Left = 24;
            vaultCodeLabel.Top = 92;

            vaultCodeTextBox.Left = 24;
            vaultCodeTextBox.Top = 114;
            vaultCodeTextBox.Width = 390;
            vaultCodeTextBox.Height = 26;
            vaultCodeTextBox.UseSystemPasswordChar = true;
            vaultCodeTextBox.PlaceholderText = "Create a vault code";

            confirmVaultCodeLabel.Text = "Confirm vault code";
            confirmVaultCodeLabel.ForeColor = Color.White;
            confirmVaultCodeLabel.BackColor = Color.Transparent;
            confirmVaultCodeLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            confirmVaultCodeLabel.AutoSize = true;
            confirmVaultCodeLabel.Left = 24;
            confirmVaultCodeLabel.Top = 150;

            confirmVaultCodeTextBox.Left = 24;
            confirmVaultCodeTextBox.Top = 172;
            confirmVaultCodeTextBox.Width = 390;
            confirmVaultCodeTextBox.Height = 26;
            confirmVaultCodeTextBox.UseSystemPasswordChar = true;
            confirmVaultCodeTextBox.PlaceholderText = "Repeat vault code";

            createVaultButton.Text = "Unlock Vault";
            createVaultButton.Left = 24;
            createVaultButton.Top = 210;
            createVaultButton.Width = 120;
            createVaultButton.Height = 32;
            createVaultButton.FlatStyle = FlatStyle.Flat;
            createVaultButton.ForeColor = Color.White;
            createVaultButton.BackColor = Color.FromArgb(45, 90, 160);
            createVaultButton.FlatAppearance.BorderColor = borderColor;
            createVaultButton.Click += CreateVaultButton_Click;

            vaultAccessPanel.Controls.Add(vaultAccessTitleLabel);
            vaultAccessPanel.Controls.Add(vaultAccessSubtitleLabel);
            vaultAccessPanel.Controls.Add(vaultCodeLabel);
            vaultAccessPanel.Controls.Add(vaultCodeTextBox);
            vaultAccessPanel.Controls.Add(confirmVaultCodeLabel);
            vaultAccessPanel.Controls.Add(confirmVaultCodeTextBox);
            vaultAccessPanel.Controls.Add(createVaultButton);

            Controls.Add(vaultAccessPanel);
            vaultAccessPanel.BringToFront();
        }

        private void CreateVaultUi()
        {
            vaultPanel.Left = 70;
            vaultPanel.Top = 120;
            vaultPanel.Width = 660;
            vaultPanel.Height = 560;
            vaultPanel.BackColor = Color.FromArgb(16, 20, 34);

            vaultTitleLabel.Text = "Encrypted Vault";
            vaultTitleLabel.ForeColor = Color.White;
            vaultTitleLabel.BackColor = Color.Transparent;
            vaultTitleLabel.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            vaultTitleLabel.AutoSize = true;
            vaultTitleLabel.Left = 20;
            vaultTitleLabel.Top = 16;

            vaultSubtitleLabel.Text = "Save accounts, game codes, recovery notes, license keys and private snippets.";
            vaultSubtitleLabel.ForeColor = softTextColor;
            vaultSubtitleLabel.BackColor = Color.Transparent;
            vaultSubtitleLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            vaultSubtitleLabel.AutoSize = true;
            vaultSubtitleLabel.Left = 20;
            vaultSubtitleLabel.Top = 48;

            platformLabel.Text = "Service / Platform";
            platformLabel.ForeColor = Color.White;
            platformLabel.BackColor = Color.Transparent;
            platformLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            platformLabel.AutoSize = true;
            platformLabel.Left = 20;
            platformLabel.Top = 82;

            platformTextBox.Left = 20;
            platformTextBox.Top = 104;
            platformTextBox.Width = 250;
            platformTextBox.Height = 26;
            platformTextBox.PlaceholderText = "Example: YouTube, Steam, Facebook";

            usernameLabel.Text = "Username / Email";
            usernameLabel.ForeColor = Color.White;
            usernameLabel.BackColor = Color.Transparent;
            usernameLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            usernameLabel.AutoSize = true;
            usernameLabel.Left = 20;
            usernameLabel.Top = 136;

            usernameTextBox.Left = 20;
            usernameTextBox.Top = 158;
            usernameTextBox.Width = 250;
            usernameTextBox.Height = 26;
            usernameTextBox.PlaceholderText = "Optional username or email";

            secretLabel.Text = "Password / Secret / Code";
            secretLabel.ForeColor = Color.White;
            secretLabel.BackColor = Color.Transparent;
            secretLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            secretLabel.AutoSize = true;
            secretLabel.Left = 20;
            secretLabel.Top = 190;

            secretTextBox.Left = 20;
            secretTextBox.Top = 212;
            secretTextBox.Width = 165;
            secretTextBox.Height = 26;
            secretTextBox.PlaceholderText = "Optional password or code";
            secretTextBox.UseSystemPasswordChar = true;

            passwordStrengthLabel.Text = "Strength: Not checked yet";
            passwordStrengthLabel.Left = 20;
            passwordStrengthLabel.Top = 244;
            passwordStrengthLabel.Width = 250;
            passwordStrengthLabel.Height = 20;
            passwordStrengthLabel.ForeColor = softTextColor;
            passwordStrengthLabel.BackColor = Color.Transparent;
            passwordStrengthLabel.Font = new Font("Segoe UI", 8, FontStyle.Bold);

            passwordStrengthTrack.Left = 20;
            passwordStrengthTrack.Top = 266;
            passwordStrengthTrack.Width = 250;
            passwordStrengthTrack.Height = 7;
            passwordStrengthTrack.BackColor = Color.FromArgb(35, 40, 60);

            passwordStrengthFill.Left = 0;
            passwordStrengthFill.Top = 0;
            passwordStrengthFill.Width = 0;
            passwordStrengthFill.Height = 7;
            passwordStrengthFill.BackColor = softTextColor;

            passwordStrengthTrack.Controls.Add(passwordStrengthFill);

            secretTextBox.TextChanged += (s, e) => UpdatePasswordStrengthPreview();
            platformTextBox.TextChanged += (s, e) => UpdatePasswordStrengthPreview();

            createPasswordButton.Text = "Generate";
            createPasswordButton.Left = 195;
            createPasswordButton.Top = 212;
            createPasswordButton.Width = 75;
            createPasswordButton.Height = 26;
            createPasswordButton.FlatStyle = FlatStyle.Flat;
            createPasswordButton.UseVisualStyleBackColor = false;
            createPasswordButton.ForeColor = Color.White;
            createPasswordButton.BackColor = Color.FromArgb(45, 90, 160);
            createPasswordButton.FlatAppearance.BorderColor = borderColor;
            createPasswordButton.Click += (s, e) => ShowCreatePasswordDialog(PasswordGeneratorTarget.VaultField);

            websiteLabel.Text = "Website / App link";
            websiteLabel.ForeColor = Color.White;
            websiteLabel.BackColor = Color.Transparent;
            websiteLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            websiteLabel.AutoSize = true;
            websiteLabel.Left = 20;
            websiteLabel.Top = 286;

            websiteTextBox.Left = 20;
            websiteTextBox.Top = 308;
            websiteTextBox.Width = 250;
            websiteTextBox.Height = 26;
            websiteTextBox.PlaceholderText = "Example: https://accounts.google.com";
            noteLabel.Text = "Note / Description";
            noteLabel.ForeColor = Color.White;
            noteLabel.BackColor = Color.Transparent;
            noteLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            noteLabel.AutoSize = true;
            noteLabel.Left = 20;
            noteLabel.Top = 340;

            noteTextBox.Left = 20;
            noteTextBox.Top = 362;
            noteTextBox.Width = 250;
            noteTextBox.Height = 26;
            noteTextBox.PlaceholderText = "Optional note";

            saveEntryButton.Text = "Save entry";
            saveEntryButton.Left = 20;
            saveEntryButton.Top = 398;
            saveEntryButton.Width = 95;
            saveEntryButton.Height = 28;
            saveEntryButton.FlatStyle = FlatStyle.Flat;
            saveEntryButton.ForeColor = Color.White;
            saveEntryButton.BackColor = Color.FromArgb(45, 90, 160);
            saveEntryButton.FlatAppearance.BorderColor = borderColor;
            saveEntryButton.Click += SaveEntryButton_Click;

            clearButton.Text = "Clear";
            clearButton.Left = 125;
            clearButton.Top = 398;
            clearButton.Width = 80;
            clearButton.Height = 28;
            clearButton.FlatStyle = FlatStyle.Flat;
            clearButton.ForeColor = Color.White;
            clearButton.BackColor = Color.FromArgb(35, 40, 60);
            clearButton.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);
            clearButton.Click += ClearButton_Click;

            saveChangesButton.Text = "Save changes";
            saveChangesButton.Left = 20;
            saveChangesButton.Top = saveEntryButton.Top;
            saveChangesButton.Width = 110;
            saveChangesButton.Height = 28;
            saveChangesButton.FlatStyle = FlatStyle.Flat;
            saveChangesButton.UseVisualStyleBackColor = false;
            saveChangesButton.ForeColor = Color.White;
            saveChangesButton.BackColor = Color.FromArgb(45, 90, 160);
            saveChangesButton.FlatAppearance.BorderColor = borderColor;
            saveChangesButton.Visible = false;
            saveChangesButton.Click += SaveChangesButton_Click;

            cancelEditButton.Text = "Cancel edit";
            cancelEditButton.Left = 140;
            cancelEditButton.Top = saveEntryButton.Top;
            cancelEditButton.Width = 100;
            cancelEditButton.Height = 28;
            cancelEditButton.FlatStyle = FlatStyle.Flat;
            cancelEditButton.UseVisualStyleBackColor = false;
            cancelEditButton.ForeColor = Color.White;
            cancelEditButton.BackColor = Color.FromArgb(35, 40, 60);
            cancelEditButton.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);
            cancelEditButton.Visible = false;
            cancelEditButton.Click += CancelEditButton_Click;

            vaultSearchTextBox.Left = 315;
            vaultSearchTextBox.Top = 82;
            vaultSearchTextBox.Width = 310;
            vaultSearchTextBox.Height = 26;
            vaultSearchTextBox.PlaceholderText = "Search saved entries...";
            vaultSearchTextBox.BackColor = Color.FromArgb(24, 28, 44);
            vaultSearchTextBox.ForeColor = Color.White;
            vaultSearchTextBox.BorderStyle = BorderStyle.FixedSingle;
            vaultSearchTextBox.TextChanged += (s, e) => RefreshVaultList();

            vaultListBox.Left = 315;
            vaultListBox.Top = 114;
            vaultListBox.Width = 310;
            vaultListBox.Height = 106;
            vaultListBox.BackColor = Color.FromArgb(24, 28, 44);
            vaultListBox.ForeColor = Color.White;
            vaultListBox.BorderStyle = BorderStyle.FixedSingle;
            vaultListBox.SelectedIndexChanged += VaultListBox_SelectedIndexChanged;

            selectedPreviewLabel.Text = "Select an entry to preview it.";
            selectedPreviewLabel.ForeColor = softTextColor;
            selectedPreviewLabel.BackColor = Color.FromArgb(24, 28, 44);
            selectedPreviewLabel.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            selectedPreviewLabel.Left = 315;
            selectedPreviewLabel.Top = 228;
            selectedPreviewLabel.Width = 310;
            selectedPreviewLabel.Height = 92;
            selectedPreviewLabel.Multiline = true;
            selectedPreviewLabel.ReadOnly = true;
            selectedPreviewLabel.ScrollBars = ScrollBars.Vertical;
            selectedPreviewLabel.WordWrap = true;
            selectedPreviewLabel.BorderStyle = BorderStyle.FixedSingle;

            revealButton.Text = "Reveal";
            revealButton.Left = 315;
            revealButton.Top = 336;
            revealButton.Width = 72;
            revealButton.Height = 30;
            revealButton.FlatStyle = FlatStyle.Flat;
            revealButton.ForeColor = Color.White;
            revealButton.BackColor = Color.FromArgb(35, 40, 60);
            revealButton.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);
            revealButton.Click += RevealButton_Click;

            copySecretButton.Text = "Copy Password";
            copySecretButton.Left = 395;
            copySecretButton.Top = 336;
            copySecretButton.Width = 100;
            copySecretButton.Height = 30;
            copySecretButton.FlatStyle = FlatStyle.Flat;
            copySecretButton.ForeColor = Color.White;
            copySecretButton.BackColor = Color.FromArgb(35, 40, 60);
            copySecretButton.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);
            copySecretButton.Click += CopySecretButton_Click;

            copyUsernameButton.Text = "Copy user";
            copyUsernameButton.Left = 493;
            copyUsernameButton.Top = 336;
            copyUsernameButton.Width = 80;
            copyUsernameButton.Height = 30;
            copyUsernameButton.FlatStyle = FlatStyle.Flat;
            copyUsernameButton.ForeColor = Color.White;
            copyUsernameButton.BackColor = Color.FromArgb(35, 40, 60);
            copyUsernameButton.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);
            copyUsernameButton.Click += CopyUsernameButton_Click;

            deleteEntryButton.Text = "Delete";
            deleteEntryButton.Left = 581;
            deleteEntryButton.Top = 336;
            deleteEntryButton.Width = 70;
            deleteEntryButton.Height = 30;
            deleteEntryButton.FlatStyle = FlatStyle.Flat;
            deleteEntryButton.ForeColor = Color.White;
            deleteEntryButton.BackColor = Color.FromArgb(80, 35, 45);
            deleteEntryButton.FlatAppearance.BorderColor = Color.FromArgb(150, 80, 90);
            deleteEntryButton.Click += DeleteEntryButton_Click;

            vaultPanel.Controls.Add(vaultTitleLabel);
            vaultPanel.Controls.Add(vaultSubtitleLabel);
            vaultPanel.Controls.Add(platformLabel);
            vaultPanel.Controls.Add(platformTextBox);
            vaultPanel.Controls.Add(usernameLabel);
            vaultPanel.Controls.Add(usernameTextBox);

            vaultPanel.Controls.Add(secretLabel);
            vaultPanel.Controls.Add(secretTextBox);
            vaultPanel.Controls.Add(createPasswordButton);
            vaultPanel.Controls.Add(passwordStrengthLabel);
            vaultPanel.Controls.Add(passwordStrengthTrack);

            vaultPanel.Controls.Add(websiteLabel);
            vaultPanel.Controls.Add(websiteTextBox);
            vaultPanel.Controls.Add(noteLabel);
            vaultPanel.Controls.Add(noteTextBox);

            vaultPanel.Controls.Add(saveEntryButton);
            vaultPanel.Controls.Add(clearButton);
            vaultPanel.Controls.Add(saveChangesButton);
            vaultPanel.Controls.Add(cancelEditButton);
            vaultPanel.Controls.Add(vaultSearchTextBox);
            vaultPanel.Controls.Add(vaultListBox);
            vaultPanel.Controls.Add(selectedPreviewLabel);
            vaultPanel.Controls.Add(revealButton);
            vaultPanel.Controls.Add(copySecretButton);
            vaultPanel.Controls.Add(copyUsernameButton);
            vaultPanel.Controls.Add(deleteEntryButton);
            vaultPanel.Controls.Add(openSiteButton);
            vaultPanel.Controls.Add(openAndFillButton);
            vaultPanel.Controls.Add(editEntryButton);

            Controls.Add(vaultPanel);
            vaultPanel.BringToFront();
            lockVaultButton.Text = "Lock vault";
            lockVaultButton.Left = 315;
            lockVaultButton.Top = 408;
            lockVaultButton.Width = 90;
            lockVaultButton.Height = 30;
            lockVaultButton.FlatStyle = FlatStyle.Flat;
            lockVaultButton.ForeColor = Color.White;
            lockVaultButton.BackColor = Color.FromArgb(35, 40, 60);
            lockVaultButton.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);
            lockVaultButton.Click += LockVaultButton_Click;

            changeVaultCodeButton.Text = "Change vault code";
            changeVaultCodeButton.Left = 415;
            changeVaultCodeButton.Top = 408;
            changeVaultCodeButton.Width = 130;
            changeVaultCodeButton.Height = 30;
            changeVaultCodeButton.FlatStyle = FlatStyle.Flat;
            changeVaultCodeButton.ForeColor = Color.White;
            changeVaultCodeButton.BackColor = Color.FromArgb(35, 40, 60);
            changeVaultCodeButton.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);
            changeVaultCodeButton.Click += ChangeVaultCodeButton_Click;

          
            favoriteButton.Text = "☆ Favorite";
            favoriteButton.Left = 555;
            favoriteButton.Top = 408;
            favoriteButton.Width = 100;
            favoriteButton.Height = 30;
            StyleActionButton(favoriteButton);
            favoriteButton.Click += FavoriteButton_Click;

            vaultPanel.Controls.Add(lockVaultButton);
            vaultPanel.Controls.Add(changeVaultCodeButton);
            vaultPanel.Controls.Add(favoriteButton);

            securitySettingsLabel.Text = "Recovery key settings";
            securitySettingsLabel.Left = 315;
            securitySettingsLabel.Top = 450;
            securitySettingsLabel.Width = 180;
            securitySettingsLabel.Height = 22;
            securitySettingsLabel.ForeColor = Color.White;
            securitySettingsLabel.BackColor = Color.Transparent;
            securitySettingsLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            recoveryReminderLabel.Text = "Remind me:";
            recoveryReminderLabel.Left = 315;
            recoveryReminderLabel.Top = 480;
            recoveryReminderLabel.Width = 75;
            recoveryReminderLabel.Height = 24;
            recoveryReminderLabel.ForeColor = softTextColor;
            recoveryReminderLabel.BackColor = Color.Transparent;
            recoveryReminderLabel.Font = new Font("Segoe UI", 8, FontStyle.Regular);

            recoveryReminderComboBox.Left = 390;
            recoveryReminderComboBox.Top = 477;
            recoveryReminderComboBox.Width = 130;
            recoveryReminderComboBox.Height = 28;
            recoveryReminderComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            recoveryReminderComboBox.Items.Add("Never");
            recoveryReminderComboBox.Items.Add("30 days");
            recoveryReminderComboBox.Items.Add("90 days");
            recoveryReminderComboBox.SelectedIndex = 0;
            recoveryReminderComboBox.SelectionChangeCommitted += RecoveryReminderComboBox_SelectionChangeCommitted;

            rotateRecoveryKeyButton.Text = "New recovery key";
            rotateRecoveryKeyButton.Left = 525;
            rotateRecoveryKeyButton.Top = 475;
            rotateRecoveryKeyButton.Width = 125;
            rotateRecoveryKeyButton.Height = 30;
            rotateRecoveryKeyButton.FlatStyle = FlatStyle.Flat;
            rotateRecoveryKeyButton.ForeColor = Color.White;
            rotateRecoveryKeyButton.BackColor = Color.FromArgb(35, 40, 60);
            rotateRecoveryKeyButton.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);
            rotateRecoveryKeyButton.Click += RotateRecoveryKeyButton_Click;

            securityCenterButton.Text = "Security check";
            securityCenterButton.Left = 315;
            securityCenterButton.Top = 515;
            securityCenterButton.Width = 140;
            securityCenterButton.Height = 30;
            securityCenterButton.FlatStyle = FlatStyle.Flat;
            securityCenterButton.ForeColor = Color.White;
            securityCenterButton.BackColor = Color.FromArgb(45, 90, 160);
            securityCenterButton.FlatAppearance.BorderColor = borderColor;
            securityCenterButton.Click += (s, e) => ShowSecurityCenterDialog();

            backupButton.Text = "Backup";
            backupButton.Left = 465;
            backupButton.Top = 515;
            backupButton.Width = 120;
            backupButton.Height = 30;
            backupButton.FlatStyle = FlatStyle.Flat;
            backupButton.ForeColor = Color.White;
            backupButton.BackColor = Color.FromArgb(35, 40, 60);
            backupButton.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);
            backupButton.Click += (s, e) => ShowBackupDialog();

            vaultPanel.Controls.Add(securitySettingsLabel);
            vaultPanel.Controls.Add(recoveryReminderLabel);
            vaultPanel.Controls.Add(recoveryReminderComboBox);
            vaultPanel.Controls.Add(rotateRecoveryKeyButton);
            vaultPanel.Controls.Add(securityCenterButton);
            vaultPanel.Controls.Add(backupButton);

            performanceSettingsLabel.Text = "Performance & safety";
            performanceSettingsLabel.Left = 20;
            performanceSettingsLabel.Top = 445;
            performanceSettingsLabel.Width = 200;
            performanceSettingsLabel.Height = 22;
            performanceSettingsLabel.ForeColor = Color.White;
            performanceSettingsLabel.BackColor = Color.Transparent;
            performanceSettingsLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            animationEnabledCheckBox.Text = "Background animation";
            animationEnabledCheckBox.Left = 20;
            animationEnabledCheckBox.Top = 475;
            animationEnabledCheckBox.Width = 220;
            animationEnabledCheckBox.Height = 24;
            animationEnabledCheckBox.Checked = true;
            animationEnabledCheckBox.ForeColor = softTextColor;
            animationEnabledCheckBox.BackColor = Color.Transparent;
            animationEnabledCheckBox.CheckedChanged += async (s, e) =>
            {
                currentVaultSettings.BackgroundAnimationEnabled = animationEnabledCheckBox.Checked;
                UpdateAnimationState();

                if (isVaultUnlocked)
                {
                    try
                    {
                        await SaveCurrentVaultToCloudAsync();
                    }
                    catch
                    {
                        // Ignore settings sync errors here.
                    }
                }
            };

            autoLockLabel.Text = "Lock after:";
            autoLockLabel.Left = 20;
            autoLockLabel.Top = 510;
            autoLockLabel.Width = 75;
            autoLockLabel.Height = 24;
            autoLockLabel.ForeColor = softTextColor;
            autoLockLabel.BackColor = Color.Transparent;
            autoLockLabel.Font = new Font("Segoe UI", 8, FontStyle.Regular);

            autoLockComboBox.Left = 95;
            autoLockComboBox.Top = 507;
            autoLockComboBox.Width = 130;
            autoLockComboBox.Height = 28;
            autoLockComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            autoLockComboBox.Items.Add("Never");
            autoLockComboBox.Items.Add("5 minutes");
            autoLockComboBox.Items.Add("10 minutes");
            autoLockComboBox.Items.Add("30 minutes");
            autoLockComboBox.SelectedIndex = 2;
            autoLockComboBox.SelectionChangeCommitted += async (s, e) =>
            {
                if (autoLockComboBox.SelectedIndex == 1)
                {
                    currentVaultSettings.AutoLockMinutes = 5;
                }
                else if (autoLockComboBox.SelectedIndex == 2)
                {
                    currentVaultSettings.AutoLockMinutes = 10;
                }
                else if (autoLockComboBox.SelectedIndex == 3)
                {
                    currentVaultSettings.AutoLockMinutes = 30;
                }
                else
                {
                    currentVaultSettings.AutoLockMinutes = 0;
                }

                MarkVaultActivity();

                if (isVaultUnlocked)
                {
                    try
                    {
                        await SaveCurrentVaultToCloudAsync();
                        selectedPreviewLabel.Text = "Performance setting saved.";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not save performance setting: " + ex.Message);
                    }
                }
            };

            vaultPanel.Controls.Add(performanceSettingsLabel);
            vaultPanel.Controls.Add(animationEnabledCheckBox);
            vaultPanel.Controls.Add(autoLockLabel);
            vaultPanel.Controls.Add(autoLockComboBox);

            openSiteButton.Text = "Open site";
            openSiteButton.Left = 315;
            openSiteButton.Top = 372;
            openSiteButton.Width = 90;
            openSiteButton.Height = 30;
            openSiteButton.FlatStyle = FlatStyle.Flat;
            openSiteButton.ForeColor = Color.White;
            openSiteButton.BackColor = Color.FromArgb(35, 40, 60);
            openSiteButton.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);
            openSiteButton.Click += OpenSiteButton_Click;

            openAndFillButton.Text = "Open + Fill";
            openAndFillButton.Left = 415;
            openAndFillButton.Top = 372;
            openAndFillButton.Width = 100;
            openAndFillButton.Height = 30;
            openAndFillButton.FlatStyle = FlatStyle.Flat;
            openAndFillButton.ForeColor = Color.White;
            openAndFillButton.BackColor = Color.FromArgb(45, 90, 160);
            openAndFillButton.FlatAppearance.BorderColor = borderColor;
            openAndFillButton.Click += async (s, e) => await OpenAndFillButton_Click();

            editEntryButton.Text = "Edit";
            editEntryButton.Left = 525;
            editEntryButton.Top = openSiteButton.Top;
            editEntryButton.Width = 70;
            editEntryButton.Height = 30;
            editEntryButton.FlatStyle = FlatStyle.Flat;
            editEntryButton.UseVisualStyleBackColor = false;
            editEntryButton.ForeColor = Color.White;
            editEntryButton.BackColor = Color.FromArgb(35, 40, 60);
            editEntryButton.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);
            editEntryButton.Click += EditEntryButton_Click;
        }

        private void Card_Paint(object? sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(borderColor, 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, loginCard.Width - 1, loginCard.Height - 1);
            }
        }

        private void LoginCard_MouseEnter(object? sender, EventArgs e)
        {
            loginCard.BackColor = cardHoverColor;
            loginCard.Invalidate();
        }

        private void LoginCard_MouseLeave(object? sender, EventArgs e)
        {
            loginCard.BackColor = cardColor;
            loginCard.Invalidate();
        }

        private async void GoogleLoginCard_Click(object? sender, EventArgs e)
        {
            await LoginWithGoogleAsync();
        }

        private async Task LoginWithGoogleAsync()
        {
            try
            {
                loginCard.Enabled = false;
                accountStatusLabel.Text = "Opening Google...";
                accountStatusLabel.ForeColor = Color.FromArgb(200, 210, 255);

                currentDriveService = await GoogleAuthService.LoginAsync();
                string email = await GoogleAuthService.GetUserEmailAsync(currentDriveService);

                accountStatusLabel.Text = "Connected: " + email;
                accountStatusLabel.ForeColor = successColor;
                logoutButton.Enabled = true;

                cloudVaultExists = await GoogleDriveVaultService.VaultExistsAsync(currentDriveService);

                if (cloudVaultExists)
                {
                    ConfigureVaultAccessForUnlock();
                }
                else
                {
                    ConfigureVaultAccessForCreate();
                }

                ShowVaultAccessUi();
            }
            catch (Exception ex)
            {
                loginCard.Enabled = true;
                accountStatusLabel.Text = "Connection failed";
                accountStatusLabel.ForeColor = dangerColor;

                if (ex is FileNotFoundException && ex.Message.Contains("credentials.json"))
                {
                    bool installed = ShowGoogleCredentialsSetupDialog();

                    if (installed)
                    {
                        await LoginWithGoogleAsync();
                        return;
                    }

                    accountStatusLabel.Text = "Google setup missing";
                    MessageBox.Show(
                        "Google setup was cancelled. Google Drive sync needs a credentials.json file before login can continue.",
                        "Google setup missing",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                MessageBox.Show("Google login error: " + ex.Message);
            }
        }
        private bool ShowGoogleCredentialsSetupDialog()
        {
            DialogResult setupChoice = MessageBox.Show(
                GoogleAuthService.GetCredentialsSetupMessage() + Environment.NewLine + Environment.NewLine +
                "Choose credentials.json now?",
                "Google setup required",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (setupChoice != DialogResult.Yes)
            {
                return false;
            }

            using (OpenFileDialog openDialog = new OpenFileDialog())
            {
                openDialog.Title = "Choose Google credentials.json";
                openDialog.Filter = "Google credentials.json (*.json)|*.json|All files (*.*)|*.*";
                openDialog.CheckFileExists = true;
                openDialog.Multiselect = false;

                if (openDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return false;
                }

                try
                {
                    GoogleAuthService.InstallCredentialsFile(openDialog.FileName);

                    MessageBox.Show(
                        "Google setup saved successfully." + Environment.NewLine + Environment.NewLine +
                        "QuickForge will now try Google login again.",
                        "Google setup saved",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Could not save Google setup: " + ex.Message,
                        "Google setup failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );

                    return false;
                }
            }
        }
        private void ConfigureVaultAccessForCreate()
        {
            vaultAccessTitleLabel.Text = "Create Vault Code";
            vaultAccessSubtitleLabel.Text = "First time setup. This code will unlock your encrypted vault.";
            vaultCodeLabel.Text = "Create vault code";
            confirmVaultCodeLabel.Visible = true;
            confirmVaultCodeTextBox.Visible = true;
            createVaultButton.Text = "Create Vault";

            vaultCodeTextBox.Clear();
            confirmVaultCodeTextBox.Clear();
        }

        private void ConfigureVaultAccessForUnlock()
        {
            vaultAccessTitleLabel.Text = "Unlock Vault";
            vaultAccessSubtitleLabel.Text = "A vault already exists. Enter your vault code or recovery key.";
            vaultCodeLabel.Text = "Vault code / recovery key";
            confirmVaultCodeLabel.Visible = false;
            confirmVaultCodeTextBox.Visible = false;
            createVaultButton.Text = "Unlock Vault";

            vaultCodeTextBox.Clear();
            confirmVaultCodeTextBox.Clear();
        }
        private async void CreateVaultButton_Click(object? sender, EventArgs e)
        {
            if (currentDriveService == null)
            {
                MessageBox.Show("Google Drive is not connected.");
                return;
            }

            string code = vaultCodeTextBox.Text.Trim();
            string confirmCode = confirmVaultCodeTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Enter your vault code first.");
                return;
            }

            if (!cloudVaultExists)
            {
                if (code != confirmCode)
                {
                    MessageBox.Show("Vault codes do not match.");
                    return;
                }

                string recoveryKey = VaultCryptoService.GenerateRecoveryKey();

                bool recoveryKeyConfirmed = ShowFirstRecoveryKeyDialog(recoveryKey);

                if (!recoveryKeyConfirmed)
                {
                    MessageBox.Show("Vault setup cancelled. Save your recovery key before creating the vault.");
                    return;
                }

                vaultCode = code;
                vaultEntries.Clear();

                currentVaultSettings = new VaultSettings
                {
                    RecoveryKeyReminderDays = 0,
                    LastRecoveryKeyRotatedAt = DateTime.UtcNow
                };

                VaultData vaultData = new VaultData
                {
                    Entries = new List<VaultEntry>(vaultEntries),
                    Settings = currentVaultSettings,
                    UpdatedAt = DateTime.UtcNow
                };

                try
                {
                    string encryptedJson = VaultCryptoService.CreateEncryptedVault(
                        vaultData,
                        vaultCode,
                        recoveryKey,
                        out byte[] dataKey,
                        out EncryptedVaultFile encryptedVaultFile
                    );

                    currentDataKey = dataKey;
                    currentEncryptedVaultFile = encryptedVaultFile;

                    await GoogleDriveVaultService.UploadEncryptedVaultAsync(
                        currentDriveService,
                        encryptedJson
                    );

                    cloudVaultExists = true;

                    vaultCodeTextBox.Clear();
                    confirmVaultCodeTextBox.Clear();

                    

                    GrantSecretAccessWindow();

                    ShowVaultUi();
                    selectedPreviewLabel.Text = "Vault created. Recovery key saved.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not create encrypted vault: " + ex.Message);
                }

                return;
            }

            try
            {
                vaultCode = code;
                await LoadVaultFromCloudAsync();

                GrantSecretAccessWindow();

                vaultCodeTextBox.Clear();
                confirmVaultCodeTextBox.Clear();

                ShowVaultUi();
            }
            catch
            {
                vaultCode = "";
                currentDataKey = null;
                currentEncryptedVaultFile = null;
                vaultCodeTextBox.Clear();
                confirmVaultCodeTextBox.Clear();

                MessageBox.Show("Wrong vault code/recovery key or corrupted vault file.");
            }
        }

        private async Task SaveCurrentVaultToCloudAsync()
        {
            if (currentDriveService == null)
            {
                throw new InvalidOperationException("Google Drive is not connected.");
            }

            if (currentDataKey == null || currentEncryptedVaultFile == null)
            {
                throw new InvalidOperationException("Vault is locked.");
            }

            string encryptedJson = CreateCurrentEncryptedVaultJson();

            await GoogleDriveVaultService.UploadEncryptedVaultAsync(
                currentDriveService,
                encryptedJson
            );

            cloudVaultExists = true;
        }

        private async Task LoadVaultFromCloudAsync()
        {
            if (currentDriveService == null)
            {
                throw new InvalidOperationException("Google Drive is not connected.");
            }

            string? encryptedJson =
                await GoogleDriveVaultService.DownloadEncryptedVaultAsync(currentDriveService);

            if (string.IsNullOrWhiteSpace(encryptedJson))
            {
                throw new InvalidOperationException("No encrypted vault was found.");
            }

            VaultData vaultData = VaultCryptoService.DecryptVault(
                encryptedJson,
                vaultCode,
                out byte[] dataKey,
                out EncryptedVaultFile encryptedVaultFile

            );
            currentVaultSettings = vaultData.Settings ?? new VaultSettings();
            ApplyRecoverySettingsToUi();
            currentDataKey = dataKey;
            currentEncryptedVaultFile = encryptedVaultFile;

            vaultEntries.Clear();

            foreach (VaultEntry entry in vaultData.Entries)
            {
                vaultEntries.Add(entry);
            }

            RefreshVaultList();
        }
        private void LockVaultButton_Click(object? sender, EventArgs e)
        {
            LockVaultForSafety("Vault locked.");
        }
        private async void ChangeVaultCodeButton_Click(object? sender, EventArgs e)
        {
            if (currentDriveService == null)
            {
                MessageBox.Show("Google Drive is not connected.");
                return;
            }

            if (currentDataKey == null || currentEncryptedVaultFile == null)
            {
                MessageBox.Show("Unlock the vault first.");
                return;
            }

            string? oldCode = ShowPasswordPrompt("Change Vault Code","Enter your current vault code or recovery key:");

            if (
                oldCode == null ||
                currentEncryptedVaultFile == null ||
                !VaultCryptoService.CanUnlockVault(currentEncryptedVaultFile, oldCode)
            )
            {
                MessageBox.Show("Wrong vault code or recovery key.");
                return;
            }

            string? newCode = ShowPasswordPrompt("Change Vault Code", "Enter new vault code:");

            if (string.IsNullOrWhiteSpace(newCode))
            {
                MessageBox.Show("New vault code cannot be empty.");
                return;
            }

            string? confirmNewCode = ShowPasswordPrompt("Change Vault Code", "Confirm new vault code:");

            if (newCode != confirmNewCode)
            {
                MessageBox.Show("New vault codes do not match.");
                return;
            }

            try
            {
                VaultCryptoService.ChangeVaultCode(
                    currentEncryptedVaultFile,
                    currentDataKey,
                    newCode
                );

                vaultCode = newCode;

                await SaveCurrentVaultToCloudAsync();

                selectedPreviewLabel.Text = "Vault code changed and synced.";
                MessageBox.Show("Vault code changed successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not change vault code: " + ex.Message);
            }
        }
        private void LogoutButton_Click(object? sender, EventArgs e)
        {
            try
            {
                GoogleAuthService.Logout();

                vaultCode = "";
                vaultEntries.Clear();
                RefreshVaultList();
                currentDriveService = null;
                cloudVaultExists = false;
                accountStatusLabel.Text = "Not connected";
                accountStatusLabel.ForeColor = softTextColor;
                logoutButton.Enabled = false;
                isVaultUnlocked = false;
                ClearSecretAccessWindow();
                currentDataKey = null;
                currentEncryptedVaultFile = null;
                currentVaultSettings = new VaultSettings();
                hasShownRecoveryReminderThisSession = false;

                ShowLoggedOutUi();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Logout error: " + ex.Message);
            }
        }

        private void ShowLoggedOutUi()
        {
            loginCard.Visible = true;
            loginCard.Enabled = true;
            vaultAccessPanel.Visible = false;
            vaultPanel.Visible = false;
            logoutButton.Enabled = false;

            accountStatusLabel.Text = "Not connected";
            accountStatusLabel.ForeColor = softTextColor;
        }

        private void ShowVaultAccessUi()
        {
            loginCard.Visible = false;
            vaultAccessPanel.Visible = true;
            vaultPanel.Visible = false;

            vaultAccessPanel.BringToFront();
            topBarPanel.BringToFront();
        }

        private void ShowVaultUi()
        {
            isVaultUnlocked = true;

            ApplyRecoverySettingsToUi();
            ApplyPerformanceSettingsToUi();
            MarkVaultActivity();
            CheckRecoveryKeyReminder();

            loginCard.Visible = false;
            vaultAccessPanel.Visible = false;
            vaultPanel.Visible = true;

            vaultPanel.BringToFront();
            topBarPanel.BringToFront();
        }
        private async void SaveEntryButton_Click(object? sender, EventArgs e)
        {
            if (editingEntry != null)
            {
                MessageBox.Show("Finish editing or cancel edit first.");
                return;
            }
            string platform = platformTextBox.Text.Trim();
            string username = usernameTextBox.Text.Trim();
            string secret = secretTextBox.Text.Trim();
            string website = websiteTextBox.Text.Trim();
            string note = noteTextBox.Text.Trim();

            if (
                string.IsNullOrWhiteSpace(platform) &&
                string.IsNullOrWhiteSpace(username) &&
                string.IsNullOrWhiteSpace(secret) &&
                string.IsNullOrWhiteSpace(website) &&
                string.IsNullOrWhiteSpace(note)
            )
            {
                MessageBox.Show("Fill in at least one field before saving.");
                return;
            }
            if (!HandleDuplicatePasswordBeforeSave(secret, null))
            {
                return;
            }
            VaultEntry entry = new VaultEntry
            {
                Platform = platform,
                Username = username,
                Secret = secret,
                Website = website,
                Note = note,
                CreatedAt = DateTime.Now
            };

            vaultEntries.Add(entry);
            RefreshVaultList();
            ClearEntryInputs();

            selectedPreviewLabel.Text = "Saved: " + entry.GetDisplayName();
            try
            {
                await SaveCurrentVaultToCloudAsync();
                selectedPreviewLabel.Text = "Saved and synced: " + entry.GetDisplayName();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Entry was saved locally, but sync failed: " + ex.Message);
            }
        }
        private void EditEntryButton_Click(object? sender, EventArgs e)
        {
            VaultEntry? entry = GetSelectedEntry();

            if (entry == null)
            {
                MessageBox.Show("Select an entry first.");
                return;
            }

            editingEntry = entry;
            editingEntryIndex = vaultEntries.IndexOf(entry);

            platformTextBox.Text = entry.Platform;
            usernameTextBox.Text = entry.Username;
            secretTextBox.Text = entry.Secret;
            websiteTextBox.Text = entry.Website;
            noteTextBox.Text = entry.Note;

            SetEntryEditMode(true);

            SetPreviewText(
                "Editing: " + entry.GetDisplayName(),
                "Make your changes on the left.",
                "Click Save changes when done."
            );
        }

        private async void SaveChangesButton_Click(object? sender, EventArgs e)
        {
            if (editingEntry == null || editingEntryIndex < 0 || editingEntryIndex >= vaultEntries.Count)
            {
                MessageBox.Show("No entry is being edited.");
                SetEntryEditMode(false);
                return;
            }

            string platform = platformTextBox.Text.Trim();
            string username = usernameTextBox.Text.Trim();
            string secret = secretTextBox.Text.Trim();
            string website = websiteTextBox.Text.Trim();
            string note = noteTextBox.Text.Trim();

            if (
                string.IsNullOrWhiteSpace(platform) &&
                string.IsNullOrWhiteSpace(username) &&
                string.IsNullOrWhiteSpace(secret) &&
                string.IsNullOrWhiteSpace(website) &&
                string.IsNullOrWhiteSpace(note)
            )
            {
                MessageBox.Show("Fill in at least one field before saving changes.");
                return;
            }
            if (!HandleDuplicatePasswordBeforeSave(secret, editingEntry))
            {
                return;
            }
            editingEntry.Platform = platform;
            editingEntry.Username = username;
            editingEntry.Secret = secret;
            editingEntry.Website = website;
            editingEntry.Note = note;

            try
            {
                await SaveCurrentVaultToCloudAsync();

                RefreshVaultList();

                int visibleIndex = visibleVaultEntries.IndexOf(editingEntry);

                if (visibleIndex >= 0 && visibleIndex < vaultListBox.Items.Count)
                {
                    vaultListBox.SelectedIndex = visibleIndex;
                }

                SetPreviewText(
                    "Saved changes: " + editingEntry.GetDisplayName(),
                    "User: " + MaskEmpty(editingEntry.Username),
                    "Password/code: " + MaskSecret(editingEntry.Secret)
                );

                ClearEntryInputs();
                SetEntryEditMode(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save changes: " + ex.Message);
            }
        }

        private void CancelEditButton_Click(object? sender, EventArgs e)
        {
            ClearEntryInputs();
            SetEntryEditMode(false);

            selectedPreviewLabel.Text = "Edit cancelled.";
        }

        private void SetEntryEditMode(bool isEditing)
        {
            if (!isEditing)
            {
                editingEntry = null;
                editingEntryIndex = -1;
            }

            saveEntryButton.Visible = !isEditing;
            clearButton.Visible = !isEditing;

            saveChangesButton.Visible = isEditing;
            cancelEditButton.Visible = isEditing;

            editEntryButton.Enabled = !isEditing;
            deleteEntryButton.Enabled = !isEditing;
            favoriteButton.Enabled = !isEditing;
            openSiteButton.Enabled = !isEditing;
            openAndFillButton.Enabled = !isEditing;
        }
        private void ClearButton_Click(object? sender, EventArgs e)
        {
            ClearEntryInputs();
        }

        private void ClearEntryInputs()
        {
            platformTextBox.Clear();
            usernameTextBox.Clear();
            secretTextBox.Clear();
            websiteTextBox.Clear();
            noteTextBox.Clear();
        }

        private void SetPreviewText(params string[] lines)
        {
            selectedPreviewLabel.Text = string.Join(Environment.NewLine, lines);
        }
        private void VaultListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            VaultEntry? entry = GetSelectedEntry();

            if (entry == null)
            {
                selectedPreviewLabel.Text = "Select an entry to preview it.";
                UpdateFavoriteButtonText();
                return;
            }

            SetPreviewText(
                "Selected: " + entry.GetDisplayName(),
                "User: " + MaskEmpty(entry.Username),
                "Password/code: " + MaskSecret(entry.Secret),
                "Favorite: " + (entry.IsFavorite ? "Yes" : "No")
            );

            UpdateFavoriteButtonText();
        }
        private void OpenSiteButton_Click(object? sender, EventArgs e)
        {
            VaultEntry? entry = GetSelectedEntry();

            if (entry == null)
            {
                MessageBox.Show("Select an entry first.");
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.Website))
            {
                MessageBox.Show("This entry has no website link.");
                return;
            }

            OpenWebsite(entry.Website);
        }

        private async Task OpenAndFillButton_Click()
        {
            VaultEntry? entry = GetSelectedEntry();

            if (entry == null)
            {
                MessageBox.Show("Select an entry first.");
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.Website))
            {
                MessageBox.Show("This entry has no website link.");
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.Username) || string.IsNullOrWhiteSpace(entry.Secret))
            {
                MessageBox.Show("This entry needs both username and password for Open + Fill.");
                return;
            }

            if (!EnsureSecretAccessForSecretAction())
            {
                return;
            }

            OpenWebsite(entry.Website);

            await Task.Delay(1800);

            Clipboard.SetText(entry.Username);
            SendKeys.SendWait("^v");

            await Task.Delay(200);

            SendKeys.SendWait("{TAB}");

            await Task.Delay(200);

            Clipboard.SetText(entry.Secret);
            SendKeys.SendWait("^v");

            _ = ClearClipboardLaterAsync(entry.Secret, 20000);
        }

        private void OpenWebsite(string website)
        {
            try
            {
                string cleanWebsite = website.Trim();

                if (!cleanWebsite.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !cleanWebsite.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    cleanWebsite = "https://" + cleanWebsite;
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = cleanWebsite,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open site: " + ex.Message);
            }
        }
        private void RevealButton_Click(object? sender, EventArgs e)
        {
            VaultEntry? entry = GetSelectedEntry();

            if (entry == null)
            {
                MessageBox.Show("Select an entry first.");
                return;
            }

            if (!isVaultUnlocked)
            {
                MessageBox.Show("Unlock the vault first.");
                return;
            }

            if (!EnsureSecretAccessForSecretAction())
            {
                return;
            }

            SetPreviewText(
            "Platform: " + MaskEmpty(entry.Platform),
            "User: " + MaskEmpty(entry.Username),
            "Password/code: " + MaskEmpty(entry.Secret),
            "Website: " + MaskEmpty(entry.Website),
            "Note: " + MaskEmpty(entry.Note));

            hideRevealTimer.Stop();
            hideRevealTimer.Start();
        }

        private void CopySecretButton_Click(object? sender, EventArgs e)
        {
            VaultEntry? entry = GetSelectedEntry();

            if (entry == null)
            {
                MessageBox.Show("Select an entry first.");
                return;
            }

            if (!isVaultUnlocked)
            {
                MessageBox.Show("Unlock the vault first.");
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.Secret))
            {
                MessageBox.Show("This entry has no password/code to copy.");
                return;
            }

            if (!EnsureSecretAccessForSecretAction())
            {
                return;
            }

            Clipboard.SetText(entry.Secret);
            selectedPreviewLabel.Text = "Copied password/code for: " + entry.GetDisplayName() +
                ". Clipboard clears in 20 seconds.";

            _ = ClearClipboardLaterAsync(entry.Secret, 20000);
        }
        private void CopyUsernameButton_Click(object? sender, EventArgs e)
        {
            VaultEntry? entry = GetSelectedEntry();

            if (entry == null)
            {
                MessageBox.Show("Select an entry first.");
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.Username))
            {
                MessageBox.Show("This entry has no username/email to copy.");
                return;
            }

            Clipboard.SetText(entry.Username);
            selectedPreviewLabel.Text = "Copied username for: " + entry.GetDisplayName();
        }
        private bool ShowDeleteEntryConfirmationDialog(VaultEntry entry)
        {
            bool confirmed = false;

            using (Form dialog = new Form())
            {
                dialog.Width = 430;
                dialog.Height = 230;
                dialog.Text = "Delete entry";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.BackColor = Color.FromArgb(16, 20, 34);

                Label titleLabel = new Label();
                titleLabel.Text = "Delete this entry?";
                titleLabel.Left = 20;
                titleLabel.Top = 18;
                titleLabel.Width = 360;
                titleLabel.Height = 28;
                titleLabel.ForeColor = Color.White;
                titleLabel.BackColor = Color.Transparent;
                titleLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);

                Label entryLabel = new Label();
                entryLabel.Text = entry.GetDisplayName();
                entryLabel.Left = 20;
                entryLabel.Top = 55;
                entryLabel.Width = 360;
                entryLabel.Height = 24;
                entryLabel.ForeColor = Color.White;
                entryLabel.BackColor = Color.Transparent;
                entryLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);

                Label warningLabel = new Label();
                warningLabel.Text = "This action cannot be undone after sync.";
                warningLabel.Left = 20;
                warningLabel.Top = 90;
                warningLabel.Width = 360;
                warningLabel.Height = 30;
                warningLabel.ForeColor = dangerColor;
                warningLabel.BackColor = Color.Transparent;

                Button deleteButton = new Button();
                deleteButton.Text = "Delete";
                deleteButton.Left = 190;
                deleteButton.Top = 145;
                deleteButton.Width = 95;
                deleteButton.Height = 34;
                deleteButton.FlatStyle = FlatStyle.Flat;
                deleteButton.UseVisualStyleBackColor = false;
                deleteButton.ForeColor = Color.White;
                deleteButton.BackColor = Color.FromArgb(120, 35, 45);
                deleteButton.FlatAppearance.BorderColor = Color.FromArgb(190, 80, 90);
                deleteButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(150, 45, 55);
                deleteButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(90, 25, 35);

                Button cancelButton = new Button();
                cancelButton.Text = "Cancel";
                cancelButton.Left = 295;
                cancelButton.Top = 145;
                cancelButton.Width = 95;
                cancelButton.Height = 34;
                StyleActionButton(cancelButton);

                deleteButton.Click += (s, e) =>
                {
                    confirmed = true;
                    dialog.Close();
                };

                cancelButton.Click += (s, e) =>
                {
                    confirmed = false;
                    dialog.Close();
                };

                dialog.Controls.Add(titleLabel);
                dialog.Controls.Add(entryLabel);
                dialog.Controls.Add(warningLabel);
                dialog.Controls.Add(deleteButton);
                dialog.Controls.Add(cancelButton);

                dialog.CancelButton = cancelButton;

                dialog.ShowDialog(this);
            }

            return confirmed;
        }
        private async void FavoriteButton_Click(object? sender, EventArgs e)
        {
            VaultEntry? entry = GetSelectedEntry();

            if (entry == null)
            {
                MessageBox.Show("Select an entry first.");
                return;
            }

            entry.IsFavorite = !entry.IsFavorite;

            int selectedIndex = vaultListBox.SelectedIndex;

            RefreshVaultList();

            if (selectedIndex >= 0 && selectedIndex < vaultListBox.Items.Count)
            {
                vaultListBox.SelectedIndex = selectedIndex;
            }

            UpdateFavoriteButtonText();

            selectedPreviewLabel.Text = entry.IsFavorite
                ? "Added to favorites: " + entry.GetDisplayName()
                : "Removed from favorites: " + entry.GetDisplayName();

            try
            {
                await SaveCurrentVaultToCloudAsync();

                selectedPreviewLabel.Text = entry.IsFavorite
                    ? "Added to favorites and synced: " + entry.GetDisplayName()
                    : "Removed from favorites and synced: " + entry.GetDisplayName();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Favorite changed locally, but sync failed: " + ex.Message);
            }
        }

        private void UpdateFavoriteButtonText()
        {
            VaultEntry? entry = GetSelectedEntry();

            if (entry == null)
            {
                favoriteButton.Text = "☆ Favorite";
                favoriteButton.BackColor = Color.FromArgb(35, 40, 60);
                return;
            }

            if (entry.IsFavorite)
            {
                favoriteButton.Text = "★ Favorited";
                favoriteButton.BackColor = Color.FromArgb(120, 85, 35);
            }
            else
            {
                favoriteButton.Text = "☆ Favorite";
                favoriteButton.BackColor = Color.FromArgb(35, 40, 60);
            }
        }
        private async void DeleteEntryButton_Click(object? sender, EventArgs e)
        {
            VaultEntry? entry = GetSelectedEntry();

            if (entry == null)
            {
                MessageBox.Show("Select an entry first.");
                return;
            }

            if (!ShowDeleteEntryConfirmationDialog(entry))
            {
                selectedPreviewLabel.Text = "Delete cancelled.";
                return;
            }

            vaultEntries.Remove(entry);

            if (editingEntry == entry)
            {
                ClearEntryInputs();
                SetEntryEditMode(false);
            }

            RefreshVaultList();
            selectedPreviewLabel.Text = "Deleted entry.";

            try
            {
                await SaveCurrentVaultToCloudAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Entry was deleted locally, but sync failed: " + ex.Message);
            }
        }
        private bool AskForVaultCode()
        {
            string? input = ShowPasswordPrompt("Unlock Vault", "Enter your vault code:");

            if (input == null)
            {
                return false;
            }

            return input == vaultCode;
        }

        private string? ShowPasswordPrompt(string title, string message)
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 360;
                prompt.Height = 170;
                prompt.Text = title;
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;

                Label textLabel = new Label();
                textLabel.Text = message;
                textLabel.Left = 20;
                textLabel.Top = 20;
                textLabel.Width = 300;

                TextBox inputBox = new TextBox();
                inputBox.Left = 20;
                inputBox.Top = 50;
                inputBox.Width = 300;
                inputBox.UseSystemPasswordChar = true;

                Button confirmation = new Button();
                confirmation.Text = "Unlock";
                confirmation.Left = 220;
                confirmation.Top = 85;
                confirmation.Width = 100;
                confirmation.DialogResult = DialogResult.OK;

                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(inputBox);
                prompt.Controls.Add(confirmation);
                prompt.AcceptButton = confirmation;

                if (prompt.ShowDialog(this) == DialogResult.OK)
                {
                    return inputBox.Text;
                }

                return null;
            }
        }
        private void ApplyRecoverySettingsToUi()
        {
            if (recoveryReminderComboBox.Items.Count == 0)
            {
                return;
            }

            if (currentVaultSettings.RecoveryKeyReminderDays == 30)
            {
                recoveryReminderComboBox.SelectedIndex = 1;
            }
            else if (currentVaultSettings.RecoveryKeyReminderDays == 90)
            {
                recoveryReminderComboBox.SelectedIndex = 2;
            }
            else
            {
                recoveryReminderComboBox.SelectedIndex = 0;
            }
        }

        private async void RecoveryReminderComboBox_SelectionChangeCommitted(object? sender, EventArgs e)
        {
            if (!isVaultUnlocked)
            {
                MessageBox.Show("Unlock the vault first.");
                return;
            }

            if (recoveryReminderComboBox.SelectedIndex == 1)
            {
                currentVaultSettings.RecoveryKeyReminderDays = 30;
            }
            else if (recoveryReminderComboBox.SelectedIndex == 2)
            {
                currentVaultSettings.RecoveryKeyReminderDays = 90;
            }
            else
            {
                currentVaultSettings.RecoveryKeyReminderDays = 0;
            }

            try
            {
                await SaveCurrentVaultToCloudAsync();
                selectedPreviewLabel.Text = "Recovery reminder setting saved.";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save reminder setting: " + ex.Message);
            }
        }

        private async void RotateRecoveryKeyButton_Click(object? sender, EventArgs e)
        {
            if (!isVaultUnlocked)
            {
                MessageBox.Show("Unlock the vault first.");
                return;
            }

            if (currentDataKey == null || currentEncryptedVaultFile == null)
            {
                MessageBox.Show("Vault is not ready.");
                return;
            }

            string newRecoveryKey = VaultCryptoService.GenerateRecoveryKey();

            bool confirmed = ShowRecoveryKeyRotationDialog(newRecoveryKey);

            if (!confirmed)
            {
                return;
            }

            try
            {
                VaultCryptoService.RotateRecoveryKey(
                    currentEncryptedVaultFile,
                    currentDataKey,
                    newRecoveryKey
                );

                currentVaultSettings.LastRecoveryKeyRotatedAt = DateTime.UtcNow;

                await SaveCurrentVaultToCloudAsync();

                selectedPreviewLabel.Text = "Recovery key rotated and synced.";
                MessageBox.Show("Recovery key rotated successfully. The old recovery key no longer works.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not rotate recovery key: " + ex.Message);
            }
        }
        private bool ShowFirstRecoveryKeyDialog(string recoveryKey)
        {
            bool copied = false;
            bool confirmed = false;

            using (Form dialog = new Form())
            {
                dialog.Width = 520;
                dialog.Height = 330;
                dialog.Text = "Save Recovery Key";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.BackColor = Color.FromArgb(16, 20, 34);

                Label titleLabel = new Label();
                titleLabel.Text = "Your recovery key";
                titleLabel.Left = 20;
                titleLabel.Top = 18;
                titleLabel.Width = 460;
                titleLabel.Height = 24;
                titleLabel.ForeColor = Color.White;
                titleLabel.BackColor = Color.Transparent;
                titleLabel.Font = new Font("Segoe UI", 11, FontStyle.Bold);

                Label infoLabel = new Label();
                infoLabel.Text = "Save this key somewhere safe. It can unlock your vault if you forget your vault code.";
                infoLabel.Left = 20;
                infoLabel.Top = 45;
                infoLabel.Width = 460;
                infoLabel.Height = 35;
                infoLabel.ForeColor = softTextColor;
                infoLabel.BackColor = Color.Transparent;

                TextBox keyTextBox = new TextBox();
                keyTextBox.Left = 20;
                keyTextBox.Top = 85;
                keyTextBox.Width = 460;
                keyTextBox.Height = 26;
                keyTextBox.ReadOnly = true;
                keyTextBox.Text = recoveryKey;
                keyTextBox.BackColor = Color.FromArgb(24, 28, 44);
                keyTextBox.ForeColor = Color.White;
                keyTextBox.BorderStyle = BorderStyle.FixedSingle;

                Label warningLabel = new Label();
                warningLabel.Left = 20;
                warningLabel.Top = 120;
                warningLabel.Width = 460;
                warningLabel.Height = 55;
                warningLabel.Text =
                    "For safety, QuickForge will not download this as a plain text file.\n" +
                    "Copy it and store it somewhere safe outside this app.";
                warningLabel.ForeColor = Color.FromArgb(255, 190, 90);
                warningLabel.BackColor = Color.Transparent;

                Button copyButton = new Button();
                copyButton.Text = "Copy recovery key";
                copyButton.Left = 20;
                copyButton.Top = 185;
                copyButton.Width = 160;
                copyButton.Height = 32;
                StyleActionButton(copyButton, true);

                CheckBox savedCheckBox = new CheckBox();
                savedCheckBox.Text = "I have saved this recovery key safely";
                savedCheckBox.Left = 20;
                savedCheckBox.Top = 230;
                savedCheckBox.Width = 330;
                savedCheckBox.Height = 28;
                savedCheckBox.ForeColor = softTextColor;
                savedCheckBox.BackColor = Color.Transparent;

                Button cancelButton = new Button();
                cancelButton.Text = "Cancel setup";
                cancelButton.Left = 230;
                cancelButton.Top = 270;
                cancelButton.Width = 110;
                cancelButton.Height = 32;
                cancelButton.DialogResult = DialogResult.Cancel;
                StyleActionButton(cancelButton);

                Button continueButton = new Button();
                continueButton.Text = "Continue";
                continueButton.Left = 355;
                continueButton.Top = 270;
                continueButton.Width = 125;
                continueButton.Height = 32;
                continueButton.Enabled = false;
                StyleActionButton(continueButton, true);

                void UpdateContinueState()
                {
                    continueButton.Enabled = copied && savedCheckBox.Checked;
                }

                copyButton.Click += (s, e) =>
                {
                    Clipboard.SetText(recoveryKey);
                    copied = true;
                    copyButton.Text = "Copied";
                    _ = ClearClipboardLaterAsync(recoveryKey, 60000);
                    UpdateContinueState();
                };

                savedCheckBox.CheckedChanged += (s, e) =>
                {
                    UpdateContinueState();
                };

                continueButton.Click += (s, e) =>
                {
                    confirmed = true;
                    dialog.DialogResult = DialogResult.OK;
                    keyTextBox.Clear();
                    dialog.Close();
                };

                dialog.Controls.Add(titleLabel);
                dialog.Controls.Add(infoLabel);
                dialog.Controls.Add(keyTextBox);
                dialog.Controls.Add(warningLabel);
                dialog.Controls.Add(copyButton);
                dialog.Controls.Add(savedCheckBox);
                dialog.Controls.Add(cancelButton);
                dialog.Controls.Add(continueButton);

                dialog.AcceptButton = continueButton;
                dialog.CancelButton = cancelButton;

                dialog.ShowDialog(this);
            }

            return confirmed;
        }
        private bool ShowRecoveryKeyRotationDialog(string newRecoveryKey)
        {
            bool copied = false;
            bool confirmed = false;

            using (Form dialog = new Form())
            {
                dialog.Width = 520;
                dialog.Height = 320;
                dialog.Text = "Rotate Recovery Key";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.BackColor = Color.FromArgb(16, 20, 34);

                Label titleLabel = new Label();
                titleLabel.Text = "New recovery key";
                titleLabel.Left = 20;
                titleLabel.Top = 18;
                titleLabel.Width = 460;
                titleLabel.Height = 24;
                titleLabel.ForeColor = Color.White;
                titleLabel.BackColor = Color.Transparent;
                titleLabel.Font = new Font("Segoe UI", 11, FontStyle.Bold);

                TextBox keyTextBox = new TextBox();
                keyTextBox.Left = 20;
                keyTextBox.Top = 55;
                keyTextBox.Width = 460;
                keyTextBox.Height = 26;
                keyTextBox.ReadOnly = true;
                keyTextBox.Text = newRecoveryKey;
                keyTextBox.BackColor = Color.FromArgb(24, 28, 44);
                keyTextBox.ForeColor = Color.White;
                keyTextBox.BorderStyle = BorderStyle.FixedSingle;

                Label warningLabel = new Label();
                warningLabel.Left = 20;
                warningLabel.Top = 95;
                warningLabel.Width = 460;
                warningLabel.Height = 65;
                warningLabel.Text =
                    "For safety, QuickForge will not download this as a plain text file.\n" +
                    "Copy it and store it somewhere safe. After rotation, the old recovery key will stop working.";
                warningLabel.ForeColor = Color.FromArgb(255, 190, 90);
                warningLabel.BackColor = Color.Transparent;

                Button copyButton = new Button();
                copyButton.Text = "Copy recovery key";
                copyButton.Left = 20;
                copyButton.Top = 170;
                copyButton.Width = 160;
                copyButton.Height = 32;
                StyleActionButton(copyButton, true);

                CheckBox savedCheckBox = new CheckBox();
                savedCheckBox.Text = "I have saved this recovery key safely";
                savedCheckBox.Left = 20;
                savedCheckBox.Top = 215;
                savedCheckBox.Width = 330;
                savedCheckBox.Height = 28;
                savedCheckBox.ForeColor = softTextColor;
                savedCheckBox.BackColor = Color.Transparent;

                Button cancelButton = new Button();
                cancelButton.Text = "Cancel";
                cancelButton.Left = 230;
                cancelButton.Top = 255;
                cancelButton.Width = 95;
                cancelButton.Height = 32;
                cancelButton.DialogResult = DialogResult.Cancel;
                StyleActionButton(cancelButton);

                Button confirmButton = new Button();
                confirmButton.Text = "Confirm rotation";
                confirmButton.Left = 340;
                confirmButton.Top = 255;
                confirmButton.Width = 140;
                confirmButton.Height = 32;
                confirmButton.Enabled = false;
                StyleActionButton(confirmButton, true);

                void UpdateConfirmState()
                {
                    confirmButton.Enabled = copied && savedCheckBox.Checked;
                }

                copyButton.Click += (s, e) =>
                {
                    Clipboard.SetText(newRecoveryKey);
                    copied = true;
                    copyButton.Text = "Copied";
                    _ = ClearClipboardLaterAsync(newRecoveryKey, 60000);
                    UpdateConfirmState();
                };

                savedCheckBox.CheckedChanged += (s, e) =>
                {
                    UpdateConfirmState();
                };

                confirmButton.Click += (s, e) =>
                {
                    confirmed = true;
                    dialog.DialogResult = DialogResult.OK;
                    keyTextBox.Clear();
                    dialog.Close();
                };

                dialog.Controls.Add(titleLabel);
                dialog.Controls.Add(keyTextBox);
                dialog.Controls.Add(warningLabel);
                dialog.Controls.Add(copyButton);
                dialog.Controls.Add(savedCheckBox);
                dialog.Controls.Add(confirmButton);
                dialog.Controls.Add(cancelButton);

                dialog.AcceptButton = confirmButton;
                dialog.CancelButton = cancelButton;

                dialog.ShowDialog(this);
            }

            return confirmed;
        }

        private void CheckRecoveryKeyReminder()
        {
            if (hasShownRecoveryReminderThisSession)
            {
                return;
            }

            int days = currentVaultSettings.RecoveryKeyReminderDays;

            if (days <= 0)
            {
                return;
            }

            DateTime lastRotation = currentVaultSettings.LastRecoveryKeyRotatedAt;

            if (lastRotation == default)
            {
                currentVaultSettings.LastRecoveryKeyRotatedAt = DateTime.UtcNow;
                return;
            }

            double elapsedDays = (DateTime.UtcNow - lastRotation).TotalDays;

            if (elapsedDays >= days)
            {
                hasShownRecoveryReminderThisSession = true;

                DialogResult result = MessageBox.Show(
                    "It has been about " + days + " days since you last changed your recovery key.\n\n" +
                    "Do you want to create a new recovery key now?",
                    "Recovery key reminder",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );

                if (result == DialogResult.Yes)
                {
                    RotateRecoveryKeyButton_Click(this, EventArgs.Empty);
                }
            }
        }
        private void ShowBackupDialog()
        {
            using (Form dialog = new Form())
            {
                dialog.Width = 500;
                dialog.Height = 280;
                dialog.Text = "Encrypted Backup";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.BackColor = Color.FromArgb(16, 20, 34);

                Label titleLabel = new Label();
                titleLabel.Text = "Encrypted backup";
                titleLabel.Left = 20;
                titleLabel.Top = 18;
                titleLabel.Width = 420;
                titleLabel.Height = 30;
                titleLabel.ForeColor = Color.White;
                titleLabel.BackColor = Color.Transparent;
                titleLabel.Font = new Font("Segoe UI", 14, FontStyle.Bold);

                Label infoLabel = new Label();
                infoLabel.Text =
                    "Export a safe encrypted backup, or import one if Google sync has a problem.";
                infoLabel.Left = 20;
                infoLabel.Top = 52;
                infoLabel.Width = 430;
                infoLabel.Height = 40;
                infoLabel.ForeColor = softTextColor;
                infoLabel.BackColor = Color.Transparent;

                Label warningLabel = new Label();
                warningLabel.Text =
                    "Backup files are encrypted. You still need your vault code or recovery key to open them.";
                warningLabel.Left = 20;
                warningLabel.Top = 95;
                warningLabel.Width = 430;
                warningLabel.Height = 45;
                warningLabel.ForeColor = Color.FromArgb(255, 190, 90);
                warningLabel.BackColor = Color.Transparent;

                Button exportButton = new Button();
                exportButton.Text = "Export encrypted backup";
                exportButton.Left = 20;
                exportButton.Top = 155;
                exportButton.Width = 190;
                exportButton.Height = 36;
                StyleActionButton(exportButton, true);
                exportButton.Click += (s, e) =>
                {
                    dialog.Close();
                    ExportEncryptedBackup();
                };

                Button importButton = new Button();
                importButton.Text = "Import encrypted backup";
                importButton.Left = 225;
                importButton.Top = 155;
                importButton.Width = 190;
                importButton.Height = 36;
                StyleActionButton(importButton);
                importButton.Click += async (s, e) =>
                {
                    dialog.Close();
                    await ImportEncryptedBackupAsync();
                };

                Button closeButton = new Button();
                closeButton.Text = "Close";
                closeButton.Left = 360;
                closeButton.Top = 210;
                closeButton.Width = 95;
                closeButton.Height = 32;
                StyleActionButton(closeButton);
                closeButton.Click += (s, e) => dialog.Close();

                dialog.Controls.Add(titleLabel);
                dialog.Controls.Add(infoLabel);
                dialog.Controls.Add(warningLabel);
                dialog.Controls.Add(exportButton);
                dialog.Controls.Add(importButton);
                dialog.Controls.Add(closeButton);

                dialog.ShowDialog(this);
            }
        }

        private string CreateCurrentEncryptedVaultJson()
        {
            if (currentDataKey == null || currentEncryptedVaultFile == null)
            {
                throw new InvalidOperationException("Unlock the vault first.");
            }

            VaultData vaultData = new VaultData
            {
                Entries = new List<VaultEntry>(vaultEntries),
                Settings = currentVaultSettings,
                UpdatedAt = DateTime.UtcNow
            };

            string encryptedJson = VaultCryptoService.EncryptVaultDataWithExistingKeys(
                vaultData,
                currentDataKey,
                currentEncryptedVaultFile
            );

            currentEncryptedVaultFile = System.Text.Json.JsonSerializer
                .Deserialize<EncryptedVaultFile>(encryptedJson);

            return encryptedJson;
        }

        private void ExportEncryptedBackup()
        {
            if (!isVaultUnlocked)
            {
                MessageBox.Show("Unlock the vault before exporting a backup.");
                return;
            }

            try
            {
                string encryptedJson = CreateCurrentEncryptedVaultJson();

                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    saveDialog.Title = "Export encrypted backup";
                    saveDialog.FileName = "QuickForge-Backup.qfvault";
                    saveDialog.Filter = "QuickForge encrypted backup (*.qfvault)|*.qfvault|JSON files (*.json)|*.json|All files (*.*)|*.*";

                    if (saveDialog.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    File.WriteAllText(saveDialog.FileName, encryptedJson);

                    selectedPreviewLabel.Text = "Encrypted backup exported.";
                    MessageBox.Show("Encrypted backup exported successfully.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not export backup: " + ex.Message);
            }
        }

        private async Task ImportEncryptedBackupAsync()
        {
            if (currentDriveService == null)
            {
                MessageBox.Show("Connect Google first, then import the backup.");
                return;
            }

            using (OpenFileDialog openDialog = new OpenFileDialog())
            {
                openDialog.Title = "Import encrypted backup";
                openDialog.Filter = "QuickForge encrypted backup (*.qfvault)|*.qfvault|JSON files (*.json)|*.json|All files (*.*)|*.*";

                if (openDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    string encryptedJson = File.ReadAllText(openDialog.FileName);

                    string? unlockCode = ShowPasswordPrompt(
                        "Import Backup",
                        "Enter the vault code or recovery key for this backup:"
                    );

                    if (string.IsNullOrWhiteSpace(unlockCode))
                    {
                        selectedPreviewLabel.Text = "Import cancelled.";
                        return;
                    }

                    VaultData importedVaultData = VaultCryptoService.DecryptVault(
                        encryptedJson,
                        unlockCode,
                        out byte[] importedDataKey,
                        out EncryptedVaultFile importedEncryptedVaultFile
                    );
                    bool importConfirmed = ShowImportBackupPreviewDialog(importedVaultData);

                    if (!importConfirmed)
                    {
                        selectedPreviewLabel.Text = "Import cancelled.";
                        return;
                    }

                    await GoogleDriveVaultService.UploadEncryptedVaultAsync(
                        currentDriveService,
                        encryptedJson
                    );

                    vaultCode = unlockCode;
                    currentDataKey = importedDataKey;
                    currentEncryptedVaultFile = importedEncryptedVaultFile;
                    currentVaultSettings = importedVaultData.Settings ?? new VaultSettings();

                    vaultEntries.Clear();

                    foreach (VaultEntry entry in importedVaultData.Entries)
                    {
                        vaultEntries.Add(entry);
                    }

                    cloudVaultExists = true;

                    RefreshVaultList();
                    GrantSecretAccessWindow();
                    ShowVaultUi();

                    selectedPreviewLabel.Text = "Encrypted backup imported and synced.";
                    MessageBox.Show("Encrypted backup imported successfully.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not import backup: " + ex.Message);
                }
            }
        }
        private bool ShowImportBackupPreviewDialog(VaultData importedVaultData)
        {
            bool confirmed = false;

            int totalEntries = importedVaultData.Entries.Count;
            int favoriteEntries = importedVaultData.Entries.Count(entry => entry.IsFavorite);
            int missingWebsiteLinks = importedVaultData.Entries.Count(entry =>
                string.IsNullOrWhiteSpace(entry.Website)
            );

            string updatedText = importedVaultData.UpdatedAt == DateTime.MinValue
                ? "Unknown"
                : importedVaultData.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

            using (Form dialog = new Form())
            {
                dialog.Width = 560;
                dialog.Height = 365;
                dialog.Text = "Import encrypted backup";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.BackColor = Color.FromArgb(16, 20, 34);

                Label titleLabel = new Label();
                titleLabel.Text = "Encrypted backup verified";
                titleLabel.Left = 22;
                titleLabel.Top = 18;
                titleLabel.Width = 500;
                titleLabel.Height = 28;
                titleLabel.ForeColor = Color.White;
                titleLabel.BackColor = Color.Transparent;
                titleLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);

                Label summaryLabel = new Label();
                summaryLabel.Text =
                    "This backup contains:" + Environment.NewLine + Environment.NewLine +
                    "Saved entries: " + totalEntries + Environment.NewLine +
                    "Favorites: " + favoriteEntries + Environment.NewLine +
                    "Missing website links: " + missingWebsiteLinks + Environment.NewLine +
                    "Last updated: " + updatedText;
                summaryLabel.Left = 22;
                summaryLabel.Top = 58;
                summaryLabel.Width = 500;
                summaryLabel.Height = 120;
                summaryLabel.ForeColor = softTextColor;
                summaryLabel.BackColor = Color.Transparent;
                summaryLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);

                Label warningLabel = new Label();
                warningLabel.Text =
                    "Importing this backup will replace your current cloud vault after upload." +
                    Environment.NewLine +
                    "Cancel now if this is not the backup you expected.";
                warningLabel.Left = 22;
                warningLabel.Top = 190;
                warningLabel.Width = 500;
                warningLabel.Height = 55;
                warningLabel.ForeColor = Color.FromArgb(255, 190, 90);
                warningLabel.BackColor = Color.Transparent;
                warningLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);

                Button cancelButton = new Button();
                cancelButton.Text = "Cancel";
                cancelButton.Left = 285;
                cancelButton.Top = 275;
                cancelButton.Width = 95;
                cancelButton.Height = 34;
                cancelButton.DialogResult = DialogResult.Cancel;
                StyleActionButton(cancelButton);

                Button importButton = new Button();
                importButton.Text = "Import and replace vault";
                importButton.Left = 395;
                importButton.Top = 275;
                importButton.Width = 135;
                importButton.Height = 34;
                StyleActionButton(importButton, true);

                importButton.Click += (s, e) =>
                {
                    confirmed = true;
                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                };

                cancelButton.Click += (s, e) =>
                {
                    confirmed = false;
                    dialog.DialogResult = DialogResult.Cancel;
                    dialog.Close();
                };

                dialog.Controls.Add(titleLabel);
                dialog.Controls.Add(summaryLabel);
                dialog.Controls.Add(warningLabel);
                dialog.Controls.Add(cancelButton);
                dialog.Controls.Add(importButton);

                dialog.AcceptButton = importButton;
                dialog.CancelButton = cancelButton;

                dialog.ShowDialog(this);
            }

            return confirmed;
        }
        private void ShowSecurityCenterDialog()
        {
            int totalEntries = vaultEntries.Count;
            int favoriteEntries = vaultEntries.Count(entry => entry.IsFavorite);
            int weakPasswords = CountWeakPasswords();
            int reusedPasswordEntries = CountReusedPasswordEntries();
            int missingWebsiteLinks = vaultEntries.Count(entry =>
                string.IsNullOrWhiteSpace(entry.Website)
            );

            string autoLockText = currentVaultSettings.AutoLockMinutes <= 0
                ? "Off"
                : currentVaultSettings.AutoLockMinutes + " minutes";

            string recoveryReminderText = currentVaultSettings.RecoveryKeyReminderDays <= 0
                ? "Never"
                : currentVaultSettings.RecoveryKeyReminderDays + " days";

            string summary;

            if (totalEntries == 0)
            {
                summary = "Your vault is ready. Add your first login.";
            }
            else if (weakPasswords == 0 && reusedPasswordEntries == 0)
            {
                summary = "Your vault looks good.";
            }
            else
            {
                summary = "Some passwords need attention.";
            }

            using (Form dialog = new Form())
            {
                dialog.Width = 520;
                dialog.Height = 430;
                dialog.Text = "Security Center";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.BackColor = Color.FromArgb(16, 20, 34);

                Label titleLabel = new Label();
                titleLabel.Text = "Security Center";
                titleLabel.Left = 20;
                titleLabel.Top = 18;
                titleLabel.Width = 440;
                titleLabel.Height = 30;
                titleLabel.ForeColor = Color.White;
                titleLabel.BackColor = Color.Transparent;
                titleLabel.Font = new Font("Segoe UI", 14, FontStyle.Bold);

                Label summaryLabel = new Label();
                summaryLabel.Text = summary;
                summaryLabel.Left = 20;
                summaryLabel.Top = 52;
                summaryLabel.Width = 440;
                summaryLabel.Height = 28;
                summaryLabel.ForeColor =
                    weakPasswords == 0 && reusedPasswordEntries == 0
                        ? successColor
                        : Color.FromArgb(255, 190, 90);
                summaryLabel.BackColor = Color.Transparent;
                summaryLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);

                TextBox statusBox = new TextBox();
                statusBox.Left = 20;
                statusBox.Top = 90;
                statusBox.Width = 460;
                statusBox.Height = 220;
                statusBox.Multiline = true;
                statusBox.ReadOnly = true;
                statusBox.WordWrap = true;
                statusBox.ScrollBars = ScrollBars.Vertical;
                statusBox.BackColor = Color.FromArgb(24, 28, 44);
                statusBox.ForeColor = Color.White;
                statusBox.BorderStyle = BorderStyle.FixedSingle;

                statusBox.Text =
                    "Vault: " + (isVaultUnlocked ? "Unlocked" : "Locked") + Environment.NewLine +
                    "Google sync: " + (currentDriveService != null ? "Connected" : "Not connected") + Environment.NewLine +
                    "Auto-lock: " + autoLockText + Environment.NewLine +
                    "Clipboard cleanup: Active" + Environment.NewLine +
                    Environment.NewLine +
                    "Saved entries: " + totalEntries + Environment.NewLine +
                    "Favorites: " + favoriteEntries + Environment.NewLine +
                    "Weak passwords: " + weakPasswords + Environment.NewLine +
                    "Reused passwords: " + reusedPasswordEntries + Environment.NewLine +
                    "Missing website links: " + missingWebsiteLinks + Environment.NewLine +
                    "Recovery key reminder: " + recoveryReminderText;

                Label adviceLabel = new Label();
                adviceLabel.Left = 20;
                adviceLabel.Top = 320;
                adviceLabel.Width = 460;
                adviceLabel.Height = 40;
                adviceLabel.ForeColor = softTextColor;
                adviceLabel.BackColor = Color.Transparent;

                if (reusedPasswordEntries > 0)
                {
                    adviceLabel.Text = "Best next step: replace reused passwords first.";
                }
                else if (weakPasswords > 0)
                {
                    adviceLabel.Text = "Best next step: generate stronger passwords for weak entries.";
                }
                else if (totalEntries > 0 && favoriteEntries == 0)
                {
                    adviceLabel.Text = "Tip: add favorites to make QuickFill faster.";
                }
                else if (missingWebsiteLinks > 0)
                {
                    adviceLabel.Text = "Optional: add website links to make QuickFill smoother.";
                }
                else
                {
                    adviceLabel.Text = "No urgent action needed.";
                }

                Button closeButton = new Button();
                closeButton.Text = "Close";
                closeButton.Left = 380;
                closeButton.Top = 365;
                closeButton.Width = 100;
                closeButton.Height = 32;
                StyleActionButton(closeButton, true);
                closeButton.Click += (s, e) => dialog.Close();

                dialog.Controls.Add(titleLabel);
                dialog.Controls.Add(summaryLabel);
                dialog.Controls.Add(statusBox);
                dialog.Controls.Add(adviceLabel);
                dialog.Controls.Add(closeButton);

                dialog.ShowDialog(this);
            }
        }

        private int CountWeakPasswords()
        {
            int count = 0;

            foreach (VaultEntry entry in vaultEntries)
            {
                if (string.IsNullOrWhiteSpace(entry.Secret))
                {
                    continue;
                }

                if (IsWeakPasswordForSecurityCenter(entry.Secret, entry.Platform))
                {
                    count++;
                }
            }

            return count;
        }

        private bool IsWeakPasswordForSecurityCenter(string password, string platform)
        {
            if (password.Length < 8)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(platform) &&
                password.Contains(platform, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            bool hasLower = password.Any(char.IsLower);
            bool hasUpper = password.Any(char.IsUpper);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSymbol = password.Any(ch => !char.IsLetterOrDigit(ch));

            int varietyScore = 0;

            if (hasLower) varietyScore++;
            if (hasUpper) varietyScore++;
            if (hasDigit) varietyScore++;
            if (hasSymbol) varietyScore++;

            return password.Length < 12 || varietyScore < 3;
        }

        private int CountReusedPasswordEntries()
        {
            return vaultEntries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Secret))
                .GroupBy(entry => entry.Secret)
                .Where(group => group.Count() > 1)
                .Sum(group => group.Count());
        }
        private bool HasActiveSecretAccess()
        {
            return isVaultUnlocked &&
                   currentEncryptedVaultFile != null &&
                   DateTime.UtcNow <= secretAccessValidUntilUtc;
        }

        private void GrantSecretAccessWindow()
        {
            secretAccessValidUntilUtc = DateTime.UtcNow.AddMinutes(SecretAccessMinutes);
        }

        private void ClearSecretAccessWindow()
        {
            secretAccessValidUntilUtc = DateTime.MinValue;
        }

        private bool EnsureSecretAccessForSecretAction()
        {
            if (HasActiveSecretAccess())
            {
                return true;
            }

            if (currentEncryptedVaultFile == null)
            {
                MessageBox.Show("Vault is not ready.");
                return false;
            }

            string? input = ShowPasswordPrompt(
                "Verify Vault Access",
                "Enter your vault code or recovery key. This will allow reveal/copy for 10 minutes:"
            );

            if (
                input == null ||
                !VaultCryptoService.CanUnlockVault(currentEncryptedVaultFile, input)
            )
            {
                MessageBox.Show("Wrong vault code or recovery key.");
                return false;
            }

            GrantSecretAccessWindow();
            return true;
        }
        private void HideRevealTimer_Tick(object? sender, EventArgs e)
        {
            hideRevealTimer.Stop();

            VaultEntry? entry = GetSelectedEntry();

            if (entry != null)
            {
                SetPreviewText(
                "Selected: " + entry.GetDisplayName(),
                "User: " + MaskEmpty(entry.Username),
                "Password/code: " + MaskSecret(entry.Secret));
            }
        }

        private VaultEntry? GetSelectedEntry()
        {
            int index = vaultListBox.SelectedIndex;

            if (index < 0 || index >= visibleVaultEntries.Count)
            {
                return null;
            }

            return visibleVaultEntries[index];
        }

        private void RefreshVaultList()
        {
            VaultEntry? selectedBeforeRefresh = GetSelectedEntry();

            vaultListBox.Items.Clear();
            visibleVaultEntries.Clear();

            string cleanFilter = vaultSearchTextBox.Text.Trim().ToLowerInvariant();

            IEnumerable<VaultEntry> filteredEntries = vaultEntries;

            if (!string.IsNullOrWhiteSpace(cleanFilter))
            {
                filteredEntries = filteredEntries.Where(entry =>
                {
                    string searchable =
                        (entry.Platform + " " +
                         entry.Username + " " +
                         entry.Website + " " +
                         entry.Note)
                        .ToLowerInvariant();

                    return searchable.Contains(cleanFilter);
                });
            }

            foreach (VaultEntry entry in filteredEntries)
            {
                visibleVaultEntries.Add(entry);

                string prefix = entry.IsFavorite ? "⭐ " : "";
                vaultListBox.Items.Add(prefix + entry.GetDisplayName());
            }

            if (vaultEntries.Count == 0)
            {
                selectedPreviewLabel.Text =
                    "No saved logins yet." + Environment.NewLine +
                    "Add your first login on the left.";
            }
            else if (visibleVaultEntries.Count == 0)
            {
                selectedPreviewLabel.Text =
                    "No matching entries found." + Environment.NewLine +
                    "Try another search.";
            }
            else if (selectedBeforeRefresh != null)
            {
                int newIndex = visibleVaultEntries.IndexOf(selectedBeforeRefresh);

                if (newIndex >= 0 && newIndex < vaultListBox.Items.Count)
                {
                    vaultListBox.SelectedIndex = newIndex;
                }
            }

            UpdateFavoriteButtonText();
        }

        private string MaskEmpty(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "(empty)";
            }

            return value;
        }

        private string MaskSecret(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
            {
                return "(empty)";
            }

            return "********";
        }
        private VaultEntry? FindDuplicateSecret(string secret, VaultEntry? ignoredEntry)
        {
            if (string.IsNullOrWhiteSpace(secret))
            {
                return null;
            }

            return vaultEntries.FirstOrDefault(entry =>
                entry != ignoredEntry &&
                !string.IsNullOrWhiteSpace(entry.Secret) &&
                entry.Secret == secret
            );
        }

        private bool HandleDuplicatePasswordBeforeSave(string secret, VaultEntry? ignoredEntry)
        {
            VaultEntry? duplicateEntry = FindDuplicateSecret(secret, ignoredEntry);

            if (duplicateEntry == null)
            {
                return true;
            }

            DuplicatePasswordChoice choice = ShowDuplicatePasswordDialog(duplicateEntry);

            if (choice == DuplicatePasswordChoice.SaveAnyway)
            {
                return true;
            }

            if (choice == DuplicatePasswordChoice.GenerateNewPassword)
            {
                string newPassword = GenerateUniquePassword("Strong");
                secretTextBox.Text = newPassword;

                selectedPreviewLabel.Text =
                    "Generated a new password. Review it, then click save again.";

                return false;
            }

            selectedPreviewLabel.Text = "Save cancelled.";
            return false;
        }

        private DuplicatePasswordChoice ShowDuplicatePasswordDialog(VaultEntry duplicateEntry)
        {
            DuplicatePasswordChoice choice = DuplicatePasswordChoice.Cancel;

            using (Form dialog = new Form())
            {
                dialog.Width = 470;
                dialog.Height = 250;
                dialog.Text = "Password already used";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.BackColor = Color.FromArgb(16, 20, 34);

                Label titleLabel = new Label();
                titleLabel.Text = "This password is already used";
                titleLabel.Left = 20;
                titleLabel.Top = 18;
                titleLabel.Width = 410;
                titleLabel.Height = 28;
                titleLabel.ForeColor = Color.White;
                titleLabel.BackColor = Color.Transparent;
                titleLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);

                Label messageLabel = new Label();
                messageLabel.Text =
                    "This password is already used for:\n\n" +
                    duplicateEntry.GetDisplayName() +
                    "\n\nUsing the same password twice is risky.";
                messageLabel.Left = 20;
                messageLabel.Top = 55;
                messageLabel.Width = 410;
                messageLabel.Height = 90;
                messageLabel.ForeColor = softTextColor;
                messageLabel.BackColor = Color.Transparent;

                Button saveAnywayButton = new Button();
                saveAnywayButton.Text = "Save anyway";
                saveAnywayButton.Left = 20;
                saveAnywayButton.Top = 160;
                saveAnywayButton.Width = 115;
                saveAnywayButton.Height = 34;
                StyleActionButton(saveAnywayButton);

                Button generateButton = new Button();
                generateButton.Text = "Generate new";
                generateButton.Left = 150;
                generateButton.Top = 160;
                generateButton.Width = 125;
                generateButton.Height = 34;
                StyleActionButton(generateButton, true);

                Button cancelButton = new Button();
                cancelButton.Text = "Cancel";
                cancelButton.Left = 290;
                cancelButton.Top = 160;
                cancelButton.Width = 100;
                cancelButton.Height = 34;
                StyleActionButton(cancelButton);

                saveAnywayButton.Click += (s, e) =>
                {
                    choice = DuplicatePasswordChoice.SaveAnyway;
                    dialog.Close();
                };

                generateButton.Click += (s, e) =>
                {
                    choice = DuplicatePasswordChoice.GenerateNewPassword;
                    dialog.Close();
                };

                cancelButton.Click += (s, e) =>
                {
                    choice = DuplicatePasswordChoice.Cancel;
                    dialog.Close();
                };

                dialog.Controls.Add(titleLabel);
                dialog.Controls.Add(messageLabel);
                dialog.Controls.Add(saveAnywayButton);
                dialog.Controls.Add(generateButton);
                dialog.Controls.Add(cancelButton);

                dialog.ShowDialog(this);
            }

            return choice;
        }
        private enum PasswordGeneratorTarget
        {
            VaultField,
            QuickFill
        }
        private enum DuplicatePasswordChoice
        {
            Cancel,
            SaveAnyway,
            GenerateNewPassword
        }
        private async Task FillGeneratedPasswordAsync(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            if (quickFillTargetWindow == IntPtr.Zero)
            {
                Clipboard.SetText(password);
                MessageBox.Show("Password copied. Click the password field and paste it.");
                return;
            }

            quickFillForm?.Hide();

            Clipboard.SetText(password);

            await Task.Delay(250);

            SetForegroundWindow(quickFillTargetWindow);

            await Task.Delay(350);

            SendKeys.SendWait("^v");

            _ = ClearClipboardLaterAsync(password, 20000);
        }

        private void ShowCreatePasswordDialog(PasswordGeneratorTarget target)
        {
            string currentPassword = "";

            using (Form dialog = new Form())
            {
                dialog.Width = 460;
                dialog.Height = 330;
                dialog.Text = "Generate password";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.BackColor = Color.FromArgb(16, 20, 34);

                Label titleLabel = new Label();
                titleLabel.Text = "Generate password";
                titleLabel.Left = 20;
                titleLabel.Top = 18;
                titleLabel.Width = 250;
                titleLabel.Height = 28;
                titleLabel.ForeColor = Color.White;
                titleLabel.BackColor = Color.Transparent;
                titleLabel.Font = new Font("Segoe UI", 14, FontStyle.Bold);

                Label subtitleLabel = new Label();
                subtitleLabel.Text = "New password — not saved yet.";
                subtitleLabel.Left = 20;
                subtitleLabel.Top = 50;
                subtitleLabel.Width = 360;
                subtitleLabel.Height = 22;
                subtitleLabel.ForeColor = softTextColor;
                subtitleLabel.BackColor = Color.Transparent;

                Label typeLabel = new Label();
                typeLabel.Text = "Type";
                typeLabel.Left = 20;
                typeLabel.Top = 82;
                typeLabel.Width = 100;
                typeLabel.Height = 22;
                typeLabel.ForeColor = Color.White;
                typeLabel.BackColor = Color.Transparent;
                typeLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);

                ComboBox typeComboBox = new ComboBox();
                typeComboBox.Left = 20;
                typeComboBox.Top = 105;
                typeComboBox.Width = 180;
                typeComboBox.Height = 28;
                typeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                typeComboBox.Items.Add("Easy to remember");
                typeComboBox.Items.Add("Strong");
                typeComboBox.Items.Add("Code style");
                typeComboBox.SelectedIndex = 1;

                TextBox passwordBox = new TextBox();
                passwordBox.Left = 20;
                passwordBox.Top = 150;
                passwordBox.Width = 400;
                passwordBox.Height = 55;
                passwordBox.Multiline = true;
                passwordBox.ReadOnly = true;
                passwordBox.ScrollBars = ScrollBars.Vertical;
                passwordBox.WordWrap = true;
                passwordBox.BackColor = Color.FromArgb(24, 28, 44);
                passwordBox.ForeColor = Color.White;
                passwordBox.BorderStyle = BorderStyle.FixedSingle;

                Label statusLabel = new Label();
                statusLabel.Text = "Ready.";
                statusLabel.Left = 20;
                statusLabel.Top = 210;
                statusLabel.Width = 400;
                statusLabel.Height = 22;
                statusLabel.ForeColor = softTextColor;
                statusLabel.BackColor = Color.Transparent;

                Button generateAgainButton = new Button();
                generateAgainButton.Text = "Generate again";
                generateAgainButton.Left = 220;
                generateAgainButton.Top = 103;
                generateAgainButton.Width = 130;
                generateAgainButton.Height = 32;
                StyleActionButton(generateAgainButton);

                Button copyButton = new Button();
                copyButton.Text = "Copy password";
                copyButton.Left = 20;
                copyButton.Top = 245;
                copyButton.Width = 125;
                copyButton.Height = 32;
                StyleActionButton(copyButton);

                Button fillButton = new Button();
                fillButton.Text = "Fill password";
                fillButton.Left = 155;
                fillButton.Top = 245;
                fillButton.Width = 120;
                fillButton.Height = 32;
                StyleActionButton(fillButton, true);
                fillButton.Visible = target == PasswordGeneratorTarget.QuickFill;

                Button useInVaultButton = new Button();
                useInVaultButton.Text = "Use in vault";
                useInVaultButton.Left = 155;
                useInVaultButton.Top = 245;
                useInVaultButton.Width = 120;
                useInVaultButton.Height = 32;
                StyleActionButton(useInVaultButton, true);
                useInVaultButton.Visible = target == PasswordGeneratorTarget.VaultField;

                Button cancelButton = new Button();
                cancelButton.Text = "Cancel";
                cancelButton.Left = 290;
                cancelButton.Top = 245;
                cancelButton.Width = 90;
                cancelButton.Height = 32;
                StyleActionButton(cancelButton);
                cancelButton.Click += (s, e) => dialog.Close();

                void GenerateNewPassword()
                {
                    string type = typeComboBox.SelectedItem?.ToString() ?? "Strong";
                    currentPassword = GenerateUniquePassword(type);
                    passwordBox.Text = currentPassword;
                    statusLabel.Text = "New password — not saved yet.";
                }

                generateAgainButton.Click += (s, e) =>
                {
                    GenerateNewPassword();
                };

                typeComboBox.SelectedIndexChanged += (s, e) =>
                {
                    GenerateNewPassword();
                };

                copyButton.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(currentPassword))
                    {
                        return;
                    }

                    Clipboard.SetText(currentPassword);
                    statusLabel.Text = "Password copied. Clipboard clears in 20 seconds.";
                    _ = ClearClipboardLaterAsync(currentPassword, 20000);
                };

                fillButton.Click += async (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(currentPassword))
                    {
                        return;
                    }

                    statusLabel.Text = "Filling password...";
                    dialog.Hide();

                    await FillGeneratedPasswordAsync(currentPassword);

                    dialog.Close();
                };

                useInVaultButton.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(currentPassword))
                    {
                        return;
                    }

                    secretTextBox.Text = currentPassword;
                    dialog.Close();
                };

                dialog.Controls.Add(titleLabel);
                dialog.Controls.Add(subtitleLabel);
                dialog.Controls.Add(typeLabel);
                dialog.Controls.Add(typeComboBox);
                dialog.Controls.Add(generateAgainButton);
                dialog.Controls.Add(passwordBox);
                dialog.Controls.Add(statusLabel);
                dialog.Controls.Add(copyButton);
                dialog.Controls.Add(fillButton);
                dialog.Controls.Add(useInVaultButton);
                dialog.Controls.Add(cancelButton);

                GenerateNewPassword();

                dialog.ShowDialog(this);
            }
        }

        private string GenerateUniquePassword(string type)
        {
            for (int i = 0; i < 100; i++)
            {
                string password = GeneratePasswordByType(type);

                if (!vaultEntries.Any(entry => entry.Secret == password))
                {
                    return password;
                }
            }

            return GeneratePasswordByType(type);
        }

        private string GeneratePasswordByType(string type)
        {
            if (type == "Easy to remember")
            {
                return GenerateReadablePassword();
            }

            if (type == "Code style")
            {
                return GenerateCodeStylePassword();
            }

            return GenerateStrongPassword();
        }

        private string GenerateStrongPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%*-_+=?";
            char[] result = new char[22];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
            }

            return new string(result);
        }

        private string GenerateReadablePassword()
        {
            string[] firstWords =
            {
        "River", "Wolf", "Nova", "Dragon", "Pixel", "Storm",
        "Forest", "Rocket", "Shadow", "Crystal", "Falcon", "Ocean"
    };

            string[] secondWords =
            {
        "Gate", "Blade", "Stone", "Light", "Forge", "Cloud",
        "Mage", "Runner", "Shield", "Flame", "Tower", "Star"
    };

            string first = firstWords[RandomNumberGenerator.GetInt32(firstWords.Length)];
            string second = secondWords[RandomNumberGenerator.GetInt32(secondWords.Length)];
            int number = RandomNumberGenerator.GetInt32(10, 99);

            string[] symbols = { "!", "#", "%", "?" };
            string symbol = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];

            return first + "-" + second + "-" + number + symbol;
        }

        private string GenerateCodeStylePassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

            string MakeGroup()
            {
                char[] group = new char[4];

                for (int i = 0; i < group.Length; i++)
                {
                    group[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
                }

                return new string(group);
            }

            return MakeGroup() + "-" + MakeGroup() + "-" + MakeGroup() + "-" + MakeGroup();
        }

        private void StyleActionButton(Button button, bool primary = false)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI", 8, FontStyle.Bold);

            if (primary)
            {
                button.BackColor = Color.FromArgb(45, 90, 160);
                button.FlatAppearance.BorderColor = borderColor;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 105, 180);
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(35, 75, 140);
            }
            else
            {
                button.BackColor = Color.FromArgb(35, 40, 60);
                button.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 52, 75);
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(25, 30, 48);
            }
        }
        private void UpdatePasswordStrengthPreview()
        {
            string password = secretTextBox.Text;
            string platform = platformTextBox.Text;

            if (string.IsNullOrWhiteSpace(password))
            {
                passwordStrengthLabel.Text = "Strength: Not checked yet";
                passwordStrengthLabel.ForeColor = softTextColor;
                passwordStrengthFill.Width = 0;
                passwordStrengthFill.BackColor = softTextColor;
                return;
            }

            PasswordStrengthResult result = CheckPasswordStrength(password, platform);

            passwordStrengthLabel.Text = "Strength: " + result.Title + " — " + result.Hint;
            passwordStrengthLabel.ForeColor = result.Color;
            passwordStrengthFill.BackColor = result.Color;

            int maxWidth = passwordStrengthTrack.Width;
            passwordStrengthFill.Width = Math.Max(8, (int)(maxWidth * (result.Percent / 100.0)));
        }

        private PasswordStrengthResult CheckPasswordStrength(string password, string platform)
        {
            bool alreadyUsed = vaultEntries.Any(entry =>
                entry != editingEntry &&
                !string.IsNullOrWhiteSpace(entry.Secret) &&
                entry.Secret == password
            );

            if (alreadyUsed)
            {
                return new PasswordStrengthResult(
                    "Risky",
                    "already used",
                    20,
                    dangerColor
                );
            }

            if (!string.IsNullOrWhiteSpace(platform) &&
                password.Contains(platform, StringComparison.OrdinalIgnoreCase))
            {
                return new PasswordStrengthResult(
                    "Risky",
                    "avoid service name",
                    25,
                    dangerColor
                );
            }

            int score = 0;

            if (password.Length >= 20)
            {
                score += 4;
            }
            else if (password.Length >= 16)
            {
                score += 3;
            }
            else if (password.Length >= 12)
            {
                score += 2;
            }
            else if (password.Length >= 8)
            {
                score += 1;
            }

            bool hasLower = password.Any(char.IsLower);
            bool hasUpper = password.Any(char.IsUpper);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSymbol = password.Any(ch => !char.IsLetterOrDigit(ch));

            if (hasLower) score++;
            if (hasUpper) score++;
            if (hasDigit) score++;
            if (hasSymbol) score++;

            if (password.Length < 8)
            {
                return new PasswordStrengthResult(
                    "Weak",
                    "add more characters",
                    20,
                    dangerColor
                );
            }

            if (score <= 3)
            {
                string hint = "add numbers or symbols";

                if (!hasUpper)
                {
                    hint = "add uppercase";
                }
                else if (!hasDigit)
                {
                    hint = "add a number";
                }
                else if (!hasSymbol)
                {
                    hint = "add a symbol";
                }

                return new PasswordStrengthResult(
                    "Weak",
                    hint,
                    30,
                    dangerColor
                );
            }

            if (score <= 5)
            {
                return new PasswordStrengthResult(
                    "Okay",
                    "can be stronger",
                    55,
                    Color.FromArgb(255, 190, 90)
                );
            }

            if (score <= 6)
            {
                return new PasswordStrengthResult(
                    "Strong",
                    "good password",
                    78,
                    Color.FromArgb(120, 210, 255)
                );
            }

            return new PasswordStrengthResult(
                "Very strong",
                "ready to save",
                100,
                successColor
            );
        }

        private class PasswordStrengthResult
        {
            public string Title { get; }
            public string Hint { get; }
            public int Percent { get; }
            public Color Color { get; }

            public PasswordStrengthResult(string title, string hint, int percent, Color color)
            {
                Title = title;
                Hint = hint;
                Percent = percent;
                Color = color;
            }
        }
        private void MarkVaultActivity()
        {
            lastVaultActivityUtc = DateTime.UtcNow;
        }

        private void ApplyPerformanceSettingsToUi()
        {
            animationEnabledCheckBox.Checked = currentVaultSettings.BackgroundAnimationEnabled;

            if (currentVaultSettings.AutoLockMinutes == 5)
            {
                autoLockComboBox.SelectedIndex = 1;
            }
            else if (currentVaultSettings.AutoLockMinutes == 10)
            {
                autoLockComboBox.SelectedIndex = 2;
            }
            else if (currentVaultSettings.AutoLockMinutes == 30)
            {
                autoLockComboBox.SelectedIndex = 3;
            }
            else
            {
                autoLockComboBox.SelectedIndex = 0;
            }

            UpdateAnimationState();
        }

        private void UpdateAnimationState()
        {
            bool shouldAnimate =
                animationEnabledCheckBox.Checked &&
                Visible &&
                WindowState != FormWindowState.Minimized;

            if (shouldAnimate)
            {
                if (!animationTimer.Enabled)
                {
                    animationTimer.Start();
                }
            }
            else
            {
                if (animationTimer.Enabled)
                {
                    animationTimer.Stop();
                }
            }
        }

        private void AutoLockTimer_Tick(object? sender, EventArgs e)
        {
            if (!isVaultUnlocked)
            {
                return;
            }

            int minutes = currentVaultSettings.AutoLockMinutes;

            if (minutes <= 0)
            {
                return;
            }

            double idleMinutes = (DateTime.UtcNow - lastVaultActivityUtc).TotalMinutes;

            if (idleMinutes >= minutes)
            {
                LockVaultForSafety("Vault locked for safety.");
            }
        }

        private void LockVaultForSafety(string message)
        {
            quickFillForm?.Hide();

            isVaultUnlocked = false;
            ClearSecretAccessWindow();

            vaultCode = "";
            currentDataKey = null;
            currentEncryptedVaultFile = null;

            vaultEntries.Clear();
            RefreshVaultList();
            ClearEntryInputs();
            SetEntryEditMode(false);

            selectedPreviewLabel.Text = message;

            try
            {
                if (Clipboard.ContainsText())
                {
                    Clipboard.Clear();
                }
            }
            catch
            {
                // Ignore clipboard errors.
            }

            ConfigureVaultAccessForUnlock();
            ShowVaultAccessUi();
        }
        private void ShowQuickFill()
        {
            IntPtr activeWindow = GetForegroundWindow();

            if (activeWindow != Handle)
            {
                quickFillTargetWindow = activeWindow;
            }
            if (currentDriveService == null)
            {
                ShowMainWindow();
                MessageBox.Show("Sign in with Google first.");
                return;
            }

            if (!isVaultUnlocked)
            {
                ShowMainWindow();
                MessageBox.Show("Unlock your vault first.");
                return;
            }

            if (vaultEntries.Count == 0)
            {
                ShowMainWindow();
                MessageBox.Show(
                    "No saved logins yet.\n\n" +
                    "Open QuickForge and save your first login."
                );
                return;
            }

            if (quickFillForm == null || quickFillForm.IsDisposed)
            {
                BuildQuickFillForm();
            }

            RefreshQuickFillList("");

            quickFillForm!.Show();
            quickFillForm.TopMost = true;
            quickFillForm.Activate();

            quickFillSearchBox!.Focus();
            quickFillSearchBox.SelectAll();
        }
        private void StyleQuickFillButton(Button button, bool primary = false)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI", 8, FontStyle.Bold);

            if (primary)
            {
                button.BackColor = Color.FromArgb(45, 90, 160);
                button.FlatAppearance.BorderColor = borderColor;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 105, 180);
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(35, 75, 140);
            }
            else
            {
                button.BackColor = Color.FromArgb(35, 40, 60);
                button.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 52, 75);
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(25, 30, 48);
            }
        }
        private void BuildQuickFillForm()
        {
            quickFillForm = new Form();
            quickFillForm.Text = "QuickFill";
            quickFillForm.Width = 760;
            quickFillForm.Height = 430;
            quickFillForm.StartPosition = FormStartPosition.CenterScreen;
            quickFillForm.FormBorderStyle = FormBorderStyle.FixedSingle;
            quickFillForm.MaximizeBox = false;
            quickFillForm.MinimizeBox = false;
            quickFillForm.BackColor = Color.FromArgb(16, 20, 34);

            Label createTitleLabel = new Label();
            createTitleLabel.Text = "Generate";
            createTitleLabel.Left = 22;
            createTitleLabel.Top = 18;
            createTitleLabel.Width = 180;
            createTitleLabel.Height = 28;
            createTitleLabel.ForeColor = Color.White;
            createTitleLabel.BackColor = Color.Transparent;
            createTitleLabel.Font = new Font("Segoe UI", 14, FontStyle.Bold);

            Label createHintLabel = new Label();
            createHintLabel.Text = "Generate a new password for signup pages.";
            createHintLabel.Left = 22;
            createHintLabel.Top = 50;
            createHintLabel.Width = 280;
            createHintLabel.Height = 45;
            createHintLabel.ForeColor = softTextColor;
            createHintLabel.BackColor = Color.Transparent;

            Button createPasswordQuickFillButton = new Button();
            createPasswordQuickFillButton.Text = "Generate password";
            createPasswordQuickFillButton.Left = 22;
            createPasswordQuickFillButton.Top = 105;
            createPasswordQuickFillButton.Width = 160;
            createPasswordQuickFillButton.Height = 34;
            StyleActionButton(createPasswordQuickFillButton, true);
            createPasswordQuickFillButton.Click += (s, e) =>
            {
                ShowCreatePasswordDialog(PasswordGeneratorTarget.QuickFill);
            };

            Label createStatusLabel = new Label();
            createStatusLabel.Text = "Generated passwords are not saved automatically.";
            createStatusLabel.Left = 22;
            createStatusLabel.Top = 155;
            createStatusLabel.Width = 270;
            createStatusLabel.Height = 60;
            createStatusLabel.ForeColor = softTextColor;
            createStatusLabel.BackColor = Color.Transparent;

            Panel divider = new Panel();
            divider.Left = 320;
            divider.Top = 20;
            divider.Width = 1;
            divider.Height = 340;
            divider.BackColor = Color.FromArgb(60, 70, 100);

            Label savedTitleLabel = new Label();
            savedTitleLabel.Text = "Saved logins";
            savedTitleLabel.Left = 350;
            savedTitleLabel.Top = 18;
            savedTitleLabel.Width = 220;
            savedTitleLabel.Height = 28;
            savedTitleLabel.ForeColor = Color.White;
            savedTitleLabel.BackColor = Color.Transparent;
            savedTitleLabel.Font = new Font("Segoe UI", 14, FontStyle.Bold);

            Label savedHintLabel = new Label();
            savedHintLabel.Text = "Search and use a saved login quickly.";
            savedHintLabel.Left = 350;
            savedHintLabel.Top = 50;
            savedHintLabel.Width = 340;
            savedHintLabel.Height = 22;
            savedHintLabel.ForeColor = softTextColor;
            savedHintLabel.BackColor = Color.Transparent;

            quickFillSearchBox = new TextBox();
            quickFillSearchBox.Left = 350;
            quickFillSearchBox.Top = 82;
            quickFillSearchBox.Width = 360;
            quickFillSearchBox.Height = 28;
            quickFillSearchBox.PlaceholderText = "Search YouTube, Steam, Discord...";

            quickFillListBox = new ListBox();
            quickFillListBox.Left = 350;
            quickFillListBox.Top = 122;
            quickFillListBox.Width = 360;
            quickFillListBox.Height = 150;
            quickFillListBox.BackColor = Color.FromArgb(24, 28, 44);
            quickFillListBox.ForeColor = Color.White;

            Button copyUserButton = new Button();
            copyUserButton.Text = "Copy username";
            copyUserButton.Left = 350;
            copyUserButton.Top = 290;
            copyUserButton.Width = 115;
            copyUserButton.Height = 32;
            StyleActionButton(copyUserButton);
            copyUserButton.Click += (s, e) => QuickFillCopyUsername();

            Button copyPasswordButton = new Button();
            copyPasswordButton.Text = "Copy password";
            copyPasswordButton.Left = 475;
            copyPasswordButton.Top = 290;
            copyPasswordButton.Width = 115;
            copyPasswordButton.Height = 32;
            StyleActionButton(copyPasswordButton);
            copyPasswordButton.Click += (s, e) => QuickFillCopyPassword();

            Button fillLoginButton = new Button();
            fillLoginButton.Text = "Fill login";
            fillLoginButton.Left = 600;
            fillLoginButton.Top = 290;
            fillLoginButton.Width = 110;
            fillLoginButton.Height = 32;
            StyleActionButton(fillLoginButton, true);
            fillLoginButton.Click += async (s, e) => await QuickFillAutoFillAsync();

            Button hideButton = new Button();
            hideButton.Text = "Hide";
            hideButton.Left = 600;
            hideButton.Top = 330;
            hideButton.Width = 110;
            hideButton.Height = 32;
            StyleActionButton(hideButton);
            hideButton.Click += (s, e) => quickFillForm.Hide();

            quickFillStatusLabel = new Label();
            quickFillStatusLabel.Text = "Tip: Ctrl + Alt + Q opens this window.";
            quickFillStatusLabel.Left = 350;
            quickFillStatusLabel.Top = 335;
            quickFillStatusLabel.Width = 235;
            quickFillStatusLabel.Height = 32;
            quickFillStatusLabel.ForeColor = softTextColor;
            quickFillStatusLabel.BackColor = Color.Transparent;

            quickFillSearchBox.TextChanged += (s, e) =>
            {
                RefreshQuickFillList(quickFillSearchBox.Text);
            };

            quickFillListBox.DoubleClick += async (s, e) =>
            {
                await QuickFillAutoFillAsync();
            };

            quickFillForm.Controls.Add(createTitleLabel);
            quickFillForm.Controls.Add(createHintLabel);
            quickFillForm.Controls.Add(createPasswordQuickFillButton);
            quickFillForm.Controls.Add(createStatusLabel);
            quickFillForm.Controls.Add(divider);

            quickFillForm.Controls.Add(savedTitleLabel);
            quickFillForm.Controls.Add(savedHintLabel);
            quickFillForm.Controls.Add(quickFillSearchBox);
            quickFillForm.Controls.Add(quickFillListBox);
            quickFillForm.Controls.Add(copyUserButton);
            quickFillForm.Controls.Add(copyPasswordButton);
            quickFillForm.Controls.Add(fillLoginButton);
            quickFillForm.Controls.Add(hideButton);
            quickFillForm.Controls.Add(quickFillStatusLabel);
        }
        private void RefreshQuickFillList(string filter)
        {
            if (quickFillListBox == null)
            {
                return;
            }

            quickFillListBox.Items.Clear();

            string cleanFilter = filter.Trim().ToLowerInvariant();

            IEnumerable<VaultEntry> orderedEntries = vaultEntries
                .OrderByDescending(entry => entry.IsFavorite)
                .ThenBy(entry => entry.GetDisplayName());

            foreach (VaultEntry entry in orderedEntries)
            {
                string searchable =
                    (entry.Platform + " " + entry.Username + " " + entry.Website + " " + entry.Note)
                    .ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(cleanFilter) || searchable.Contains(cleanFilter))
                {
                    quickFillListBox.Items.Add(new QuickFillItem(entry));
                }
            }

            if (quickFillListBox.Items.Count > 0)
            {
                quickFillListBox.SelectedIndex = 0;
                SetQuickFillStatus("Choose a login, then copy or fill.");
            }
            else
            {
                SetQuickFillStatus("No matching logins found.");
            }
        }

        private VaultEntry? GetSelectedQuickFillEntry()
        {
            if (quickFillListBox == null)
            {
                return null;
            }

            if (quickFillListBox.SelectedItem is QuickFillItem item)
            {
                return item.Entry;
            }

            return null;
        }

        private void QuickFillCopyUsername()
        {
            VaultEntry? entry = GetSelectedQuickFillEntry();

            if (entry == null)
            {
                SetQuickFillStatus("Choose a saved login first.");
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.Username))
            {
                SetQuickFillStatus("This login has no username.");
                return;
            }

            Clipboard.SetText(entry.Username);
            SetQuickFillStatus("Username copied.");
        }

        private void QuickFillCopyPassword()
        {
            VaultEntry? entry = GetSelectedQuickFillEntry();

            if (entry == null)
            {
                SetQuickFillStatus("Choose a saved login first.");
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.Secret))
            {
                SetQuickFillStatus("This login has no password.");
                return;
            }

            if (!EnsureSecretAccessForSecretAction())
            {
                return;
            }

            Clipboard.SetText(entry.Secret);
            SetQuickFillStatus("Password copied.");

            _ = ClearClipboardLaterAsync(entry.Secret, 20000);
        }

        private async Task QuickFillAutoFillAsync()
        {
            VaultEntry? entry = GetSelectedQuickFillEntry();

            if (entry == null)
            {
                SetQuickFillStatus("Choose a saved login first.");
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.Username))
            {
                SetQuickFillStatus("This login has no username.");
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.Secret))
            {
                SetQuickFillStatus("This login has no password.");
                return;
            }

            if (!EnsureSecretAccessForSecretAction())
            {
                return;
            }

            quickFillForm?.Hide();

            await Task.Delay(250);

            Clipboard.SetText(entry.Username);
            SendKeys.SendWait("^v");

            await Task.Delay(150);

            SendKeys.SendWait("{TAB}");

            await Task.Delay(150);

            Clipboard.SetText(entry.Secret);
            SendKeys.SendWait("^v");

            _ = ClearClipboardLaterAsync(entry.Secret, 20000);
        }

        private async Task ClearClipboardLaterAsync(string valueToClear, int delayMs)
        {
            await Task.Delay(delayMs);

            try
            {
                if (Clipboard.ContainsText() && Clipboard.GetText() == valueToClear)
                {
                    Clipboard.Clear();
                }
            }
            catch
            {
                // Ignore clipboard issues.
            }
        }

        private void SetQuickFillStatus(string text)
        {
            if (quickFillStatusLabel != null)
            {
                quickFillStatusLabel.Text = text;
            }
        }

        private class QuickFillItem
        {
            public VaultEntry Entry { get; }

            public QuickFillItem(VaultEntry entry)
            {
                Entry = entry;
            }

            public override string ToString()
            {
                string prefix = Entry.IsFavorite ? "⭐ " : "";

                if (!string.IsNullOrWhiteSpace(Entry.Platform) &&
                    !string.IsNullOrWhiteSpace(Entry.Username))
                {
                    return prefix + Entry.Platform + "  •  " + Entry.Username;
                }

                return prefix + Entry.GetDisplayName();
            }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                quickFillForm?.Hide();

                isVaultUnlocked = false;
                ClearSecretAccessWindow();

                vaultCode = "";
                vaultEntries.Clear();
                currentDataKey = null;
                currentEncryptedVaultFile = null;

                UnregisterHotKey(Handle, QuickFillHotkeyId);

                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            catch
            {
                // Ignore cleanup errors while closing.
            }

            base.OnFormClosed(e);
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotkey && m.WParam.ToInt32() == QuickFillHotkeyId)
            {
                ShowQuickFill();
                return;
            }

            base.WndProc(ref m);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
            }

            UpdateAnimationState();
        }
        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            time += 0.016f;
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
            using (SolidBrush brush = new SolidBrush(backgroundColor))
            {
                g.FillRectangle(brush, ClientRectangle);
            }

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

    public class VaultEntry
    {
        public string Platform { get; set; } = "";
        public string Username { get; set; } = "";
        public string Secret { get; set; } = "";
        public string Website { get; set; } = "";
        public string Note { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public bool IsFavorite { get; set; } = false;

        public string GetDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(Platform))
            {
                return Platform;
            }

            if (!string.IsNullOrWhiteSpace(Username))
            {
                return Username;
            }

            if (!string.IsNullOrWhiteSpace(Note))
            {
                return Note.Length > 24 ? Note.Substring(0, 24) + "..." : Note;
            }

            if (!string.IsNullOrWhiteSpace(Secret))
            {
                return "Secret entry";
            }

            return "Untitled entry";
        }
    }
}









