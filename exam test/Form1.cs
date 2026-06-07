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
using System.Text;

namespace exam_test
{
    public partial class Form1 : Form
    {
        private const string AppName = "QuickForge Sync";
        private const string AppStatus = "Beta Preview";
        private const string AppVersion = "v0.2.0-beta-preview";
        private const string AppDisplayName = AppName + " " + AppStatus;


        private readonly System.Windows.Forms.Timer animationTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer hideRevealTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer autoLockTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer unlockStatusTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer autoRefreshTimer = new System.Windows.Forms.Timer();

        private float time = 0f;

        private const int TreeDepth = 9;
        private const float BaseLength = 120f;
        private const float BaseSpread = 28f;
        private const float BranchMovement = 9f;

        private string vaultCode = "";
        private readonly List<VaultEntry> vaultEntries = new List<VaultEntry>();
        private DriveService? currentDriveService;
        private bool cloudVaultExists = false;
        private string connectedGoogleEmail = "";
        private byte[]? currentDataKey;
        private EncryptedVaultFile? currentEncryptedVaultFile;
        private bool isVaultUnlocked = false;
        private const int SecretAccessMinutes = 10;
        private DateTime secretAccessValidUntilUtc = DateTime.MinValue;
        private VaultSettings currentVaultSettings = new VaultSettings();
        private bool hasShownRecoveryReminderThisSession = false;
        private bool isVaultUnlockAttemptRunning = false;
        private DateTime vaultUnlockSubmitBlockedUntilUtc = DateTime.MinValue;

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

        private readonly Label vaultCodeStrengthLabel = new Label();
        private readonly Panel vaultCodeStrengthTrack = new Panel();
        private readonly Panel vaultCodeStrengthFill = new Panel();
        private readonly Label confirmVaultCodeLabel = new Label();
        private readonly TextBox confirmVaultCodeTextBox = new TextBox();
        private readonly Label vaultUnlockStatusLabel = new Label();
        private Button? vaultCodeVisibilityButton;
        private Button? confirmVaultCodeVisibilityButton;
        private Button? secretVisibilityButton;
        private readonly Button createVaultButton = new Button();
        private readonly Button resetTestVaultButton = new Button();
        private readonly Button importBackupAccessButton = new Button();

        // Vault workspace
        private readonly Panel vaultPanel = new Panel();
        private readonly Label vaultTitleLabel = new Label();
        private readonly Label vaultSubtitleLabel = new Label();
        private readonly Label syncStatusLabel = new Label();
        private readonly Label restrictedModeBannerLabel = new Label();
        private DateTime? lastCloudSaveUtc = null;
        private DateTime? lastCloudLoadUtc = null;
        private string? lastKnownCloudFingerprint = null;

        private bool backgroundVaultSyncRunning = false;
        private bool backgroundVaultSyncRequested = false;
        private string backgroundVaultSyncReason = "";
        private bool hasUnsyncedLocalChanges = false;
        private bool autoRefreshRunning = false;

        private const int MaxBackgroundSyncRetries = 5;
        private const int BackgroundSyncRetryDelaySeconds = 10;
        private int backgroundVaultSyncRetryCount = 0;

        private string localDeviceId = "";
        private string localDeviceName = "";
        private bool newDeviceDetectedThisSession = false;
        private string newDeviceDetectedName = "";
        private bool untrustedDeviceDetectedThisSession = false;
        private string untrustedDeviceDetectedName = "";
        private bool restrictedModeWarningShownThisSession = false;
        private const int MaxSafetyTimelineEvents = 25;
        private const int MaxDeletedEntryTombstones = 250;

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
        private readonly Button refreshCloudButton = new Button();
        private readonly Button manualSyncButton = new Button();
        private readonly Button securityCenterButton = new Button();
        private readonly Label securitySettingsLabel = new Label();
        private readonly Label recoveryReminderLabel = new Label();
        private readonly ComboBox recoveryReminderComboBox = new ComboBox();
        private readonly Button rotateRecoveryKeyButton = new Button();

        private readonly Label performanceSettingsLabel = new Label();
        private readonly CheckBox animationEnabledCheckBox = new CheckBox();
        private readonly Label autoLockLabel = new Label();
        private readonly ComboBox autoLockComboBox = new ComboBox();
        private readonly Label autoRefreshLabel = new Label();
        private readonly ComboBox autoRefreshComboBox = new ComboBox();

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
            ClientSize = new Size(800, 790);
            Text = AppDisplayName + " " + AppVersion;
            MinimumSize = new Size(800, 790);
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

            animationTimer.Interval = 33;
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();

            hideRevealTimer.Interval = 15000;
            hideRevealTimer.Tick += HideRevealTimer_Tick;

            autoLockTimer.Interval = 30000;
            autoLockTimer.Tick += AutoLockTimer_Tick;
            autoLockTimer.Start();

            autoRefreshTimer.Tick += AutoRefreshTimer_Tick;
        }


        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!ConfirmPendingBackgroundSyncBeforeExit("close QuickForge"))
            {
                e.Cancel = true;
                return;
            }

            base.OnFormClosing(e);
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

            appTitleLabel.Text = AppName;
            appTitleLabel.ForeColor = Color.White;
            appTitleLabel.BackColor = Color.Transparent;
            appTitleLabel.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            appTitleLabel.AutoSize = true;
            appTitleLabel.Left = 18;
            appTitleLabel.Top = 13;

            appSubtitleLabel.Text = AppStatus + " " + AppVersion + " - encrypted cloud vault for controlled personal beta use.";
            appSubtitleLabel.ForeColor = softTextColor;
            appSubtitleLabel.BackColor = Color.Transparent;
            appSubtitleLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            appSubtitleLabel.AutoSize = false;
            appSubtitleLabel.Left = 18;
            appSubtitleLabel.Top = 42;
            appSubtitleLabel.Width = 410;
            appSubtitleLabel.Height = 22;

            accountStatusLabel.Text = "Not connected";
            accountStatusLabel.ForeColor = softTextColor;
            accountStatusLabel.BackColor = Color.Transparent;
            accountStatusLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            accountStatusLabel.AutoSize = false;
            accountStatusLabel.TextAlign = ContentAlignment.MiddleRight;
            accountStatusLabel.Left = 440;
            accountStatusLabel.Top = 52;
            accountStatusLabel.Width = 305;
            accountStatusLabel.Height = 22;

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
            topBarPanel.Controls.Add(aboutButton);
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
                "QuickForge encrypts vault data before syncing. Keep your vault code and recovery key safe." + Environment.NewLine + Environment.NewLine +
                "Tested:" + Environment.NewLine +
                "- Public release ZIP works" + Environment.NewLine +
                "- Google login works on separate accounts" + Environment.NewLine +
                "- Vault data is isolated per Google account" + Environment.NewLine +
                "- 19 automated crypto/backup tests pass",
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
                "- Your vault is encrypted before sync" + Environment.NewLine +
                "- Each Google account has its own isolated vault" + Environment.NewLine +
                "- Controlled personal beta use is supported" + Environment.NewLine +
                "- Save your recovery key safely";
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
            betaWarningLabel.Text = "Beta Preview: encrypted cloud vault for controlled personal beta use.";
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

            googleSubtitleLabel.Text = "Each Google account has its own encrypted vault";
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
            vaultAccessPanel.Height = 455;
            vaultAccessPanel.BackColor = Color.FromArgb(16, 20, 34);

            unlockStatusTimer.Interval = 1000;
            unlockStatusTimer.Tick += (s, e) =>
            {
                if (vaultAccessPanel.Visible && cloudVaultExists)
                {
                    UpdateVaultUnlockStatusLabel();
                }
            };

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
            vaultAccessSubtitleLabel.AutoSize = false;
            vaultAccessSubtitleLabel.Left = 24;
            vaultAccessSubtitleLabel.Top = 55;
            vaultAccessSubtitleLabel.Width = 390;
            vaultAccessSubtitleLabel.Height = 36;

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
            vaultCodeTextBox.TextChanged += (s, e) => UpdateVaultCodeStrengthPreview();

            vaultCodeStrengthLabel.Text = "Vault code strength: Not checked yet";
            vaultCodeStrengthLabel.Left = 24;
            vaultCodeStrengthLabel.Top = 142;
            vaultCodeStrengthLabel.Width = 390;
            vaultCodeStrengthLabel.Height = 32;
            vaultCodeStrengthLabel.ForeColor = softTextColor;
            vaultCodeStrengthLabel.BackColor = Color.Transparent;
            vaultCodeStrengthLabel.Font = new Font("Segoe UI", 8, FontStyle.Bold);

            vaultCodeStrengthTrack.Left = 24;
            vaultCodeStrengthTrack.Top = 174;
            vaultCodeStrengthTrack.Width = 390;
            vaultCodeStrengthTrack.Height = 7;
            vaultCodeStrengthTrack.BackColor = Color.FromArgb(35, 40, 60);

            vaultCodeStrengthFill.Left = 0;
            vaultCodeStrengthFill.Top = 0;
            vaultCodeStrengthFill.Width = 0;
            vaultCodeStrengthFill.Height = 7;
            vaultCodeStrengthFill.BackColor = softTextColor;

            vaultCodeStrengthTrack.Controls.Add(vaultCodeStrengthFill);

            confirmVaultCodeLabel.Text = "Confirm vault code";
            confirmVaultCodeLabel.ForeColor = Color.White;
            confirmVaultCodeLabel.BackColor = Color.Transparent;
            confirmVaultCodeLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            confirmVaultCodeLabel.AutoSize = true;
            confirmVaultCodeLabel.Left = 24;
            confirmVaultCodeLabel.Top = 195;

            confirmVaultCodeTextBox.Left = 24;
            confirmVaultCodeTextBox.Top = 217;
            confirmVaultCodeTextBox.Width = 390;
            confirmVaultCodeTextBox.Height = 26;
            confirmVaultCodeTextBox.UseSystemPasswordChar = true;
            confirmVaultCodeTextBox.PlaceholderText = "Repeat vault code";

            vaultUnlockStatusLabel.Text = "";
            vaultUnlockStatusLabel.Left = 24;
            vaultUnlockStatusLabel.Top = 252;
            vaultUnlockStatusLabel.Width = 400;
            vaultUnlockStatusLabel.Height = 130;
            vaultUnlockStatusLabel.ForeColor = softTextColor;
            vaultUnlockStatusLabel.BackColor = Color.FromArgb(20, 25, 42);
            vaultUnlockStatusLabel.BorderStyle = BorderStyle.FixedSingle;
            vaultUnlockStatusLabel.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            vaultUnlockStatusLabel.Padding = new Padding(8, 6, 8, 6);
            vaultUnlockStatusLabel.Visible = false;

            createVaultButton.Text = "Unlock Vault";
            createVaultButton.Left = 24;
            createVaultButton.Top = 305;
            createVaultButton.Width = 120;
            createVaultButton.Height = 32;
            createVaultButton.FlatStyle = FlatStyle.Flat;
            createVaultButton.ForeColor = Color.White;
            createVaultButton.BackColor = Color.FromArgb(45, 90, 160);
            createVaultButton.FlatAppearance.BorderColor = borderColor;
            createVaultButton.Click += CreateVaultButton_Click;

            resetTestVaultButton.Text = "Reset Test Vault";
            resetTestVaultButton.Left = 155;
            resetTestVaultButton.Top = 305;
            resetTestVaultButton.Width = 150;
            resetTestVaultButton.Height = 32;
            resetTestVaultButton.FlatStyle = FlatStyle.Flat;
            resetTestVaultButton.ForeColor = Color.White;
            resetTestVaultButton.BackColor = Color.FromArgb(120, 35, 45);
            resetTestVaultButton.FlatAppearance.BorderColor = Color.FromArgb(190, 80, 90);
            resetTestVaultButton.Visible = false;
            resetTestVaultButton.Click += ResetTestVaultButton_Click;

            importBackupAccessButton.Text = "Import Backup";
            importBackupAccessButton.Left = 315;
            importBackupAccessButton.Top = 305;
            importBackupAccessButton.Width = 110;
            importBackupAccessButton.Height = 32;
            importBackupAccessButton.FlatStyle = FlatStyle.Flat;
            importBackupAccessButton.ForeColor = Color.White;
            importBackupAccessButton.BackColor = Color.FromArgb(35, 40, 60);
            importBackupAccessButton.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);
            importBackupAccessButton.Click += async (s, e) => await ImportEncryptedBackupAsync();

            vaultAccessPanel.Controls.Add(vaultAccessTitleLabel);
            vaultAccessPanel.Controls.Add(vaultAccessSubtitleLabel);
            vaultAccessPanel.Controls.Add(vaultCodeLabel);
            vaultAccessPanel.Controls.Add(vaultCodeTextBox);
            vaultAccessPanel.Controls.Add(vaultCodeStrengthLabel);
            vaultAccessPanel.Controls.Add(vaultCodeStrengthTrack);
            vaultAccessPanel.Controls.Add(confirmVaultCodeLabel);
            vaultAccessPanel.Controls.Add(confirmVaultCodeTextBox);
            vaultAccessPanel.Controls.Add(vaultUnlockStatusLabel);
            vaultCodeVisibilityButton = AttachPasswordVisibilityToggle(vaultAccessPanel, vaultCodeTextBox);
            confirmVaultCodeVisibilityButton = AttachPasswordVisibilityToggle(vaultAccessPanel, confirmVaultCodeTextBox);
            vaultAccessPanel.Controls.Add(createVaultButton);
            vaultAccessPanel.Controls.Add(resetTestVaultButton);
            vaultAccessPanel.Controls.Add(importBackupAccessButton);

            Controls.Add(vaultAccessPanel);
            vaultAccessPanel.BringToFront();
        }

        private void CreateVaultUi()
        {
            vaultPanel.Left = 70;
            vaultPanel.Top = 120;
            vaultPanel.Width = 660;
            vaultPanel.Height = 620;
            vaultPanel.BackColor = Color.FromArgb(16, 20, 34);

            vaultTitleLabel.Text = "Encrypted Vault";
            vaultTitleLabel.ForeColor = Color.White;
            vaultTitleLabel.BackColor = Color.Transparent;
            vaultTitleLabel.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            vaultTitleLabel.AutoSize = true;
            vaultTitleLabel.Left = 20;
            vaultTitleLabel.Top = 16;

            vaultSubtitleLabel.Text = "Save accounts, codes, notes and private snippets.";
            vaultSubtitleLabel.ForeColor = softTextColor;
            vaultSubtitleLabel.BackColor = Color.Transparent;
            vaultSubtitleLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            vaultSubtitleLabel.AutoSize = true;
            vaultSubtitleLabel.Left = 20;
            vaultSubtitleLabel.Top = 48;

            syncStatusLabel.Text =
                "Sync: Not connected" + Environment.NewLine +
                "Last save: Not yet" + Environment.NewLine +
                "Last load: Not yet";
            syncStatusLabel.ForeColor = softTextColor;
            syncStatusLabel.BackColor = Color.Transparent;
            syncStatusLabel.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            syncStatusLabel.Left = 430;
            syncStatusLabel.Top = 16;
            syncStatusLabel.Width = 220;
            syncStatusLabel.Height = 60;
            syncStatusLabel.TextAlign = ContentAlignment.TopRight;

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
            vaultPanel.Controls.Add(syncStatusLabel);
            vaultPanel.Controls.Add(restrictedModeBannerLabel);
            vaultPanel.Controls.Add(platformLabel);
            vaultPanel.Controls.Add(platformTextBox);
            vaultPanel.Controls.Add(usernameLabel);
            vaultPanel.Controls.Add(usernameTextBox);

            vaultPanel.Controls.Add(secretLabel);
            vaultPanel.Controls.Add(secretTextBox);
            secretVisibilityButton = AttachPasswordVisibilityToggle(vaultPanel, secretTextBox);
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

          
            favoriteButton.Text = "\u2606 Favorite";
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
            securityCenterButton.Top = 570;
            securityCenterButton.Width = 105;
            securityCenterButton.Height = 30;
            securityCenterButton.FlatStyle = FlatStyle.Flat;
            securityCenterButton.ForeColor = Color.White;
            securityCenterButton.BackColor = Color.FromArgb(45, 90, 160);
            securityCenterButton.FlatAppearance.BorderColor = borderColor;
            securityCenterButton.Click += (s, e) => ShowSecurityCenterDialog();

            backupButton.Text = "Backup";
            backupButton.Left = 430;
            backupButton.Top = 570;
            backupButton.Width = 70;
            backupButton.Height = 30;
            backupButton.FlatStyle = FlatStyle.Flat;
            backupButton.ForeColor = Color.White;
            backupButton.BackColor = Color.FromArgb(35, 40, 60);
            backupButton.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);
            backupButton.Click += (s, e) =>
            {
                if (!RequireTrustedDeviceForSensitiveAction("Export encrypted backup"))
                {
                    return;
                }

                ShowBackupDialog();
            };

            refreshCloudButton.Text = "Refresh";
            refreshCloudButton.Left = 505;
            refreshCloudButton.Top = 570;
            refreshCloudButton.Width = 75;
            refreshCloudButton.Height = 30;
            refreshCloudButton.FlatStyle = FlatStyle.Flat;
            refreshCloudButton.ForeColor = Color.White;
            refreshCloudButton.BackColor = Color.FromArgb(35, 40, 60);
            refreshCloudButton.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);
            refreshCloudButton.Click += RefreshCloudButton_Click;

            manualSyncButton.Text = "Sync";
            manualSyncButton.Left = 585;
            manualSyncButton.Top = 570;
            manualSyncButton.Width = 65;
            manualSyncButton.Height = 30;
            manualSyncButton.FlatStyle = FlatStyle.Flat;
            manualSyncButton.ForeColor = Color.White;
            manualSyncButton.BackColor = Color.FromArgb(45, 90, 160);
            manualSyncButton.FlatAppearance.BorderColor = borderColor;
            manualSyncButton.Click += ManualSyncButton_Click;

            vaultPanel.Controls.Add(securitySettingsLabel);
            vaultPanel.Controls.Add(recoveryReminderLabel);
            vaultPanel.Controls.Add(recoveryReminderComboBox);
            vaultPanel.Controls.Add(rotateRecoveryKeyButton);
            vaultPanel.Controls.Add(securityCenterButton);
            vaultPanel.Controls.Add(backupButton);
            vaultPanel.Controls.Add(refreshCloudButton);
            vaultPanel.Controls.Add(manualSyncButton);

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

            autoRefreshLabel.Text = "Auto-refresh:";
            autoRefreshLabel.Left = 20;
            autoRefreshLabel.Top = 545;
            autoRefreshLabel.Width = 100;
            autoRefreshLabel.Height = 24;
            autoRefreshLabel.ForeColor = softTextColor;
            autoRefreshLabel.BackColor = Color.Transparent;
            autoRefreshLabel.Font = new Font("Segoe UI", 8, FontStyle.Regular);

            autoRefreshComboBox.Left = 125;
            autoRefreshComboBox.Top = 542;
            autoRefreshComboBox.Width = 165;
            autoRefreshComboBox.Height = 28;
            autoRefreshComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            autoRefreshComboBox.Items.Add("Never");
            autoRefreshComboBox.Items.Add("Every 1 minute");
            autoRefreshComboBox.Items.Add("Every 5 minutes");
            autoRefreshComboBox.Items.Add("Every 15 minutes");
            autoRefreshComboBox.Items.Add("Every 30 minutes");
            autoRefreshComboBox.SelectedIndex = 2;
            autoRefreshComboBox.SelectionChangeCommitted += async (s, e) =>
            {
                if (!RequireTrustedDeviceForSensitiveAction("Change auto-refresh setting"))
                {
                    ApplyPerformanceSettingsToUi();
                    return;
                }

                currentVaultSettings.AutoRefreshMinutes = GetAutoRefreshMinutesFromSelection();
                ConfigureAutoRefreshTimer();
                MarkVaultActivity();

                if (isVaultUnlocked)
                {
                    try
                    {
                        await SaveCurrentVaultToCloudAsync();
                        selectedPreviewLabel.Text = "Auto-refresh setting saved.";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not save auto-refresh setting: " + ex.Message);
                    }
                }
            };
            vaultPanel.Controls.Add(performanceSettingsLabel);
            vaultPanel.Controls.Add(animationEnabledCheckBox);
            vaultPanel.Controls.Add(autoLockLabel);
            vaultPanel.Controls.Add(autoLockComboBox);
            vaultPanel.Controls.Add(autoRefreshLabel);
            vaultPanel.Controls.Add(autoRefreshComboBox);

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
                connectedGoogleEmail = email;

                accountStatusLabel.Text = "Connected: " + email;
                accountStatusLabel.ForeColor = successColor;
                logoutButton.Enabled = true;

                cloudVaultExists = await GoogleDriveVaultService.VaultExistsAsync(currentDriveService);

                lastCloudSaveUtc = null;
                lastCloudLoadUtc = null;
                SetSyncStatus(currentDriveService != null ? "Active" : "Not connected");

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
        private string GetConnectedGoogleEmailDisplay()
        {
            if (string.IsNullOrWhiteSpace(connectedGoogleEmail))
            {
                return "this Google account";
            }

            return connectedGoogleEmail;
        }

        private void UpdateVaultUnlockStatusLabel()
        {
            if (!cloudVaultExists)
            {
                vaultUnlockStatusLabel.Visible = false;
                return;
            }

            VaultUnlockAttemptState state =
                VaultUnlockAttemptService.LoadState(GetVaultUnlockAttemptAccountId());

            vaultUnlockStatusLabel.Visible = true;

            if (VaultUnlockAttemptService.IsLockedOut(state, DateTime.UtcNow))
            {
                vaultUnlockStatusLabel.Text =
                    "Vault-code locked for about " + FormatRemainingLockoutTime(state.LockedUntilUtc) + "." + Environment.NewLine +
                    "Recovery key can bypass the waiting time." + Environment.NewLine +
                    "If the cloud vault is damaged, use Import Backup.";

                vaultUnlockStatusLabel.ForeColor = Color.FromArgb(255, 190, 90);
                return;
            }

            int remainingAttempts = VaultUnlockAttemptService.RemainingAttempts(state);

            if (state.FailedAttempts > 0)
            {
                vaultUnlockStatusLabel.Text =
                    "Wrong vault code or recovery key." + Environment.NewLine +
                    "Attempts left before lockout: " + remainingAttempts + Environment.NewLine +
                    "Recovery key can unlock if this should have worked." + Environment.NewLine +
                    "If the cloud vault is damaged, use Import Backup.";

                vaultUnlockStatusLabel.ForeColor = remainingAttempts <= 1
                    ? Color.FromArgb(255, 190, 90)
                    : softTextColor;

                return;
            }

            vaultUnlockStatusLabel.Text =
                "Vault-code attempts left: " + remainingAttempts + Environment.NewLine +
                "Recovery key can unlock if you forgot your vault password.";

            vaultUnlockStatusLabel.ForeColor = softTextColor;
        }
        private void ConfigureVaultAccessForCreate()
        {
            unlockStatusTimer.Stop();

            vaultAccessTitleLabel.Text = "Create Vault Code";
            vaultAccessSubtitleLabel.Text =
                "First-time setup for " + GetConnectedGoogleEmailDisplay() + "." + Environment.NewLine +
                "This vault is tied to this Google account.";
            vaultCodeLabel.Text = "Create vault code";
            vaultCodeTextBox.PlaceholderText = "Create a vault code";

            confirmVaultCodeLabel.Visible = true;
            confirmVaultCodeTextBox.Visible = true;

            if (vaultCodeVisibilityButton != null)
            {
                vaultCodeVisibilityButton.Visible = true;
            }

            if (confirmVaultCodeVisibilityButton != null)
            {
                confirmVaultCodeVisibilityButton.Visible = true;
            }

            vaultUnlockStatusLabel.Visible = false;
            SetVaultAccessButtonRowTop(255);
            createVaultButton.Text = "Create Vault";

            vaultCodeTextBox.Clear();
            confirmVaultCodeTextBox.Clear();

            vaultCodeStrengthLabel.Visible = true;
            vaultCodeStrengthTrack.Visible = true;
            UpdateVaultCodeStrengthPreview();
        }
        private void ConfigureVaultAccessForUnlock()
        {
            vaultAccessTitleLabel.Text = "Unlock Vault";
            vaultAccessSubtitleLabel.Text =
                "Vault for " + GetConnectedGoogleEmailDisplay() + "." + Environment.NewLine +
                "Enter your vault code or recovery key.";
            vaultCodeLabel.Text = "Vault code / recovery key";
            vaultCodeTextBox.PlaceholderText = "Enter vault code or recovery key";

            confirmVaultCodeLabel.Visible = false;
            confirmVaultCodeTextBox.Visible = false;

            if (vaultCodeVisibilityButton != null)
            {
                vaultCodeVisibilityButton.Visible = true;
            }

            if (confirmVaultCodeVisibilityButton != null)
            {
                confirmVaultCodeVisibilityButton.Visible = false;
            }

            SetVaultAccessButtonRowTop(390);
            UpdateVaultUnlockStatusLabel();
            unlockStatusTimer.Start();
            createVaultButton.Text = "Unlock Vault";

            vaultCodeTextBox.Clear();
            confirmVaultCodeTextBox.Clear();

            vaultCodeStrengthLabel.Visible = false;
            vaultCodeStrengthTrack.Visible = false;
        }
        private void UpdateDeveloperTestControls()
        {
#if DEBUG
            resetTestVaultButton.Visible =
                string.Equals(
                    connectedGoogleEmail,
                    "patrickolsen4@gmail.com",
                    StringComparison.OrdinalIgnoreCase
                );
#else
            resetTestVaultButton.Visible = false;
#endif
        }
        private void UpdateVaultCodeStrengthPreview()
        {
            string code = vaultCodeTextBox.Text;

            if (string.IsNullOrWhiteSpace(code))
            {
                vaultCodeStrengthLabel.Text = "Vault code strength: Not checked yet";
                vaultCodeStrengthLabel.ForeColor = softTextColor;
                vaultCodeStrengthFill.Width = 0;
                vaultCodeStrengthFill.BackColor = softTextColor;
                return;
            }

            int score = 0;

            if (code.Length >= 12) score += 25;
            if (code.Length >= 16) score += 10;
            if (code.Any(char.IsLower)) score += 10;
            if (code.Any(char.IsUpper)) score += 15;
            if (code.Any(char.IsDigit)) score += 15;
            if (code.Any(ch => !char.IsLetterOrDigit(ch))) score += 20;
            if (code.Distinct().Count() >= Math.Min(8, code.Length)) score += 5;

            score = Math.Max(0, Math.Min(100, score));
            vaultCodeStrengthFill.Width = (int)(vaultCodeStrengthTrack.Width * (score / 100.0));

            if (!VaultCodePolicy.IsStrongEnough(code, out string warning))
            {
                vaultCodeStrengthLabel.Text = "Vault code strength: Weak - " + warning;
                vaultCodeStrengthLabel.ForeColor = dangerColor;
                vaultCodeStrengthFill.BackColor = dangerColor;
                return;
            }

            if (score >= 85)
            {
                vaultCodeStrengthLabel.Text = "Vault code strength: Strong";
                vaultCodeStrengthLabel.ForeColor = successColor;
                vaultCodeStrengthFill.BackColor = successColor;
            }
            else
            {
                vaultCodeStrengthLabel.Text = "Vault code strength: Good - add more length/symbols for stronger protection";
                vaultCodeStrengthLabel.ForeColor = Color.FromArgb(255, 190, 90);
                vaultCodeStrengthFill.BackColor = Color.FromArgb(255, 190, 90);
            }
        }

        private void ShowVaultCodeStrengthMessage(string reason)
        {
            MessageBox.Show(
                "Your vault code is too weak." + Environment.NewLine + Environment.NewLine +
                reason + Environment.NewLine + Environment.NewLine +
                "Use at least 12 characters and mix words, numbers, and symbols." + Environment.NewLine +
                "Example style: River-Forge-72#Moon",
                "Weak vault code",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
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
                if (!VaultCodePolicy.IsStrongEnough(code, out string vaultCodeWarning))
                {
                    ShowVaultCodeStrengthMessage(vaultCodeWarning);
                    return;
                }

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
                    ShowEmergencyBackupGuidance();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not create encrypted vault: " + ex.Message);
                }

                return;
            }

            try
            {
                bool unlocked = await TryLoadVaultFromCloudWithLockoutAsync(code);

                if (!unlocked)
                {
                    return;
                }

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

                await ShowVaultOpenRecoveryMessageAsync();
            }
        }

        private async Task ShowVaultOpenRecoveryMessageAsync()
        {
            SetPreviewText(
                "Could not unlock vault.",
                "Check your vault code/recovery key, or import an encrypted backup.",
                "If the cloud vault is corrupted, a valid backup can replace it."
            );

            DialogResult result = MessageBox.Show(
                "QuickForge could not unlock this vault." + Environment.NewLine + Environment.NewLine +
                "Possible reasons:" + Environment.NewLine +
                "- Wrong vault code or recovery key" + Environment.NewLine +
                "- The cloud vault file is damaged or corrupted" + Environment.NewLine + Environment.NewLine +
                "If you have an encrypted backup, you can import it and replace the damaged cloud vault." + Environment.NewLine + Environment.NewLine +
                "Do you want to import an encrypted backup now?",
                "Could not unlock vault",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                await ImportEncryptedBackupAsync();
            }
        }

        private string GetVaultUnlockAttemptAccountId()
        {
            return string.IsNullOrWhiteSpace(connectedGoogleEmail)
                ? "unknown-google-account"
                : connectedGoogleEmail;
        }

        private string FormatRemainingLockoutTime(DateTime lockedUntilUtc)
        {
            TimeSpan remaining = lockedUntilUtc - DateTime.UtcNow;

            if (remaining <= TimeSpan.Zero)
            {
                return "less than a minute";
            }

            if (remaining.TotalHours >= 1)
            {
                int minutes = Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes));
                return minutes + " minute(s)";
            }

            return Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes)) + " minute(s)";
        }

        private void ShowVaultCodeLockoutMessage(VaultUnlockAttemptState state)
        {
            MessageBox.Show(
                "Vault-code unlock is temporarily locked." + Environment.NewLine + Environment.NewLine +
                "Try again in about " + FormatRemainingLockoutTime(state.LockedUntilUtc) + "." + Environment.NewLine + Environment.NewLine +
                "If this is your vault, you can still unlock immediately with your recovery key." + Environment.NewLine + Environment.NewLine +
                "If the cloud vault is damaged, use Import encrypted backup.",
                "Vault code locked",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }


        private void SetVaultAccessButtonRowTop(int top)
        {
            createVaultButton.Top = top;
            resetTestVaultButton.Top = top;
            importBackupAccessButton.Top = top;
        }

        private bool TryBeginVaultUnlockSubmit()
        {
            if (isVaultUnlockAttemptRunning)
            {
                vaultUnlockStatusLabel.Visible = true;
                vaultUnlockStatusLabel.Text =
                    "Unlock already in progress." +
                    Environment.NewLine +
                    "Please wait for the current check to finish." +
                    Environment.NewLine +
                    "This prevents accidental double-submit.";

                vaultUnlockStatusLabel.ForeColor = softTextColor;
                return false;
            }

            TimeSpan remainingDelay = vaultUnlockSubmitBlockedUntilUtc - DateTime.UtcNow;

            if (remainingDelay > TimeSpan.Zero)
            {
                vaultUnlockStatusLabel.Visible = true;
                vaultUnlockStatusLabel.Text =
                    "Please wait a moment before trying again." +
                    Environment.NewLine +
                    "This prevents accidental double-submit.";

                vaultUnlockStatusLabel.ForeColor = softTextColor;
                return false;
            }

            isVaultUnlockAttemptRunning = true;
            vaultUnlockSubmitBlockedUntilUtc = DateTime.UtcNow.AddSeconds(1);

            createVaultButton.Enabled = false;
            vaultCodeTextBox.Enabled = false;

            return true;
        }

        private async Task EndVaultUnlockSubmitGuardAsync()
        {
            TimeSpan remainingDelay = vaultUnlockSubmitBlockedUntilUtc - DateTime.UtcNow;

            if (remainingDelay > TimeSpan.Zero)
            {
                await Task.Delay(remainingDelay);
            }

            isVaultUnlockAttemptRunning = false;
            createVaultButton.Enabled = true;
            vaultCodeTextBox.Enabled = true;
            vaultCodeTextBox.Focus();
        }

        private void ClearVaultCodeInputForRetry()
        {
            vaultCodeTextBox.Clear();
            confirmVaultCodeTextBox.Clear();
            vaultCodeTextBox.Focus();
        }

        private void ShowInlineVaultUnlockFailure(int remainingAttempts)
        {
            vaultUnlockStatusLabel.Visible = true;
            vaultUnlockStatusLabel.Text =
                "Wrong vault code or recovery key." +
                Environment.NewLine +
                "Attempts left before lockout: " + remainingAttempts +
                Environment.NewLine +
                "Recovery key can unlock if this should have worked." +
                Environment.NewLine +
                "If the cloud vault is damaged, use Import Backup.";

            vaultUnlockStatusLabel.ForeColor = remainingAttempts <= 1
                ? Color.FromArgb(255, 190, 90)
                : softTextColor;

            SetPreviewText(
                "Could not unlock vault.",
                "Wrong vault code or recovery key. Attempts left: " + remainingAttempts,
                "Try your recovery key, or use Import Backup if the cloud vault is damaged."
            );
        }
        private void RecordFailedVaultUnlockAttempt()
        {
            string accountId = GetVaultUnlockAttemptAccountId();
            VaultUnlockAttemptState state = VaultUnlockAttemptService.LoadState(accountId);

            state = VaultUnlockAttemptService.RecordFailure(state, DateTime.UtcNow);
            VaultUnlockAttemptService.SaveState(accountId, state);

            UpdateVaultUnlockStatusLabel();
            ClearVaultCodeInputForRetry();

            if (VaultUnlockAttemptService.IsLockedOut(state, DateTime.UtcNow))
            {
                SetSyncStatus("Vault code locked", error: true);
                ShowVaultCodeLockoutMessage(state);
                return;
            }

            int remainingAttempts = VaultUnlockAttemptService.RemainingAttempts(state);

            SetSyncStatus("Unlock failed", error: true);
            ShowInlineVaultUnlockFailure(remainingAttempts);
        }

        private void ResetVaultUnlockAttemptState()
        {
            VaultUnlockAttemptService.ResetAfterSuccessfulUnlock(GetVaultUnlockAttemptAccountId());
            UpdateVaultUnlockStatusLabel();
        }

        private async Task<bool> ForceRecoveryKeyRotationAfterRecoveryUnlockAsync()
        {
            MessageBox.Show(
                "You unlocked with your recovery key." + Environment.NewLine + Environment.NewLine +
                "For safety, QuickForge must now rotate your recovery key." + Environment.NewLine +
                "The old recovery key will stop working after the new one is saved.",
                "Recovery key rotation required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            bool rotated = await RotateRecoveryKeyAsync();

            if (!rotated)
            {
                MessageBox.Show(
                    "Recovery key rotation is required before opening the vault." + Environment.NewLine + Environment.NewLine +
                    "Your vault was not opened. Try again and complete recovery key rotation.",
                    "Recovery key rotation required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return false;
            }

            return true;
        }

        private async Task<bool> TryLoadVaultFromCloudWithLockoutAsync(string unlockCode)
        {
            if (currentDriveService == null)
            {
                throw new InvalidOperationException("Google Drive is not connected.");
            }

            SetSyncStatus("Loading from Google Drive...");
            SetPreviewText(
                "Checking unlock code...",
                "QuickForge uses strong encryption checks, so this can take a moment.",
                "Please wait."
            );

            await Task.Yield();

            GoogleDriveVaultMetadata? cloudMetadata =
                await GoogleDriveVaultService.GetVaultMetadataAsync(currentDriveService);

            string? encryptedJson =
                await GoogleDriveVaultService.DownloadEncryptedVaultAsync(currentDriveService);

            if (string.IsNullOrWhiteSpace(encryptedJson))
            {
                throw new InvalidOperationException("No encrypted vault was found.");
            }

            VaultUnlockAttemptState attemptState =
                VaultUnlockAttemptService.LoadState(GetVaultUnlockAttemptAccountId());

            bool isLockedOut =
                VaultUnlockAttemptService.IsLockedOut(attemptState, DateTime.UtcNow);

            bool usedRecoveryKey = false;
            VaultData? vaultData = null;
            byte[]? dataKey = null;
            EncryptedVaultFile? decryptedEncryptedVaultFile = null;

            if (isLockedOut)
            {
                SetSyncStatus("Vault code locked", error: true);
                SetPreviewText(
                    "Vault-code unlock is temporarily locked.",
                    "Checking whether this is your recovery key...",
                    "Recovery key can bypass the waiting time."
                );

                await Task.Yield();

                var recoveryAttempt = await Task.Run(() =>
                {
                    bool success = VaultCryptoService.TryDecryptVaultWithRecoveryKey(
                        encryptedJson,
                        unlockCode,
                        out VaultData? loadedVault,
                        out byte[]? loadedDataKey,
                        out EncryptedVaultFile? loadedEncryptedVaultFile
                    );

                    return (
                        Success: success,
                        Vault: loadedVault,
                        DataKey: loadedDataKey,
                        EncryptedFile: loadedEncryptedVaultFile
                    );
                });

                if (!recoveryAttempt.Success ||
                    recoveryAttempt.Vault == null ||
                    recoveryAttempt.DataKey == null ||
                    recoveryAttempt.EncryptedFile == null)
                {
                    UpdateVaultUnlockStatusLabel();
                    ClearVaultCodeInputForRetry();
                    ShowVaultCodeLockoutMessage(attemptState);
                    return false;
                }

                vaultData = recoveryAttempt.Vault;
                dataKey = recoveryAttempt.DataKey;
                decryptedEncryptedVaultFile = recoveryAttempt.EncryptedFile;
                usedRecoveryKey = true;
            }
            else
            {
                bool looksLikeRecoveryKey = unlockCode
                    .Trim()
                    .StartsWith("QF-", StringComparison.OrdinalIgnoreCase);

                if (looksLikeRecoveryKey)
                {
                    SetSyncStatus("Checking recovery key...");
                    SetPreviewText(
                        "Checking recovery key...",
                        "Recovery keys start with QF-, so QuickForge skips the slower vault-code check.",
                        "Please wait."
                    );

                    await Task.Yield();

                    var recoveryKeyAttempt = await Task.Run(() =>
                    {
                        bool success = VaultCryptoService.TryDecryptVaultWithRecoveryKey(
                            encryptedJson,
                            unlockCode,
                            out VaultData? loadedVault,
                            out byte[]? loadedDataKey,
                            out EncryptedVaultFile? loadedEncryptedVaultFile
                        );

                        return (
                            Success: success,
                            Vault: loadedVault,
                            DataKey: loadedDataKey,
                            EncryptedFile: loadedEncryptedVaultFile
                        );
                    });

                    if (recoveryKeyAttempt.Success &&
                        recoveryKeyAttempt.Vault != null &&
                        recoveryKeyAttempt.DataKey != null &&
                        recoveryKeyAttempt.EncryptedFile != null)
                    {
                        vaultData = recoveryKeyAttempt.Vault;
                        dataKey = recoveryKeyAttempt.DataKey;
                        decryptedEncryptedVaultFile = recoveryKeyAttempt.EncryptedFile;
                        usedRecoveryKey = true;
                    }
                    else
                    {
                        vaultCode = "";
                        currentDataKey = null;
                        currentEncryptedVaultFile = null;

                        RecordFailedVaultUnlockAttempt();
                        return false;
                    }
                }
                else
                {
                    SetSyncStatus("Checking vault code...");
                    SetPreviewText(
                        "Checking vault code...",
                        "QuickForge is checking your vault code.",
                        "Please wait."
                    );

                    await Task.Yield();

                    var vaultCodeAttempt = await Task.Run(() =>
                    {
                        bool success = VaultCryptoService.TryDecryptVaultWithVaultCode(
                            encryptedJson,
                            unlockCode,
                            out VaultData? loadedVault,
                            out byte[]? loadedDataKey,
                            out EncryptedVaultFile? loadedEncryptedVaultFile
                        );

                        return (
                            Success: success,
                            Vault: loadedVault,
                            DataKey: loadedDataKey,
                            EncryptedFile: loadedEncryptedVaultFile
                        );
                    });

                    if (vaultCodeAttempt.Success &&
                        vaultCodeAttempt.Vault != null &&
                        vaultCodeAttempt.DataKey != null &&
                        vaultCodeAttempt.EncryptedFile != null)
                    {
                        vaultData = vaultCodeAttempt.Vault;
                        dataKey = vaultCodeAttempt.DataKey;
                        decryptedEncryptedVaultFile = vaultCodeAttempt.EncryptedFile;
                    }
                    else
                    {
                        vaultCode = "";
                        currentDataKey = null;
                        currentEncryptedVaultFile = null;

                        RecordFailedVaultUnlockAttempt();
                        return false;
                    }
                }
            }
            if (vaultData == null || dataKey == null || decryptedEncryptedVaultFile == null)
            {
                throw new InvalidOperationException("Vault unlock did not complete.");
            }

            vaultCode = unlockCode;
            currentVaultSettings = vaultData.Settings ?? new VaultSettings();
            currentDataKey = dataKey;
            currentEncryptedVaultFile = decryptedEncryptedVaultFile;

            vaultEntries.Clear();

            foreach (VaultEntry entry in vaultData.Entries)
            {
                vaultEntries.Add(entry);
            }

            RefreshVaultList();
            lastKnownCloudFingerprint = cloudMetadata?.Fingerprint ?? lastKnownCloudFingerprint;
            lastCloudLoadUtc = DateTime.UtcNow;
            SetSyncStatus("Active", success: true);

            ResetVaultUnlockAttemptState();

            if (usedRecoveryKey)
            {
                currentVaultSettings.RecoveryKeyRotationRequired = true;

                SetPreviewText(
                    "Recovery key accepted.",
                    "QuickForge must now rotate your recovery key for safety.",
                    "Complete the rotation to open the vault."
                );

                bool rotated = await ForceRecoveryKeyRotationAfterRecoveryUnlockAsync();

                if (!rotated)
                {
                    vaultCode = "";
                    currentDataKey = null;
                    currentEncryptedVaultFile = null;
                    vaultEntries.Clear();
                    RefreshVaultList();
                    SetSyncStatus("Recovery rotation required", error: true);
                    return false;
                }
            }

            ApplyRecoverySettingsToUi();

            return true;
        }

        private void SetSyncStatus(string status, bool success = false, bool error = false)
        {
            string lastSaveText = lastCloudSaveUtc.HasValue
                ? lastCloudSaveUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                : "Not yet";

            string lastLoadText = lastCloudLoadUtc.HasValue
                ? lastCloudLoadUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                : "Not yet";

            syncStatusLabel.Text =
                "Sync: " + status + Environment.NewLine +
                "Last save: " + lastSaveText + Environment.NewLine +
                "Last load: " + lastLoadText;

            if (error)
            {
                syncStatusLabel.ForeColor = dangerColor;
            }
            else if (success)
            {
                syncStatusLabel.ForeColor = successColor;
            }
            else
            {
                syncStatusLabel.ForeColor = softTextColor;
            }
        }
        private bool IsDeleteSyncReason(string reason)
        {
            return reason.Contains("Deleted locally", StringComparison.OrdinalIgnoreCase) ||
                   reason.Contains("Delete", StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateManualSyncButtonState()
        {
            if (hasUnsyncedLocalChanges)
            {
                manualSyncButton.Text = "Sync pending";
                manualSyncButton.Width = 95;
                manualSyncButton.BackColor = Color.FromArgb(120, 85, 35);
                manualSyncButton.FlatAppearance.BorderColor = Color.FromArgb(255, 190, 90);
            }
            else
            {
                manualSyncButton.Text = "Sync";
                manualSyncButton.Width = 65;
                manualSyncButton.BackColor = Color.FromArgb(45, 90, 160);
                manualSyncButton.FlatAppearance.BorderColor = borderColor;
            }
        }
        private bool IsCloudConflictException(Exception ex)
        {
            return ex.Message.Contains(
                "Cloud vault changed on another device",
                StringComparison.OrdinalIgnoreCase
            );
        }

        private void ShowCloudConflictMessage(Exception ex)
        {
            SetSyncStatus("Conflict detected", error: true);

            SetPreviewText(
                "Cloud vault changed on another device.",
                "QuickForge blocked upload to avoid overwriting newer cloud changes.",
                "Use Refresh first, or export an encrypted backup before replacing anything."
            );

            MessageBox.Show(
                "Cloud vault changed on another device." + Environment.NewLine + Environment.NewLine +
                "QuickForge stopped the upload because your local vault may be older than the Google Drive vault." + Environment.NewLine + Environment.NewLine +
                "Recommended next steps:" + Environment.NewLine +
                "1. Export an encrypted backup if you want to keep this local state." + Environment.NewLine +
                "2. Click Refresh to load the newest cloud vault." + Environment.NewLine +
                "3. Re-apply your change after refreshing if needed." + Environment.NewLine + Environment.NewLine +
                "Technical detail:" + Environment.NewLine +
                ex.Message,
                "Sync conflict detected",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }

        private async Task EnsureCloudVaultIsSafeToOverwriteAsync()
        {
            if (currentDriveService == null)
            {
                throw new InvalidOperationException("Google Drive is not connected.");
            }

            GoogleDriveVaultMetadata? cloudMetadata =
                await GoogleDriveVaultService.GetVaultMetadataAsync(currentDriveService);

            if (cloudMetadata == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(lastKnownCloudFingerprint))
            {
                lastKnownCloudFingerprint = cloudMetadata.Fingerprint;
                return;
            }

            if (!string.Equals(
                cloudMetadata.Fingerprint,
                lastKnownCloudFingerprint,
                StringComparison.Ordinal
            ))
            {
                throw new InvalidOperationException(
                    "Cloud vault changed on another device. Refresh from cloud before syncing, or export an encrypted backup first."
                );
            }
        }


        private string CreateStableLegacyEntryId(VaultEntry entry)
        {
            string raw =
                (entry.Platform ?? "").Trim().ToLowerInvariant() + "\n" +
                (entry.Username ?? "").Trim().ToLowerInvariant() + "\n" +
                (entry.Secret ?? "").Trim() + "\n" +
                (entry.Website ?? "").Trim().ToLowerInvariant() + "\n" +
                (entry.Note ?? "").Trim() + "\n" +
                entry.CreatedAt.ToUniversalTime().ToString("O");

            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            string hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            return "legacy-" + hash.Substring(0, 24);
        }

        private void NormalizeVaultEntryForSync(VaultEntry entry)
        {
            if (entry.CreatedAt == DateTime.MinValue)
            {
                entry.CreatedAt = DateTime.UtcNow;
            }

            if (entry.UpdatedAt == DateTime.MinValue)
            {
                entry.UpdatedAt = entry.CreatedAt;
            }

            if (string.IsNullOrWhiteSpace(entry.Id))
            {
                entry.Id = CreateStableLegacyEntryId(entry);
            }
        }

        private void NormalizeVaultEntriesForSync(IEnumerable<VaultEntry> entries)
        {
            foreach (VaultEntry entry in entries)
            {
                NormalizeVaultEntryForSync(entry);
            }
        }

        private void PruneDeletedEntryTombstones()
        {
            EnsureVaultSafetyCollections();

            currentVaultSettings.DeletedEntries = currentVaultSettings.DeletedEntries
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.EntryId))
                .GroupBy(item => item.EntryId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(item => item.DeletedAtUtc).First())
                .OrderByDescending(item => item.DeletedAtUtc)
                .Take(MaxDeletedEntryTombstones)
                .ToList();
        }

        private void AddDeletedEntryTombstone(VaultEntry entry, string displayName)
        {
            NormalizeVaultEntryForSync(entry);
            EnsureLocalDeviceIdentity();
            EnsureVaultSafetyCollections();

            DateTime deletedAtUtc = DateTime.UtcNow;

            VaultDeletedEntry? existing = currentVaultSettings.DeletedEntries
                .FirstOrDefault(item => string.Equals(item.EntryId, entry.Id, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                currentVaultSettings.DeletedEntries.Add(new VaultDeletedEntry
                {
                    EntryId = entry.Id,
                    DisplayName = displayName,
                    DeletedAtUtc = deletedAtUtc,
                    DeletedByDeviceId = localDeviceId,
                    DeletedByDeviceName = localDeviceName
                });
            }
            else if (deletedAtUtc >= existing.DeletedAtUtc)
            {
                existing.DisplayName = displayName;
                existing.DeletedAtUtc = deletedAtUtc;
                existing.DeletedByDeviceId = localDeviceId;
                existing.DeletedByDeviceName = localDeviceName;
            }

            PruneDeletedEntryTombstones();
        }

        private void MergeDeletedEntryTombstonesFromCloud(VaultSettings cloudSettings)
        {
            EnsureVaultSafetyCollections();
            cloudSettings.DeletedEntries ??= new List<VaultDeletedEntry>();

            foreach (VaultDeletedEntry incoming in cloudSettings.DeletedEntries)
            {
                if (incoming == null || string.IsNullOrWhiteSpace(incoming.EntryId))
                {
                    continue;
                }

                VaultDeletedEntry? existing = currentVaultSettings.DeletedEntries
                    .FirstOrDefault(item => string.Equals(item.EntryId, incoming.EntryId, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    currentVaultSettings.DeletedEntries.Add(new VaultDeletedEntry
                    {
                        EntryId = incoming.EntryId,
                        DisplayName = incoming.DisplayName,
                        DeletedAtUtc = incoming.DeletedAtUtc,
                        DeletedByDeviceId = incoming.DeletedByDeviceId,
                        DeletedByDeviceName = incoming.DeletedByDeviceName
                    });
                }
                else if (incoming.DeletedAtUtc >= existing.DeletedAtUtc)
                {
                    existing.DisplayName = incoming.DisplayName;
                    existing.DeletedAtUtc = incoming.DeletedAtUtc;
                    existing.DeletedByDeviceId = incoming.DeletedByDeviceId;
                    existing.DeletedByDeviceName = incoming.DeletedByDeviceName;
                }
            }

            PruneDeletedEntryTombstones();
        }

        private void ApplyDeletedEntryTombstonesToEntries(List<VaultEntry> entries)
        {
            EnsureVaultSafetyCollections();

            foreach (VaultEntry entry in entries)
            {
                NormalizeVaultEntryForSync(entry);
            }

            entries.RemoveAll(entry =>
            {
                VaultDeletedEntry? tombstone = currentVaultSettings.DeletedEntries
                    .FirstOrDefault(item => string.Equals(item.EntryId, entry.Id, StringComparison.OrdinalIgnoreCase));

                if (tombstone == null)
                {
                    return false;
                }

                DateTime entryUpdatedAtUtc = entry.UpdatedAt == DateTime.MinValue
                    ? DateTime.MinValue
                    : entry.UpdatedAt.ToUniversalTime();

                return tombstone.DeletedAtUtc >= entryUpdatedAtUtc;
            });
        }

        private void AddOrReplaceMergedEntry(List<VaultEntry> mergedEntries, VaultEntry entry)
        {
            NormalizeVaultEntryForSync(entry);

            int existingIndex = mergedEntries.FindIndex(existing =>
                string.Equals(existing.Id, entry.Id, StringComparison.OrdinalIgnoreCase)
            );

            if (existingIndex >= 0)
            {
                // Local version wins for the same entry. This preserves the user's latest local edit.
                mergedEntries[existingIndex] = entry;
            }
            else
            {
                mergedEntries.Add(entry);
            }
        }


        private DateTime GetDeviceTrustVersionUtc(KnownVaultDevice device)
        {
            DateTime trustChanged = device.TrustedChangedAtUtc ?? device.FirstSeenAtUtc;
            DateTime removedChanged = device.RemovedFromTrustListAtUtc ?? DateTime.MinValue;

            return removedChanged > trustChanged ? removedChanged : trustChanged;
        }

        private KnownVaultDevice CloneKnownDevice(KnownVaultDevice source)
        {
            return new KnownVaultDevice
            {
                DeviceId = source.DeviceId,
                DeviceName = source.DeviceName,
                FirstSeenAtUtc = source.FirstSeenAtUtc,
                LastSeenAtUtc = source.LastSeenAtUtc,
                SyncCount = source.SyncCount,
                IsTrusted = source.IsTrusted,
                TrustedChangedAtUtc = source.TrustedChangedAtUtc,
                TrustNote = source.TrustNote,
                IsHiddenFromTrustList = source.IsHiddenFromTrustList,
                RemovedFromTrustListAtUtc = source.RemovedFromTrustListAtUtc
            };
        }

        private void MergeKnownDeviceIntoCurrent(KnownVaultDevice incomingDevice)
        {
            if (incomingDevice == null || string.IsNullOrWhiteSpace(incomingDevice.DeviceId))
            {
                return;
            }

            EnsureVaultSafetyCollections();

            KnownVaultDevice? existingDevice = currentVaultSettings.KnownDevices
                .FirstOrDefault(device =>
                    string.Equals(device.DeviceId, incomingDevice.DeviceId, StringComparison.OrdinalIgnoreCase)
                );

            if (existingDevice == null)
            {
                currentVaultSettings.KnownDevices.Add(CloneKnownDevice(incomingDevice));
                return;
            }

            if (incomingDevice.FirstSeenAtUtc < existingDevice.FirstSeenAtUtc)
            {
                existingDevice.FirstSeenAtUtc = incomingDevice.FirstSeenAtUtc;
            }

            if (incomingDevice.LastSeenAtUtc > existingDevice.LastSeenAtUtc)
            {
                existingDevice.LastSeenAtUtc = incomingDevice.LastSeenAtUtc;
                existingDevice.DeviceName = incomingDevice.DeviceName;
            }

            existingDevice.SyncCount = Math.Max(existingDevice.SyncCount, incomingDevice.SyncCount);

            DateTime incomingTrustVersion = GetDeviceTrustVersionUtc(incomingDevice);
            DateTime existingTrustVersion = GetDeviceTrustVersionUtc(existingDevice);

            if (incomingTrustVersion >= existingTrustVersion)
            {
                existingDevice.IsTrusted = incomingDevice.IsTrusted;
                existingDevice.TrustedChangedAtUtc = incomingDevice.TrustedChangedAtUtc;
                existingDevice.TrustNote = incomingDevice.TrustNote;
                existingDevice.IsHiddenFromTrustList = incomingDevice.IsHiddenFromTrustList;
                existingDevice.RemovedFromTrustListAtUtc = incomingDevice.RemovedFromTrustListAtUtc;
            }
        }

        private void MergeVaultSettingsFromCloud(VaultSettings? cloudSettings)
        {
            if (cloudSettings == null)
            {
                return;
            }

            EnsureVaultSafetyCollections();

            cloudSettings.KnownDevices ??= new List<KnownVaultDevice>();
            cloudSettings.SafetyTimeline ??= new List<VaultSafetyEvent>();
            cloudSettings.DeletedEntries ??= new List<VaultDeletedEntry>();

            MergeDeletedEntryTombstonesFromCloud(cloudSettings);

            foreach (KnownVaultDevice cloudDevice in cloudSettings.KnownDevices)
            {
                MergeKnownDeviceIntoCurrent(cloudDevice);
            }

            List<VaultSafetyEvent> mergedTimeline = currentVaultSettings.SafetyTimeline
                .Concat(cloudSettings.SafetyTimeline)
                .GroupBy(item =>
                    item.EventAtUtc.ToString("O") + "|" +
                    item.DeviceId + "|" +
                    item.Action + "|" +
                    item.Detail
                )
                .Select(group => group.First())
                .OrderByDescending(item => item.EventAtUtc)
                .Take(MaxSafetyTimelineEvents)
                .ToList();

            currentVaultSettings.SafetyTimeline = mergedTimeline;

            if (cloudSettings.LastBackupAtUtc.HasValue &&
                (!currentVaultSettings.LastBackupAtUtc.HasValue ||
                cloudSettings.LastBackupAtUtc.Value > currentVaultSettings.LastBackupAtUtc.Value))
            {
                currentVaultSettings.LastBackupAtUtc = cloudSettings.LastBackupAtUtc;
            }

            if (cloudSettings.LastChangedAtUtc.HasValue &&
                (!currentVaultSettings.LastChangedAtUtc.HasValue ||
                cloudSettings.LastChangedAtUtc.Value > currentVaultSettings.LastChangedAtUtc.Value))
            {
                currentVaultSettings.LastChangedAtUtc = cloudSettings.LastChangedAtUtc;
                currentVaultSettings.LastChangedByDeviceId = cloudSettings.LastChangedByDeviceId;
                currentVaultSettings.LastChangedByDeviceName = cloudSettings.LastChangedByDeviceName;
            }
        }
        private async Task MergeLatestCloudVaultIntoCurrentSessionAsync()
        {
            if (currentDriveService == null)
            {
                throw new InvalidOperationException("Google Drive is not connected.");
            }

            if (currentDataKey == null)
            {
                throw new InvalidOperationException("Vault is locked.");
            }

            SetSyncStatus("Merging cloud changes...");

            GoogleDriveVaultMetadata? cloudMetadata =
                await GoogleDriveVaultService.GetVaultMetadataAsync(currentDriveService);

            string? encryptedJson =
                await GoogleDriveVaultService.DownloadEncryptedVaultAsync(currentDriveService);

            if (string.IsNullOrWhiteSpace(encryptedJson))
            {
                throw new InvalidOperationException("No encrypted cloud vault was found to merge.");
            }

            VaultData cloudVault = VaultCryptoService.DecryptVaultWithExistingDataKey(
                encryptedJson,
                currentDataKey,
                out EncryptedVaultFile latestEncryptedVaultFile
            );

            MergeVaultSettingsFromCloud(cloudVault.Settings);

            NormalizeVaultEntriesForSync(cloudVault.Entries);
            NormalizeVaultEntriesForSync(vaultEntries);

            List<VaultEntry> mergedEntries = new List<VaultEntry>();

            // Cloud first: keeps the newest cloud state from the other PC.
            foreach (VaultEntry cloudEntry in cloudVault.Entries)
            {
                AddOrReplaceMergedEntry(mergedEntries, cloudEntry);
            }

            // Local second: keeps the unsynced entry/edit from this PC.
            foreach (VaultEntry localEntry in vaultEntries)
            {
                AddOrReplaceMergedEntry(mergedEntries, localEntry);
            }

            ApplyDeletedEntryTombstonesToEntries(mergedEntries);

            vaultEntries.Clear();
            vaultEntries.AddRange(mergedEntries);

            currentEncryptedVaultFile = latestEncryptedVaultFile;
            lastKnownCloudFingerprint = cloudMetadata?.Fingerprint ?? lastKnownCloudFingerprint;
            lastCloudLoadUtc = DateTime.UtcNow;

            RefreshVaultList();

            SetPreviewText(
                "Cloud changes merged.",
                "QuickForge kept the latest cloud vault and your local unsynced changes.",
                "Syncing merged encrypted vault..."
            );
        }


        private void QueueBackgroundVaultSync(string reason)
        {
            hasUnsyncedLocalChanges = true;
            backgroundVaultSyncRequested = true;
            backgroundVaultSyncReason = reason;

            SetSyncStatus(IsDeleteSyncReason(reason) ? "Delete pending" : "Pending local changes");

            if (backgroundVaultSyncRunning)
            {
                return;
            }

            _ = RunBackgroundVaultSyncLoopAsync();
        }

        private async Task RunBackgroundVaultSyncLoopAsync()
        {
            backgroundVaultSyncRunning = true;

            try
            {
                while (backgroundVaultSyncRequested)
                {
                    backgroundVaultSyncRequested = false;
                    string reason = backgroundVaultSyncReason;
                    bool isDeleteSync = IsDeleteSyncReason(reason);

                    try
                    {
                        SetSyncStatus(isDeleteSync ? "Delete pending" : "Syncing in background...");

                        bool merged = await SaveCurrentVaultToCloudWithAutoMergeAsync();

                        if (!backgroundVaultSyncRequested)
                        {
                            hasUnsyncedLocalChanges = false;
                        }

                        backgroundVaultSyncRetryCount = 0;
                        SetSyncStatus("Active", success: true);

                        selectedPreviewLabel.Text = merged
                            ? reason + Environment.NewLine + "Cloud changes were merged and synced in the background."
                            : reason + Environment.NewLine + "Synced in the background."; 
                    }
                    catch (Exception ex)
                    {
                        hasUnsyncedLocalChanges = true;
                        backgroundVaultSyncRetryCount++;

                        if (backgroundVaultSyncRetryCount <= MaxBackgroundSyncRetries)
                        {
                            SetSyncStatus(
                                isDeleteSync
                                    ? "Delete pending - retry " + backgroundVaultSyncRetryCount + "/" + MaxBackgroundSyncRetries
                                    : "Sync retry " + backgroundVaultSyncRetryCount + "/" + MaxBackgroundSyncRetries,
                                error: true
                            );

                            SetPreviewText(
                                reason,
                                "Still saved locally, retrying...",
                                "Retry " + backgroundVaultSyncRetryCount + " of " + MaxBackgroundSyncRetries + " in " + BackgroundSyncRetryDelaySeconds + " seconds.",
                                "Error: " + ex.Message
                            );

                            await Task.Delay(TimeSpan.FromSeconds(BackgroundSyncRetryDelaySeconds));

                            backgroundVaultSyncRequested = true;
                            continue;
                        }

                        SetSyncStatus(
                            isDeleteSync ? "Delete pending - sync failed" : "Sync failed - local changes pending",
                            error: true
                        );

                        SetPreviewText(
                            reason,
                            "Still saved locally, but automatic retries failed.",
                            "Use Sync pending or export an encrypted backup before closing.",
                            "Error: " + ex.Message
                        );

                        backgroundVaultSyncRetryCount = 0;
                    }
                }
            }
            finally
            {
                backgroundVaultSyncRunning = false;

                if (backgroundVaultSyncRequested)
                {
                    _ = RunBackgroundVaultSyncLoopAsync();
                }
                else
                {
                    UpdateManualSyncButtonState();
                }
            }
        }

        private void EnsureLocalDeviceIdentity()
        {
            if (!string.IsNullOrWhiteSpace(localDeviceId))
            {
                return;
            }

            localDeviceName = string.IsNullOrWhiteSpace(Environment.MachineName)
                ? "This device"
                : Environment.MachineName.Trim();

            string appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "QuickForge Sync"
            );

            string deviceFilePath = Path.Combine(appDataFolder, "device.id");

            try
            {
                if (File.Exists(deviceFilePath))
                {
                    string[] lines = File.ReadAllLines(deviceFilePath);

                    if (lines.Length >= 2 &&
                        !string.IsNullOrWhiteSpace(lines[0]) &&
                        !string.IsNullOrWhiteSpace(lines[1]))
                    {
                        localDeviceId = lines[0].Trim();
                        localDeviceName = lines[1].Trim();
                        return;
                    }
                }
            }
            catch
            {
                // If local device identity cannot be read, create a new one safely.
            }

            localDeviceId = Guid.NewGuid().ToString("N");

            try
            {
                Directory.CreateDirectory(appDataFolder);
                File.WriteAllLines(deviceFilePath, new[] { localDeviceId, localDeviceName });
            }
            catch
            {
                // App can still run; the device will be temporary for this session.
            }
        }

        private void EnsureVaultSafetyCollections()
        {
            currentVaultSettings.KnownDevices ??= new List<KnownVaultDevice>();
            currentVaultSettings.SafetyTimeline ??= new List<VaultSafetyEvent>();
            currentVaultSettings.DeletedEntries ??= new List<VaultDeletedEntry>();
        }


        private bool IsCurrentDeviceTrusted()
        {
            if (!isVaultUnlocked)
            {
                return true;
            }

            EnsureLocalDeviceIdentity();
            EnsureVaultSafetyCollections();

            KnownVaultDevice? currentDevice = currentVaultSettings.KnownDevices
                .FirstOrDefault(device => device.DeviceId == localDeviceId);

            return currentDevice == null || currentDevice.IsTrusted;
        }

        private bool IsRestrictedModeActive()
        {
            return isVaultUnlocked && !IsCurrentDeviceTrusted();
        }

        private void ShowLocalSecurityNotification(string title, string message)
        {
            try
            {
                if (trayIcon != null)
                {
                    trayIcon.BalloonTipTitle = title;
                    trayIcon.BalloonTipText = message;
                    trayIcon.BalloonTipIcon = ToolTipIcon.Warning;
                    trayIcon.ShowBalloonTip(7000);
                }
            }
            catch
            {
                // Local notifications are best-effort only.
            }
        }

        private void ShowRestrictedModeWarningIfNeeded()
        {
            if (!IsRestrictedModeActive() || restrictedModeWarningShownThisSession)
            {
                return;
            }

            restrictedModeWarningShownThisSession = true;

            string message =
                "This device is marked as UNTRUSTED for this vault." + Environment.NewLine + Environment.NewLine +
                "QuickForge has disabled sensitive actions on this device:" + Environment.NewLine +
                "- Reveal/copy passwords" + Environment.NewLine +
                "- Open + Fill" + Environment.NewLine +
                "- Add/edit/delete entries" + Environment.NewLine +
                "- Backup/import" + Environment.NewLine +
                "- Change vault code or recovery key" + Environment.NewLine +
                "- Manage Device Trust" + Environment.NewLine + Environment.NewLine +
                "To regain full access, open QuickForge on a trusted device, go to Security Center > Device Trust, select this device, and click Trust." + Environment.NewLine + Environment.NewLine +
                "If you do not recognize this device, check your Google Account security, remove unknown signed-in devices, change your Google password, enable 2-step verification, then rotate your QuickForge vault code and recovery key from a trusted device.";

            MessageBox.Show(
                message,
                "Untrusted device mode",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            ShowLocalSecurityNotification(
                "QuickForge: Untrusted device",
                "Sensitive actions are disabled. Trust this device from another trusted device, or check Google security."
            );
        }

        private bool RequireTrustedDeviceForSensitiveAction(string actionName)
        {
            if (!IsRestrictedModeActive())
            {
                return true;
            }

            string message =
                "This device is UNTRUSTED, so QuickForge blocked this action:" +
                Environment.NewLine + Environment.NewLine +
                actionName + Environment.NewLine + Environment.NewLine +
                "To regain full access, open QuickForge on a trusted device, go to Security Center > Device Trust, select this device, and click Trust." + Environment.NewLine + Environment.NewLine +
                "If you do not recognize this device, check your Google Account security, remove unknown signed-in devices, change your Google password, enable 2-step verification, then rotate your QuickForge vault code and recovery key from a trusted device.";

            SetPreviewText(
                "UNTRUSTED DEVICE MODE",
                "Blocked action: " + actionName,
                "Sensitive actions are disabled until this device is trusted."
            );

            MessageBox.Show(
                message,
                "Restricted Mode",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            ShowLocalSecurityNotification(
                "QuickForge blocked an action",
                "Restricted Mode blocked: " + actionName
            );

            return false;
        }

        private void ApplyDeviceTrustRestrictionsToUi()
        {
            bool restricted = IsRestrictedModeActive();

            restrictedModeBannerLabel.Visible = restricted;

            if (restricted)
            {
                restrictedModeBannerLabel.BringToFront();

                SetPreviewText(
                    "UNTRUSTED DEVICE MODE",
                    "This device is not trusted for this vault.",
                    "Sensitive actions are disabled. Refresh, Sync, Lock, and Security Center remain available."
                );
            }

            platformTextBox.Enabled = !restricted;
            usernameTextBox.Enabled = !restricted;
            secretTextBox.Enabled = !restricted;
            websiteTextBox.Enabled = !restricted;
            noteTextBox.Enabled = !restricted;

            saveEntryButton.Enabled = !restricted;
            clearButton.Enabled = !restricted;
            createPasswordButton.Enabled = !restricted;
            saveChangesButton.Enabled = !restricted && saveChangesButton.Visible;
            cancelEditButton.Enabled = true;

            editEntryButton.Enabled = !restricted && editingEntry == null;
            deleteEntryButton.Enabled = !restricted && editingEntry == null;
            favoriteButton.Enabled = !restricted && editingEntry == null;
            openAndFillButton.Enabled = !restricted && editingEntry == null;

            revealButton.Enabled = !restricted;
            copySecretButton.Enabled = !restricted;
            copyUsernameButton.Enabled = !restricted;

            changeVaultCodeButton.Enabled = !restricted;
            backupButton.Enabled = !restricted;
            rotateRecoveryKeyButton.Enabled = !restricted;
            recoveryReminderComboBox.Enabled = !restricted;
            autoRefreshComboBox.Enabled = !restricted;

            if (secretVisibilityButton != null)
            {
                secretVisibilityButton.Enabled = !restricted;
            }

            securityCenterButton.Enabled = true;
            refreshCloudButton.Enabled = true;
            manualSyncButton.Enabled = true;
            lockVaultButton.Enabled = true;
            openSiteButton.Enabled = editingEntry == null;
        }
        private bool RegisterCurrentDeviceForVault(bool showWarning)
        {
            EnsureLocalDeviceIdentity();
            EnsureVaultSafetyCollections();

            int knownDeviceCountBefore = currentVaultSettings.KnownDevices.Count;

            KnownVaultDevice? device = currentVaultSettings.KnownDevices
                .FirstOrDefault(item => item.DeviceId == localDeviceId);

            bool isNewDevice = device == null;

            if (device == null)
            {
                device = new KnownVaultDevice
                {
                    DeviceId = localDeviceId,
                    DeviceName = localDeviceName,
                    FirstSeenAtUtc = DateTime.UtcNow,
                    LastSeenAtUtc = DateTime.UtcNow,
                    SyncCount = 0,
                    IsTrusted = knownDeviceCountBefore == 0,
                    TrustedChangedAtUtc = DateTime.UtcNow,
                    TrustNote = knownDeviceCountBefore == 0
                        ? "First device automatically trusted."
                        : "New device awaits manual trust.",
                    IsHiddenFromTrustList = false,
                    RemovedFromTrustListAtUtc = null
                };

                currentVaultSettings.KnownDevices.Add(device);

                AddSafetyTimelineEvent(
                    "New device registered",
                    localDeviceName + " was added to this vault."
                );
            }
            else
            {
                device.DeviceName = localDeviceName;
                device.LastSeenAtUtc = DateTime.UtcNow;

                if (device.IsHiddenFromTrustList)
                {
                    device.IsHiddenFromTrustList = false;
                    device.RemovedFromTrustListAtUtc = null;
                    device.IsTrusted = false;
                    device.TrustedChangedAtUtc = DateTime.UtcNow;
                    device.TrustNote = "Device reopened after being removed; marked untrusted.";

                    AddSafetyTimelineEvent(
                        "Removed device reopened",
                        localDeviceName + " opened the vault again after being removed, so it was marked untrusted."
                    );
                }
            }

            if (isNewDevice &&
                knownDeviceCountBefore > 0 &&
                showWarning &&
                !newDeviceDetectedThisSession)
            {
                newDeviceDetectedThisSession = true;
                newDeviceDetectedName = localDeviceName;

                MessageBox.Show(
                    "New device detected on this vault." + Environment.NewLine + Environment.NewLine +
                    "Device: " + localDeviceName + Environment.NewLine + Environment.NewLine +
                    "If this was you, no action is needed." + Environment.NewLine +
                    "If this was not you, change your vault code and rotate your recovery key.",
                    "New device detected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }

            if (!isNewDevice &&
                device != null &&
                !device.IsTrusted &&
                showWarning)
            {
                untrustedDeviceDetectedThisSession = true;
                untrustedDeviceDetectedName = localDeviceName;

                MessageBox.Show(
                    "This device is marked as untrusted for this vault." + Environment.NewLine + Environment.NewLine +
                    "Device: " + localDeviceName + Environment.NewLine + Environment.NewLine +
                    "You can still view the vault, but sensitive trust changes should only be made from a trusted device.",
                    "Untrusted device",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }

            return isNewDevice;
        }

        private void AddSafetyTimelineEvent(string action, string detail)
        {
            EnsureLocalDeviceIdentity();
            EnsureVaultSafetyCollections();

            currentVaultSettings.SafetyTimeline.Add(new VaultSafetyEvent
            {
                EventAtUtc = DateTime.UtcNow,
                DeviceId = localDeviceId,
                DeviceName = localDeviceName,
                Action = action,
                Detail = detail
            });

            currentVaultSettings.SafetyTimeline = currentVaultSettings.SafetyTimeline
                .OrderByDescending(item => item.EventAtUtc)
                .Take(MaxSafetyTimelineEvents)
                .ToList();
        }

        private void MarkVaultChangedByCurrentDevice(string action)
        {
            EnsureLocalDeviceIdentity();
            EnsureVaultSafetyCollections();

            RegisterCurrentDeviceForVault(false);

            KnownVaultDevice? device = currentVaultSettings.KnownDevices
                .FirstOrDefault(item => item.DeviceId == localDeviceId);

            if (device != null)
            {
                device.LastSeenAtUtc = DateTime.UtcNow;
                device.SyncCount++;
            }

            currentVaultSettings.LastChangedByDeviceId = localDeviceId;
            currentVaultSettings.LastChangedByDeviceName = localDeviceName;
            currentVaultSettings.LastChangedAtUtc = DateTime.UtcNow;

            AddSafetyTimelineEvent(action, "Vault changed by " + localDeviceName + ".");
        }

        private void MarkBackupCreatedByCurrentDevice(string fileName)
        {
            currentVaultSettings.LastBackupAtUtc = DateTime.UtcNow;

            AddSafetyTimelineEvent(
                "Encrypted backup exported",
                string.IsNullOrWhiteSpace(fileName)
                    ? "Encrypted backup was exported."
                    : "Backup file: " + fileName
            );
        }

        private string BuildKnownDevicesText()
        {
            EnsureVaultSafetyCollections();

            List<KnownVaultDevice> visibleDevices = currentVaultSettings.KnownDevices
                .Where(device => !device.IsHiddenFromTrustList || device.DeviceId == localDeviceId)
                .OrderByDescending(device => device.LastSeenAtUtc)
                .Take(6)
                .ToList();

            if (visibleDevices.Count == 0)
            {
                return "- No known devices recorded yet.";
            }

            return string.Join(
                Environment.NewLine,
                visibleDevices.Select(device =>
                    "- " + device.DeviceName +
                    " - last seen " + device.LastSeenAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm") +
                    " - " + (device.IsTrusted ? "trusted" : "UNTRUSTED") +
                    (device.DeviceId == localDeviceId ? " (this device)" : "")
                )
            );
        }

        private string BuildSafetyTimelineText()
        {
            EnsureVaultSafetyCollections();

            if (currentVaultSettings.SafetyTimeline.Count == 0)
            {
                return "- No safety events recorded yet.";
            }

            return string.Join(
                Environment.NewLine,
                currentVaultSettings.SafetyTimeline
                    .OrderByDescending(item => item.EventAtUtc)
                    .Take(8)
                    .Select(item =>
                        "- " + item.EventAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm") +
                        " - " + item.Action +
                        " - " + item.DeviceName
                    )
            );
        }

        private string BuildVaultSafetyReport(
            int totalEntries,
            int weakPasswords,
            int reusedPasswordEntries,
            int missingWebsiteLinks)
        {
            EnsureLocalDeviceIdentity();
            EnsureVaultSafetyCollections();
            RegisterCurrentDeviceForVault(false);

            int score = 100;
            List<string> good = new List<string>();
            List<string> warnings = new List<string>();

            bool backupRecent =
                currentVaultSettings.LastBackupAtUtc.HasValue &&
                (DateTime.UtcNow - currentVaultSettings.LastBackupAtUtc.Value).TotalDays <= 7;

            if (backupRecent)
            {
                good.Add("Backup created recently");
            }
            else if (currentVaultSettings.LastBackupAtUtc.HasValue)
            {
                int backupAgeDays = Math.Max(1, (int)(DateTime.UtcNow - currentVaultSettings.LastBackupAtUtc.Value).TotalDays);
                warnings.Add("Last backup is " + backupAgeDays + " day(s) old");
                score -= Math.Min(20, backupAgeDays);
            }
            else
            {
                warnings.Add("No encrypted backup timestamp recorded yet");
                score -= 20;
            }

            if (currentEncryptedVaultFile?.RecoveryKeyWrapper != null)
            {
                good.Add("Recovery key exists");
            }
            else
            {
                warnings.Add("Recovery key wrapper missing");
                score -= 25;
            }

            if (newDeviceDetectedThisSession)
            {
                warnings.Add("New device detected this session: " + newDeviceDetectedName);
                score -= 15;
            }
            else
            {
                good.Add("No unknown device detected this session");
            }

            KnownVaultDevice? currentKnownDevice = currentVaultSettings.KnownDevices
                .FirstOrDefault(device => device.DeviceId == localDeviceId);

            if (currentKnownDevice != null && currentKnownDevice.IsTrusted)
            {
                good.Add("This device is trusted");
            }
            else
            {
                warnings.Add("This device is not trusted yet");
                score -= 15;
            }

            if (untrustedDeviceDetectedThisSession)
            {
                warnings.Add("Untrusted device warning shown this session: " + untrustedDeviceDetectedName);
                score -= 10;
            }

            if (lastCloudSaveUtc.HasValue || lastCloudLoadUtc.HasValue)
            {
                good.Add("Sync activity detected");
            }
            else
            {
                warnings.Add("No sync timestamp recorded this session");
                score -= 10;
            }

            if (!HasPendingBackgroundVaultSync())
            {
                good.Add("Safe-close protection active");
            }
            else
            {
                warnings.Add("Background sync is still pending");
                score -= 10;
            }

            if (reusedPasswordEntries > 0)
            {
                warnings.Add(reusedPasswordEntries + " entries have reused passwords");
                score -= Math.Min(25, reusedPasswordEntries * 8);
            }
            else if (totalEntries > 0)
            {
                good.Add("No reused passwords detected");
            }

            if (weakPasswords > 0)
            {
                warnings.Add(weakPasswords + " weak password(s) detected");
                score -= Math.Min(25, weakPasswords * 6);
            }
            else if (totalEntries > 0)
            {
                good.Add("No weak passwords detected");
            }

            if (missingWebsiteLinks > 0)
            {
                warnings.Add(missingWebsiteLinks + " entries are missing website links");
                score -= Math.Min(8, missingWebsiteLinks);
            }

            score = Math.Max(0, Math.Min(100, score));

            string lastChangedText = currentVaultSettings.LastChangedAtUtc.HasValue
                ? currentVaultSettings.LastChangedAtUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm") +
                  " by " + MaskEmpty(currentVaultSettings.LastChangedByDeviceName)
                : "Not recorded yet";

            string goodText = good.Count == 0
                ? "- None yet"
                : string.Join(Environment.NewLine, good.Select(item => "- " + item));

            string warningsText = warnings.Count == 0
                ? "- No warnings"
                : string.Join(Environment.NewLine, warnings.Select(item => "- " + item));

            return
                "Vault Safety Score: " + score + "/100" + Environment.NewLine +
                "Last changed: " + lastChangedText + Environment.NewLine +
                "This device: " + localDeviceName + Environment.NewLine +
                Environment.NewLine +
                "Good:" + Environment.NewLine +
                goodText +
                Environment.NewLine + Environment.NewLine +
                "Warnings:" + Environment.NewLine +
                warningsText +
                Environment.NewLine + Environment.NewLine +
                "Known devices:" + Environment.NewLine +
                BuildKnownDevicesText() +
                Environment.NewLine + Environment.NewLine +
                "Safety timeline:" + Environment.NewLine +
                BuildSafetyTimelineText();
        }
        private async Task<bool> SaveCurrentVaultToCloudWithAutoMergeAsync()
        {
            try
            {
                await SaveCurrentVaultToCloudAsync();
                return false;
            }
            catch (Exception ex) when (IsCloudConflictException(ex))
            {
                SetPreviewText(
                    "Cloud changed on another device.",
                    "QuickForge is merging the newest cloud vault with your local unsynced changes.",
                    "This prevents overwriting another PC."
                );

                await MergeLatestCloudVaultIntoCurrentSessionAsync();
                await SaveCurrentVaultToCloudAsync();

                SetPreviewText(
                    "Merged and synced.",
                    "Your local changes and the other device changes were saved together.",
                    "Refresh the other PC to see the merged vault."
                );

                return true;
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

            SetSyncStatus("Checking cloud...");

            await EnsureCloudVaultIsSafeToOverwriteAsync();

            SetSyncStatus("Saving...");

            MarkVaultChangedByCurrentDevice("Vault synced");

            string encryptedJson = CreateCurrentEncryptedVaultJson();

            GoogleDriveVaultMetadata? uploadedMetadata =
                await GoogleDriveVaultService.UploadEncryptedVaultAsync(
                    currentDriveService,
                    encryptedJson
                );

            lastKnownCloudFingerprint = uploadedMetadata?.Fingerprint ?? lastKnownCloudFingerprint;

            cloudVaultExists = true;
            lastCloudSaveUtc = DateTime.UtcNow;
            SetSyncStatus("Active", success: true);
        }

        private async Task LoadVaultFromCloudAsync()
        {
            if (currentDriveService == null)
            {
                throw new InvalidOperationException("Google Drive is not connected.");
            }

            SetSyncStatus("Loading from Google Drive...");

            GoogleDriveVaultMetadata? cloudMetadata =
                await GoogleDriveVaultService.GetVaultMetadataAsync(currentDriveService);

            string? encryptedJson =
                await GoogleDriveVaultService.DownloadEncryptedVaultAsync(currentDriveService);

            if (string.IsNullOrWhiteSpace(encryptedJson))
            {
                throw new InvalidOperationException("No encrypted vault was found.");
            }

            VaultData vaultData;
            byte[] dataKey;
            EncryptedVaultFile encryptedVaultFile;

            if (isVaultUnlocked && currentDataKey != null)
            {
                vaultData = VaultCryptoService.DecryptVaultWithExistingDataKey(
                    encryptedJson,
                    currentDataKey,
                    out encryptedVaultFile
                );

                dataKey = currentDataKey;
            }
            else
            {
                vaultData = VaultCryptoService.DecryptVault(
                    encryptedJson,
                    vaultCode,
                    out dataKey,
                    out encryptedVaultFile
                );
            }
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
            lastKnownCloudFingerprint = cloudMetadata?.Fingerprint ?? lastKnownCloudFingerprint;
            lastCloudLoadUtc = DateTime.UtcNow;
            SetSyncStatus("Active", success: true);
        }
        private async void RefreshCloudButton_Click(object? sender, EventArgs e)
        {
            if (currentDriveService == null)
            {
                SetSyncStatus("Not connected", error: true);
                MessageBox.Show(
                    "Google Drive is not connected. Log in with Google before refreshing from cloud.",
                    "Refresh unavailable",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            if (!isVaultUnlocked)
            {
                SetSyncStatus("Vault locked", error: true);
                MessageBox.Show(
                    "Unlock your vault before refreshing from cloud.",
                    "Vault locked",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            DialogResult confirm = MessageBox.Show(
                "Refresh will load the latest encrypted vault from Google Drive." + Environment.NewLine + Environment.NewLine +
                "This is the safest option if another device may have newer changes." + Environment.NewLine + Environment.NewLine +
                "If you want to preserve the current local state first, export an encrypted backup before refreshing." + Environment.NewLine + Environment.NewLine +
                "Continue?",
                "Refresh from cloud",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information
            );

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                refreshCloudButton.Enabled = false;
                manualSyncButton.Enabled = false;

                selectedPreviewLabel.Text = "Refreshing from Google Drive...";

                await LoadVaultFromCloudAsync();

                selectedPreviewLabel.Text =
                    "Refresh completed." + Environment.NewLine +
                    "Latest encrypted vault loaded from Google Drive.";
            }
            catch (Exception ex)
            {
                SetSyncStatus("Refresh failed", error: true);
                MessageBox.Show(
                    "Refresh from cloud failed: " + ex.Message,
                    "Refresh failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                refreshCloudButton.Enabled = true;
                manualSyncButton.Enabled = true;
            }
        }

        private async void ManualSyncButton_Click(object? sender, EventArgs e)
        {
            if (currentDriveService == null)
            {
                SetSyncStatus("Not connected", error: true);
                MessageBox.Show(
                    "Google Drive is not connected. Log in with Google before syncing.",
                    "Sync unavailable",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            if (!isVaultUnlocked || currentDataKey == null || currentEncryptedVaultFile == null)
            {
                SetSyncStatus("Vault locked", error: true);
                MessageBox.Show(
                    "Unlock your vault before using Sync now.",
                    "Vault locked",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            try
            {
                manualSyncButton.Enabled = false;
                selectedPreviewLabel.Text = "Manual sync started. Checking cloud and saving encrypted vault...";

                bool merged = await SaveCurrentVaultToCloudWithAutoMergeAsync();

                selectedPreviewLabel.Text = merged
                    ? "Manual sync completed after merging cloud changes." + Environment.NewLine +
                      "Your local changes and the other PC changes were saved together."
                    : "Manual sync completed." + Environment.NewLine +
                      "Your encrypted vault was saved to Google Drive.";
            }
            catch (Exception ex) when (IsCloudConflictException(ex))
            {
                ShowCloudConflictMessage(ex);
            }
            catch (Exception ex)
            {
                SetSyncStatus("Sync failed", error: true);
                MessageBox.Show(
                    "Manual sync failed: " + ex.Message,
                    "Sync failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                manualSyncButton.Enabled = true;
            }
        }

        private void LockVaultButton_Click(object? sender, EventArgs e)
        {
            LockVaultForSafety("Vault locked.");
        }
        private async void ChangeVaultCodeButton_Click(object? sender, EventArgs e)
        {
            
            if (!RequireTrustedDeviceForSensitiveAction("Change vault code"))
            {
                return;
            }
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

            if (!VaultCodePolicy.IsStrongEnough(newCode, out string vaultCodeWarning))
            {
                ShowVaultCodeStrengthMessage(vaultCodeWarning);
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

        private bool HasPendingBackgroundVaultSync()
        {
            return hasUnsyncedLocalChanges || backgroundVaultSyncRunning || backgroundVaultSyncRequested;
        }

        private bool ConfirmPendingBackgroundSyncBeforeExit(string actionText)
        {
            if (!HasPendingBackgroundVaultSync())
            {
                return true;
            }

            SetSyncStatus("Background sync still running");

            DialogResult result = MessageBox.Show(
                "Cloud sync is still running." + Environment.NewLine + Environment.NewLine +
                "Your latest local changes may still be uploading to Google Drive." + Environment.NewLine + Environment.NewLine +
                "Recommended: choose No, wait until Sync shows Active, then " + actionText + "." + Environment.NewLine + Environment.NewLine +
                "Do you want to continue anyway?",
                "Sync still running",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2
            );

            return result == DialogResult.Yes;
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!ConfirmPendingBackgroundSyncBeforeExit("close QuickForge"))
            {
                e.Cancel = true;
            }
        }
        private bool ConfirmAccountSwitchOrLogout()
        {
            if (
                currentDriveService == null &&
                string.IsNullOrWhiteSpace(connectedGoogleEmail) &&
                !isVaultUnlocked
            )
            {
                return true;
            }

            string currentAccountText = string.IsNullOrWhiteSpace(connectedGoogleEmail)
                ? "Unknown"
                : connectedGoogleEmail;

            DialogResult result = MessageBox.Show(
                "You are about to log out or switch Google accounts." + Environment.NewLine + Environment.NewLine +
                "Current account: " + currentAccountText + Environment.NewLine + Environment.NewLine +
                "- Your current vault will be locked." + Environment.NewLine +
                "- QuickForge vaults are isolated per Google account." + Environment.NewLine +
                "- A different Google account will show a different vault." + Environment.NewLine +
                "- Use Sync now before switching if you recently changed anything." + Environment.NewLine + Environment.NewLine +
                "Continue?",
                "Before switching accounts",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            return result == DialogResult.Yes;
        }

        private void LogoutButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!ConfirmAccountSwitchOrLogout())
                {
                    return;
                }

                GoogleAuthService.Logout();

                vaultCode = "";
                vaultEntries.Clear();
                RefreshVaultList();
                currentDriveService = null;
                cloudVaultExists = false;
                connectedGoogleEmail = "";
                lastCloudSaveUtc = null;
                lastCloudLoadUtc = null;
                lastKnownCloudFingerprint = null;
                SetSyncStatus("Not connected");
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

        private async void ResetTestVaultButton_Click(object? sender, EventArgs e)
        {
            if (currentDriveService == null)
            {
                MessageBox.Show("Google Drive is not connected.");
                return;
            }

            if (!string.Equals(connectedGoogleEmail, "patrickolsen4@gmail.com", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("This test reset is only enabled for the developer account.");
                return;
            }

            DialogResult firstConfirm = MessageBox.Show(
                "This will delete the QuickForge test vault for:" + Environment.NewLine + Environment.NewLine +
                connectedGoogleEmail + Environment.NewLine + Environment.NewLine +
                "This only deletes the encrypted QuickForge vault file in Google Drive app data. It does not delete your Google account." + Environment.NewLine + Environment.NewLine +
                "Continue?",
                "Reset test vault",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (firstConfirm != DialogResult.Yes)
            {
                return;
            }

            string? typedConfirmation = ShowPasswordPrompt(
                "Confirm test reset",
                "Type RESET to delete this test vault:"
            );

            if (typedConfirmation != "RESET")
            {
                MessageBox.Show("Reset cancelled.");
                return;
            }

            try
            {
                await GoogleDriveVaultService.DeleteVaultAsync(currentDriveService);

                vaultCode = "";
                vaultEntries.Clear();
                RefreshVaultList();
                ClearEntryInputs();
                ClearSecretAccessWindow();

                currentDataKey = null;
                currentEncryptedVaultFile = null;
                currentVaultSettings = new VaultSettings();
                hasShownRecoveryReminderThisSession = false;
                isVaultUnlocked = false;
                cloudVaultExists = false;

                lastKnownCloudFingerprint = null;

                vaultCodeTextBox.Clear();
                confirmVaultCodeTextBox.Clear();

                ConfigureVaultAccessForCreate();
                ShowVaultAccessUi();

                MessageBox.Show(
                    "Test vault reset complete. You can now create a fresh vault for this Google account.",
                    "Test vault reset",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not reset test vault: " + ex.Message);
            }
        }
        private void ShowLoggedOutUi()
        {
            unlockStatusTimer.Stop();
            loginCard.Visible = true;
            loginCard.Enabled = true;
            vaultAccessPanel.Visible = false;
            vaultPanel.Visible = false;
            logoutButton.Enabled = false;
            resetTestVaultButton.Visible = false;
            importBackupAccessButton.Visible = false;

            accountStatusLabel.Text = "Not connected";
            accountStatusLabel.ForeColor = softTextColor;
        }

        private void ShowVaultAccessUi()
        {
            loginCard.Visible = false;
            vaultAccessPanel.Visible = true;
            vaultPanel.Visible = false;

            UpdateDeveloperTestControls();
            importBackupAccessButton.Visible = currentDriveService != null;

            vaultAccessPanel.BringToFront();
            topBarPanel.BringToFront();
        }

        private void ShowVaultUi()
        {
            unlockStatusTimer.Stop();
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
        
            ShowEmptyVaultOnboardingIfNeeded();
        }

        private void ShowEmptyVaultOnboardingIfNeeded()
        {
            if (!isVaultUnlocked)
            {
                return;
            }

            if (vaultEntries.Count == 0)
            {
                SetPreviewText(
                    "Your vault is empty.",
                    "Add your first login, game code, license key or private note.",
                    "Controlled personal beta use is supported. Keep your vault code and recovery key safe."
                );
            }
                }

        private void SaveEntryButton_Click(object? sender, EventArgs e)
        {
            if (!RequireTrustedDeviceForSensitiveAction("Add entry"))             {                 return;             } 
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            vaultEntries.Add(entry);
            RefreshVaultList();
            ClearEntryInputs();

            string reason = "Saved locally: " + entry.GetDisplayName();

            selectedPreviewLabel.Text =
                reason + Environment.NewLine +
                "Cloud sync is running in the background.";

            SetSyncStatus("Queued background sync");

            QueueBackgroundVaultSync(reason);
        }
        private void EditEntryButton_Click(object? sender, EventArgs e)
        {
            if (!RequireTrustedDeviceForSensitiveAction("Edit entry"))             {                 return;             } 
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

        private void SaveChangesButton_Click(object? sender, EventArgs e)
        {
            if (!RequireTrustedDeviceForSensitiveAction("Save entry changes"))             {                 return;             } 
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
            editingEntry.UpdatedAt = DateTime.UtcNow;

            VaultEntry savedEntry = editingEntry;

            RefreshVaultList();

            int visibleIndex = visibleVaultEntries.IndexOf(savedEntry);

            if (visibleIndex >= 0 && visibleIndex < vaultListBox.Items.Count)
            {
                vaultListBox.SelectedIndex = visibleIndex;
            }

            SetPreviewText(
                "Saved locally: " + savedEntry.GetDisplayName(),
                "Cloud sync is running in the background.",
                "User: " + MaskEmpty(savedEntry.Username),
                "Password/code: " + MaskSecret(savedEntry.Secret)
            );

            ClearEntryInputs();
            SetEntryEditMode(false);
            SetSyncStatus("Queued background sync");

            QueueBackgroundVaultSync("Saved changes locally: " + savedEntry.GetDisplayName());
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

            bool restricted = IsRestrictedModeActive();

            saveEntryButton.Visible = !isEditing;
            clearButton.Visible = !isEditing;

            saveChangesButton.Visible = isEditing;
            cancelEditButton.Visible = isEditing;

            saveEntryButton.Enabled = !isEditing && !restricted;
            clearButton.Enabled = !isEditing;

            saveChangesButton.Enabled = isEditing && !restricted;
            cancelEditButton.Enabled = isEditing;

            editEntryButton.Enabled = !isEditing && !restricted;
            deleteEntryButton.Enabled = !isEditing && !restricted;
            favoriteButton.Enabled = !isEditing && !restricted;
            openSiteButton.Enabled = !isEditing && !restricted;
            openAndFillButton.Enabled = !isEditing && !restricted;

            if (isEditing && restricted)
            {
                selectedPreviewLabel.Text =
                    "Restricted Mode is active." + Environment.NewLine +
                    "Save changes is disabled until this device is trusted.";
            }
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
            if (!RequireTrustedDeviceForSensitiveAction("Open + Fill"))
            {
                return;
            }

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

            string usernameToFill = entry.Username;
            string passwordToFill = entry.Secret;

            selectedPreviewLabel.Text =
                "Open + Fill started." + Environment.NewLine +
                "The website will open now." + Environment.NewLine +
                "If the username field is not focused, click it within 5 seconds.";

            OpenWebsite(entry.Website);

            await Task.Delay(5000);

            try
            {
                Clipboard.SetText(usernameToFill);
                SendKeys.SendWait("^v");

                await Task.Delay(250);

                SendKeys.SendWait("{TAB}");

                await Task.Delay(250);

                Clipboard.SetText(passwordToFill);
                SendKeys.SendWait("^v");

                selectedPreviewLabel.Text =
                    "Open + Fill completed." + Environment.NewLine +
                    "If the website did not fill correctly, click the username field and use Ctrl + Alt + Q." + Environment.NewLine +
                    "Clipboard clears in 20 seconds.";

                _ = ClearClipboardLaterAsync(passwordToFill, 20000);
            }
            catch (Exception ex)
            {
                selectedPreviewLabel.Text =
                    "Open + Fill could not complete automatically." + Environment.NewLine +
                    "Click the username field and use Ctrl + Alt + Q instead." + Environment.NewLine +
                    "Error: " + ex.Message;
            }
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
            if (!RequireTrustedDeviceForSensitiveAction("Reveal password/code"))             {                 return;             } 
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
            if (!RequireTrustedDeviceForSensitiveAction("Copy password/code"))             {                 return;             } 
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
            if (!RequireTrustedDeviceForSensitiveAction("Copy username"))             {                 return;             } 
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
        private void FavoriteButton_Click(object? sender, EventArgs e)
        {
            if (!RequireTrustedDeviceForSensitiveAction("Change favorite status"))
            {
                return;
            }

            VaultEntry? entry = GetSelectedEntry();

            if (entry == null)
            {
                MessageBox.Show("Select an entry first.");
                return;
            }

            entry.IsFavorite = !entry.IsFavorite;
            entry.UpdatedAt = DateTime.UtcNow;

            VaultEntry changedEntry = entry;

            RefreshVaultList();

            int visibleIndex = visibleVaultEntries.IndexOf(changedEntry);

            if (visibleIndex >= 0 && visibleIndex < vaultListBox.Items.Count)
            {
                vaultListBox.SelectedIndex = visibleIndex;
            }

            UpdateFavoriteButtonText();

            string reason = entry.IsFavorite
                ? "Added to favorites locally: " + entry.GetDisplayName()
                : "Removed from favorites locally: " + entry.GetDisplayName();

            selectedPreviewLabel.Text =
                reason + Environment.NewLine +
                "Cloud sync is running in the background.";

            SetSyncStatus("Queued background sync");

            QueueBackgroundVaultSync(reason);
        }

        private void UpdateFavoriteButtonText()
        {
            VaultEntry? entry = GetSelectedEntry();

            if (entry == null)
            {
                favoriteButton.Text = "\u2606 Favorite";
                favoriteButton.BackColor = Color.FromArgb(35, 40, 60);
                return;
            }

            if (entry.IsFavorite)
            {
                favoriteButton.Text = "\u2605 Favorited";
                favoriteButton.BackColor = Color.FromArgb(120, 85, 35);
            }
            else
            {
                favoriteButton.Text = "\u2606 Favorite";
                favoriteButton.BackColor = Color.FromArgb(35, 40, 60);
            }
        }
        private void DeleteEntryButton_Click(object? sender, EventArgs e)
        {
            if (!RequireTrustedDeviceForSensitiveAction("Delete entry"))
            {
                return;
            }

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

            string deletedName = entry.GetDisplayName();

            NormalizeVaultEntryForSync(entry);
            AddDeletedEntryTombstone(entry, deletedName);

            vaultEntries.Remove(entry);

            if (editingEntry == entry)
            {
                ClearEntryInputs();
                SetEntryEditMode(false);
            }

            RefreshVaultList();

            selectedPreviewLabel.Text =
                "Deleted locally: " + deletedName + Environment.NewLine +
                "Cloud sync is running in the background." + Environment.NewLine +
                "If sync fails, QuickForge will retry automatically.";

            ShowEmptyVaultOnboardingIfNeeded();
            SetSyncStatus("Delete pending");

            QueueBackgroundVaultSync("Deleted locally: " + deletedName);
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
            if (!RequireTrustedDeviceForSensitiveAction("Rotate recovery key"))             {                 return;             } 
            await RotateRecoveryKeyAsync();
        }


        private void ShowRecoveryKeyRotationProgress(string mainText, string detailText)
        {
            SetSyncStatus("Rotating recovery key...");

            if (vaultAccessPanel.Visible)
            {
                vaultUnlockStatusLabel.Visible = true;
                vaultUnlockStatusLabel.Text =
                    mainText + Environment.NewLine +
                    detailText;

                vaultUnlockStatusLabel.ForeColor = Color.FromArgb(255, 190, 90);
                vaultUnlockStatusLabel.Refresh();
                vaultAccessPanel.Refresh();
            }

            if (vaultPanel.Visible)
            {
                SetPreviewText(
                    mainText,
                    detailText,
                    "Please wait."
                );

                vaultPanel.Refresh();
            }

            if (!string.IsNullOrWhiteSpace(connectedGoogleEmail))
            {
                accountStatusLabel.Text = "Securing recovery key...";
                accountStatusLabel.ForeColor = Color.FromArgb(200, 210, 255);
                accountStatusLabel.Refresh();
            }

            Application.DoEvents();
        }

        private void RestoreConnectedAccountStatus()
        {
            if (!string.IsNullOrWhiteSpace(connectedGoogleEmail))
            {
                accountStatusLabel.Text = "Connected: " + connectedGoogleEmail;
                accountStatusLabel.ForeColor = successColor;
            }
        }
        private async Task<bool> RotateRecoveryKeyAsync()
        {
            if (currentDataKey == null || currentEncryptedVaultFile == null)
            {
                MessageBox.Show("Vault is not ready.");
                return false;
            }

            string newRecoveryKey = VaultCryptoService.GenerateRecoveryKey();

            bool confirmed = ShowRecoveryKeyRotationDialog(newRecoveryKey);

            if (!confirmed)
            {
                return false;
            }

            bool previousCreateEnabled = createVaultButton.Enabled;
            bool previousImportEnabled = importBackupAccessButton.Enabled;
            bool previousResetEnabled = resetTestVaultButton.Enabled;

            try
            {
                createVaultButton.Enabled = false;
                importBackupAccessButton.Enabled = false;
                resetTestVaultButton.Enabled = false;
                UseWaitCursor = true;
                Cursor = Cursors.WaitCursor;

                ShowRecoveryKeyRotationProgress(
                    "New recovery key confirmed.",
                    "Encrypting and saving the new recovery key..."
                );

                await Task.Yield();

                await Task.Run(() =>
                {
                    VaultCryptoService.RotateRecoveryKey(
                        currentEncryptedVaultFile,
                        currentDataKey,
                        newRecoveryKey
                    );
                });

                currentVaultSettings.LastRecoveryKeyRotatedAt = DateTime.UtcNow;
                currentVaultSettings.RecoveryKeyRotationRequired = false;

                ShowRecoveryKeyRotationProgress(
                    "New recovery key encrypted.",
                    "Syncing it safely to Google Drive..."
                );

                await SaveCurrentVaultToCloudAsync();

                ShowRecoveryKeyRotationProgress(
                    "Recovery key rotation complete.",
                    "Opening your vault..."
                );

                await Task.Delay(250);

                selectedPreviewLabel.Text = "Recovery key rotated and synced.";
                MessageBox.Show("Recovery key rotated successfully. The old recovery key no longer works.");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not rotate recovery key: " + ex.Message);
                return false;
            }
            finally
            {
                UseWaitCursor = false;
                Cursor = Cursors.Default;

                createVaultButton.Enabled = previousCreateEnabled;
                importBackupAccessButton.Enabled = previousImportEnabled;
                resetTestVaultButton.Enabled = previousResetEnabled;

                RestoreConnectedAccountStatus();
            }
        }

        private bool ShowFirstRecoveryKeyDialog(string recoveryKey)
        {
            bool copied = false;
            bool confirmed = false;

            using (Form dialog = new Form())
            {
                dialog.Width = 560;
                dialog.Height = 390;
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
                dialog.Width = 560;
                dialog.Height = 370;
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

        private void ShowEmergencyBackupGuidance()
        {
            SetPreviewText(
                "Vault created successfully.",
                "Before storing important data, export an encrypted backup.",
                "Keep your recovery key and backup file in safe, separate places.",
                "This beta has passed local account-isolation, multi-device sync, backup, restore, lockout, recovery-key, and conflict-merge tests. It has not received an external security audit."
            );

            backupButton.BackColor = Color.FromArgb(45, 90, 160);
            backupButton.FlatAppearance.BorderColor = borderColor;

            MessageBox.Show(
                "Your vault was created successfully." + Environment.NewLine + Environment.NewLine +
                "Before storing important data:" + Environment.NewLine +
                "1. Export an encrypted backup." + Environment.NewLine +
                "2. Save your recovery key somewhere safe." + Environment.NewLine +
                "3. Keep the backup and recovery key in separate places." + Environment.NewLine + Environment.NewLine +
                "The backup is encrypted, but you still need your vault code or recovery key to open it.",
                "Emergency backup recommended",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
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

        private string GetDefaultEncryptedBackupFolder()
        {
            string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (string.IsNullOrWhiteSpace(documentsFolder))
            {
                documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            }

            string backupFolder = Path.Combine(documentsFolder, "QuickForge Encrypted Backups");
            Directory.CreateDirectory(backupFolder);

            return backupFolder;
        }

        private string CreateDefaultEncryptedBackupFileName()
        {
            return "QuickForge-Encrypted-Backup-" + DateTime.Now.ToString("dd-MMMM-yyyy_'at'_HH'h'mm", System.Globalization.CultureInfo.InvariantCulture) + ".qfvault";
        }

        private void OpenFolderInExplorer(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open backup folder: " + ex.Message);
            }
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
                string defaultBackupFolder = GetDefaultEncryptedBackupFolder();

                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    saveDialog.Title = "Export encrypted QuickForge backup";
                    saveDialog.InitialDirectory = defaultBackupFolder;
                    saveDialog.FileName = CreateDefaultEncryptedBackupFileName();
                    saveDialog.DefaultExt = "qfvault";
                    saveDialog.AddExtension = true;
                    saveDialog.OverwritePrompt = true;
                    saveDialog.Filter = "QuickForge encrypted backup (*.qfvault)|*.qfvault|All files (*.*)|*.*";

                    if (saveDialog.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    string encryptedJson = CreateCurrentEncryptedVaultJson();
                    File.WriteAllText(saveDialog.FileName, encryptedJson, Encoding.UTF8);

                    string backupFileName = Path.GetFileName(saveDialog.FileName);
                    string backupFolder = Path.GetDirectoryName(saveDialog.FileName) ?? defaultBackupFolder;

                    MarkBackupCreatedByCurrentDevice(backupFileName);
                    QueueBackgroundVaultSync("Encrypted backup exported: " + backupFileName);

                    SetPreviewText(
                        "Encrypted backup exported.",
                        "File: " + backupFileName,
                        "Folder: " + backupFolder,
                        "You still need your vault code or recovery key to restore it."
                    );

                    backupButton.BackColor = Color.FromArgb(35, 40, 60);
                    backupButton.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);

                    DialogResult openFolder = MessageBox.Show(
                        "Encrypted backup exported successfully." + Environment.NewLine + Environment.NewLine +
                        "Saved as:" + Environment.NewLine +
                        backupFileName + Environment.NewLine + Environment.NewLine +
                        "Default backup folder:" + Environment.NewLine +
                        backupFolder + Environment.NewLine + Environment.NewLine +
                        "Anyone can delete this backup file from Windows, but they cannot read your passwords without your vault code or recovery key." + Environment.NewLine +
                        "Do not store your recovery key and backup file in the exact same place." + Environment.NewLine + Environment.NewLine +
                        "Open backup folder now?",
                        "Backup exported",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information
                    );

                    if (openFolder == DialogResult.Yes)
                    {
                        OpenFolderInExplorer(backupFolder);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not export backup: " + ex.Message);
            }
        }

        private void ShowImportBackupFailureMessage(Exception ex, string selectedBackupPath)
        {
            string fileName = string.IsNullOrWhiteSpace(selectedBackupPath)
                ? "Unknown"
                : Path.GetFileName(selectedBackupPath);

            SetSyncStatus("Import failed", error: true);

            SetPreviewText(
                "Backup import failed.",
                "The selected backup could not be restored.",
                "Try your recovery key, check that this is a QuickForge backup, or choose another backup file."
            );

            MessageBox.Show(
                "QuickForge could not import this encrypted backup." + Environment.NewLine + Environment.NewLine +
                "Selected file: " + fileName + Environment.NewLine + Environment.NewLine +
                "Possible reasons:" + Environment.NewLine +
                "- Wrong vault code or recovery key for this backup" + Environment.NewLine +
                "- The selected file is not a QuickForge encrypted backup" + Environment.NewLine +
                "- The backup file is damaged, incomplete, or corrupted" + Environment.NewLine +
                "- Google Drive could not be reached while replacing the cloud vault" + Environment.NewLine + Environment.NewLine +
                "What to try:" + Environment.NewLine +
                "1. Select the original .qfvault backup file." + Environment.NewLine +
                "2. Try the recovery key if the vault code does not work." + Environment.NewLine +
                "3. Try another encrypted backup if this file may be damaged." + Environment.NewLine +
                "4. Check your Google connection and try again." + Environment.NewLine + Environment.NewLine +
                "Technical detail:" + Environment.NewLine +
                ex.Message,
                "Backup import failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        private (
            VaultData VaultData,
            byte[] DataKey,
            EncryptedVaultFile EncryptedVaultFile,
            string EncryptedJsonToUpload,
            bool PreservedCurrentVaultCredentials
        ) DecryptBackupForImport(string encryptedJson, string unlockCode)
        {
            VaultData? importedVaultData = null;
            byte[]? importedDataKey = null;
            EncryptedVaultFile? importedEncryptedVaultFile = null;

            bool decrypted =
                VaultCryptoService.TryDecryptVaultWithVaultCode(
                    encryptedJson,
                    unlockCode,
                    out importedVaultData,
                    out importedDataKey,
                    out importedEncryptedVaultFile
                );

            if (!decrypted)
            {
                decrypted =
                    VaultCryptoService.TryDecryptVaultWithRecoveryKey(
                        encryptedJson,
                        unlockCode,
                        out importedVaultData,
                        out importedDataKey,
                        out importedEncryptedVaultFile
                    );
            }

            if (!decrypted &&
                isVaultUnlocked &&
                currentDataKey != null)
            {
                try
                {
                    importedVaultData = VaultCryptoService.DecryptVaultWithExistingDataKey(
                        encryptedJson,
                        currentDataKey,
                        out importedEncryptedVaultFile
                    );

                    importedDataKey = currentDataKey;
                    decrypted = true;
                }
                catch
                {
                    importedVaultData = null;
                    importedDataKey = null;
                    importedEncryptedVaultFile = null;
                    decrypted = false;
                }
            }

            if (!decrypted ||
                importedVaultData == null ||
                importedDataKey == null ||
                importedEncryptedVaultFile == null)
            {
                throw new CryptographicException("Wrong vault code or recovery key.");
            }

            NormalizeVaultEntriesForSync(importedVaultData.Entries);

            if (isVaultUnlocked &&
                currentDataKey != null &&
                currentEncryptedVaultFile != null)
            {
                string reEncryptedJson = VaultCryptoService.EncryptVaultDataWithExistingKeys(
                    importedVaultData,
                    currentDataKey,
                    currentEncryptedVaultFile
                );

                EncryptedVaultFile reEncryptedVaultFile =
                    System.Text.Json.JsonSerializer.Deserialize<EncryptedVaultFile>(reEncryptedJson)
                    ?? throw new InvalidOperationException("Imported vault could not be prepared for upload.");

                return (
                    importedVaultData,
                    currentDataKey,
                    reEncryptedVaultFile,
                    reEncryptedJson,
                    true
                );
            }

            return (
                importedVaultData,
                importedDataKey,
                importedEncryptedVaultFile,
                encryptedJson,
                false
            );
        }
        private async Task ImportEncryptedBackupAsync()
        {
            
            if (isVaultUnlocked &&
                !RequireTrustedDeviceForSensitiveAction("Import encrypted backup"))
            {
                return;
            }
if (currentDriveService == null)
            {
                MessageBox.Show(
                    "Connect Google first, then import the backup." + Environment.NewLine + Environment.NewLine +
                    "The backup will be verified locally, then uploaded to the connected Google account as the new encrypted cloud vault.",
                    "Google required for restore",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
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

                    if (string.IsNullOrWhiteSpace(encryptedJson))
                    {
                        throw new InvalidDataException("The selected backup file is empty.");
                    }

                    string? unlockCode = ShowPasswordPrompt(
                        "Import Backup",
                        "Enter the vault code or recovery key for this backup:"
                    );

                    if (string.IsNullOrWhiteSpace(unlockCode))
                    {
                        selectedPreviewLabel.Text = "Import cancelled.";
                        return;
                    }

                    var importResult = DecryptBackupForImport(encryptedJson, unlockCode);

                    VaultData importedVaultData = importResult.VaultData;
                    byte[] importedDataKey = importResult.DataKey;
                    EncryptedVaultFile importedEncryptedVaultFile = importResult.EncryptedVaultFile;

                    bool importConfirmed = ShowImportBackupPreviewDialog(importedVaultData);

                    if (!importConfirmed)
                    {
                        selectedPreviewLabel.Text = "Import cancelled.";
                        return;
                    }

                    SetSyncStatus("Restoring backup...");

                    await GoogleDriveVaultService.UploadEncryptedVaultAsync(
                        currentDriveService,
                        importResult.EncryptedJsonToUpload
                    );

                    lastCloudSaveUtc = DateTime.UtcNow;
                    SetSyncStatus("Active", success: true);

                    if (!isVaultUnlocked || string.IsNullOrWhiteSpace(vaultCode))
                    {
                        vaultCode = unlockCode;
                    }

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

                    SetPreviewText(
                        "Encrypted backup imported successfully.",
                        "The backup was verified, loaded into the app, and uploaded to Google Drive.",
                        importResult.PreservedCurrentVaultCredentials
                            ? "Your current vault code and recovery-key setup were preserved."
                            : "Cloud vault replaced for: " + GetConnectedGoogleEmailDisplay()
                    );

                    MessageBox.Show(
                        "Encrypted backup imported successfully." + Environment.NewLine + Environment.NewLine +
                        "The backup was verified, loaded into QuickForge, and uploaded to Google Drive as the current encrypted cloud vault." + Environment.NewLine + Environment.NewLine +
                        (importResult.PreservedCurrentVaultCredentials
                            ? "Your current vault code and recovery-key setup were preserved."
                            : "Account: " + GetConnectedGoogleEmailDisplay()),
                        "Backup restored",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                catch (Exception ex)
                {
                    ShowImportBackupFailureMessage(ex, openDialog.FileName);
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
                dialog.Width = 600;
                dialog.Height = 430;
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
                    "Only import encrypted QuickForge backup files you trust." +
                    Environment.NewLine +
                    "Importing this backup will replace your current cloud vault after upload." +
                    Environment.NewLine +
                    "Cancel now if this is not the backup you expected.";
                warningLabel.Left = 22;
                warningLabel.Top = 190;
                warningLabel.Width = 540;
                warningLabel.Height = 80;
                warningLabel.ForeColor = Color.FromArgb(255, 190, 90);
                warningLabel.BackColor = Color.Transparent;
                warningLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);

                Button cancelButton = new Button();
                cancelButton.Text = "Cancel";
                cancelButton.Left = 315;
                cancelButton.Top = 320;
                cancelButton.Width = 95;
                cancelButton.Height = 34;
                cancelButton.DialogResult = DialogResult.Cancel;
                StyleActionButton(cancelButton);

                Button importButton = new Button();
                importButton.Text = "Import and replace vault";
                importButton.Left = 425;
                importButton.Top = 320;
                importButton.Width = 145;
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
                dialog.Width = 560;
                dialog.Height = 600;
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
                statusBox.Width = 500;
                statusBox.Height = 345;
                statusBox.Multiline = true;
                statusBox.ReadOnly = true;
                statusBox.WordWrap = true;
                statusBox.ScrollBars = ScrollBars.Vertical;
                statusBox.TabStop = false;
                statusBox.BackColor = Color.FromArgb(24, 28, 44);
                statusBox.ForeColor = Color.White;
                statusBox.BorderStyle = BorderStyle.FixedSingle;

                statusBox.Text =
                    BuildVaultSafetyReport(totalEntries, weakPasswords, reusedPasswordEntries, missingWebsiteLinks) +
                    Environment.NewLine + Environment.NewLine +
                    "Vault: " + (isVaultUnlocked ? "Unlocked" : "Locked") + Environment.NewLine +
                    "Google sync: " + (currentDriveService != null ? "Connected" : "Not connected") + Environment.NewLine +
                    "Cloud vault storage: App-managed Google Drive appDataFolder" + Environment.NewLine +
                    "Cloud vault status: " + (cloudVaultExists ? "Detected" : "Cloud vault missing. Restore from encrypted backup or create a new vault.") + Environment.NewLine +
                    "Auto-lock: " + autoLockText + Environment.NewLine +
                    "Clipboard cleanup: Active" + Environment.NewLine +
                    Environment.NewLine +
                    "Saved entries: " + totalEntries + Environment.NewLine +
                    "Favorites: " + favoriteEntries + Environment.NewLine +
                    "Weak passwords: " + weakPasswords + Environment.NewLine +
                    "Reused passwords: " + reusedPasswordEntries + Environment.NewLine +
                    "Missing website links: " + missingWebsiteLinks + Environment.NewLine +
                    "Recovery key reminder: " + recoveryReminderText + Environment.NewLine +
                    Environment.NewLine +
                    "Real-data status: Controlled personal beta use supported" + Environment.NewLine +
                    "Vault files: Not meant to be opened directly. Use QuickForge to unlock, export, import, or restore." + Environment.NewLine +
                    Environment.NewLine +
                    "Completed:" + Environment.NewLine +
                    "- Strong vault code policy" + Environment.NewLine +
                    "- Recovery key" + Environment.NewLine +
                    "- Encrypted backup" + Environment.NewLine +
                    "- Cloud conflict protection" + Environment.NewLine +
                    "- App-managed Google Drive vault storage" + Environment.NewLine +
                    "- Manual Sync and Refresh" + Environment.NewLine +
                    Environment.NewLine +
                    "Still required:" + Environment.NewLine +
                    "- Repeated multi-device testing" + Environment.NewLine +
                    "- Fresh install restore testing" + Environment.NewLine +
                    "- External code/security review" + Environment.NewLine +
                    "- Installer/signing decision";

                Label adviceLabel = new Label();
                adviceLabel.Left = 20;
                adviceLabel.Top = 450;
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


                Button deviceTrustButton = new Button();
                deviceTrustButton.Text = "Device Trust";
                deviceTrustButton.Left = 90;
                deviceTrustButton.Top = 500;
                deviceTrustButton.Width = 130;
                deviceTrustButton.Height = 32;
                StyleActionButton(deviceTrustButton);
                deviceTrustButton.Enabled = true;
                deviceTrustButton.Click += (s, e) => ShowDeviceTrustDialog();

                Button selfCheckButton = new Button();
                selfCheckButton.Text = "Vault self-check";
                selfCheckButton.Left = 235;
                selfCheckButton.Top = 500;
                selfCheckButton.Width = 130;
                selfCheckButton.Height = 32;
                StyleActionButton(selfCheckButton);
                selfCheckButton.Click += (s, e) => ShowVaultSelfCheckDialog();

                Button closeButton = new Button();
                closeButton.Text = "Close";
                closeButton.Left = 380;
                closeButton.Top = 500;
                closeButton.Width = 100;
                closeButton.Height = 32;
                StyleActionButton(closeButton, true);
                closeButton.Click += (s, e) => dialog.Close();

                dialog.Controls.Add(titleLabel);
                dialog.Controls.Add(summaryLabel);
                dialog.Controls.Add(statusBox);
                dialog.Controls.Add(adviceLabel);
                dialog.Controls.Add(deviceTrustButton);
                dialog.Controls.Add(selfCheckButton);
                dialog.Controls.Add(closeButton);


                dialog.Shown += (s, e) =>
                {
                    statusBox.SelectionStart = 0;
                    statusBox.SelectionLength = 0;
                    closeButton.Focus();
                };

                dialog.ShowDialog(this);
            }
        }


        private bool ConfirmVaultCodeForDeviceTrust()
        {
            if (string.IsNullOrWhiteSpace(vaultCode))
            {
                MessageBox.Show(
                    "Device trust changes require the vault code." + Environment.NewLine + Environment.NewLine +
                    "Unlock the vault with your vault code first. Recovery-key-only access is not enough for this action.",
                    "Vault code required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return false;
            }

            string? enteredVaultCode = ShowPasswordPrompt(
                "Device Trust",
                "Enter your vault code to manage trusted devices:"
            );

            if (string.IsNullOrWhiteSpace(enteredVaultCode))
            {
                return false;
            }

            if (enteredVaultCode.StartsWith("QF-", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "Recovery keys cannot be used to manage trusted devices." + Environment.NewLine + Environment.NewLine +
                    "Use your normal vault code for this action.",
                    "Vault code required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return false;
            }

            if (!string.Equals(enteredVaultCode, vaultCode, StringComparison.Ordinal))
            {
                MessageBox.Show(
                    "The vault code did not match. Device trust was not changed.",
                    "Wrong vault code",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return false;
            }

            return true;
        }

        private void ShowDeviceTrustDialog()
        {
            EnsureLocalDeviceIdentity();
            EnsureVaultSafetyCollections();
            RegisterCurrentDeviceForVault(false);

            using (Form dialog = new Form())
            {
                dialog.Width = 720;
                dialog.Height = 570;
                dialog.Text = "Device Trust";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.BackColor = Color.FromArgb(16, 20, 34);

                Label titleLabel = new Label();
                titleLabel.Text = "Device Trust";
                titleLabel.Left = 20;
                titleLabel.Top = 18;
                titleLabel.Width = 650;
                titleLabel.Height = 28;
                titleLabel.ForeColor = Color.White;
                titleLabel.BackColor = Color.Transparent;
                titleLabel.Font = new Font("Segoe UI", 14, FontStyle.Bold);

                Label introLabel = new Label();
                introLabel.Text =
                    "Review devices that have opened or synced this vault." + Environment.NewLine +
                    "First device is trusted automatically. New devices start untrusted until approved from a trusted device.";
                introLabel.Left = 20;
                introLabel.Top = 52;
                introLabel.Width = 660;
                introLabel.Height = 42;
                introLabel.ForeColor = softTextColor;
                introLabel.BackColor = Color.Transparent;
                introLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);

                ListBox deviceList = new ListBox();
                deviceList.Left = 20;
                deviceList.Top = 105;
                deviceList.Width = 660;
                deviceList.Height = 190;
                deviceList.BackColor = Color.FromArgb(24, 28, 44);
                deviceList.ForeColor = Color.White;
                deviceList.BorderStyle = BorderStyle.FixedSingle;
                deviceList.Font = new Font("Consolas", 9, FontStyle.Regular);

                Label detailLabel = new Label();
                detailLabel.Left = 20;
                detailLabel.Top = 305;
                detailLabel.Width = 660;
                detailLabel.Height = 75;
                detailLabel.ForeColor = softTextColor;
                detailLabel.BackColor = Color.Transparent;
                detailLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);

                void RefreshDeviceList()
                {
                    List<KnownVaultDevice> devices = currentVaultSettings.KnownDevices
                        .OrderByDescending(device => device.LastSeenAtUtc)
                        .ToList();

                    deviceList.Items.Clear();
                    deviceList.Tag = devices;

                    foreach (KnownVaultDevice device in devices)
                    {
                        string trustText = device.IsTrusted ? "TRUSTED" : "UNTRUSTED";
                        string currentText = device.DeviceId == localDeviceId ? " | THIS DEVICE" : "";

                        deviceList.Items.Add(
                            trustText.PadRight(10) +
                            " | " + device.DeviceName.PadRight(22) +
                            " | last seen " + device.LastSeenAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm") +
                            currentText
                        );
                    }

                    if (devices.Count > 0 && deviceList.SelectedIndex < 0)
                    {
                        deviceList.SelectedIndex = 0;
                    }

                    if (devices.Count == 0)
                    {
                        detailLabel.Text = "No known devices recorded yet.";
                    }
                }

                KnownVaultDevice? GetSelectedDevice()
                {
                    if (deviceList.Tag is not List<KnownVaultDevice> devices)
                    {
                        return null;
                    }

                    if (deviceList.SelectedIndex < 0 || deviceList.SelectedIndex >= devices.Count)
                    {
                        return null;
                    }

                    return devices[deviceList.SelectedIndex];
                }

                void UpdateDetail()
                {
                    KnownVaultDevice? selected = GetSelectedDevice();

                    if (selected == null)
                    {
                        detailLabel.Text = "Select a device to view details.";
                        return;
                    }

                    string shortId = selected.DeviceId.Length <= 10
                        ? selected.DeviceId
                        : selected.DeviceId.Substring(0, 10);

                    detailLabel.Text =
                        "Device: " + selected.DeviceName + Environment.NewLine +
                        "Status: " + (selected.IsTrusted ? "Trusted" : "Untrusted") + Environment.NewLine +
                        "First seen: " + selected.FirstSeenAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm") +
                        " | Last seen: " + selected.LastSeenAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + Environment.NewLine +
                        "Sync count: " + selected.SyncCount +
                        " | ID: " + shortId +
                        (selected.DeviceId == localDeviceId ? " | This device" : "");
                }

                deviceList.SelectedIndexChanged += (s, e) => UpdateDetail();

                Button trustButton = new Button();
                Button untrustButton = new Button();
                trustButton.Text = "Trust";
                trustButton.Left = 20;
                trustButton.Top = 395;
                trustButton.Width = 110;
                trustButton.Height = 34;
                StyleActionButton(trustButton, true);
                trustButton.Click += (s, e) =>
                {
                    KnownVaultDevice? selected = GetSelectedDevice();

                    if (selected == null)
                    {
                        return;
                    }

                    if (!RequireTrustedDeviceForSensitiveAction("Manage Device Trust"))
                    {
                        return;
                    }

                    if (!ConfirmVaultCodeForDeviceTrust())
                    {
                        return;
                    }

                    selected.IsTrusted = true;
                    selected.IsHiddenFromTrustList = false;
                    selected.RemovedFromTrustListAtUtc = null;
                    selected.TrustedChangedAtUtc = DateTime.UtcNow;
                    selected.TrustNote = "Trusted manually from " + localDeviceName + ".";

                    AddSafetyTimelineEvent(
                        "Device trusted",
                        selected.DeviceName + " was marked as trusted from " + localDeviceName + "."
                    );

                    QueueBackgroundVaultSync("Device trust updated.");

                    RefreshDeviceList();
                    UpdateDetail();
                    UpdateDeviceTrustActionButtons();
                };

                untrustButton.Text = "Untrust";
                untrustButton.Left = 145;
                untrustButton.Top = 395;
                untrustButton.Width = 110;
                untrustButton.Height = 34;
                StyleActionButton(untrustButton);
                untrustButton.Click += (s, e) =>
                {
                    KnownVaultDevice? selected = GetSelectedDevice();

                    if (selected == null)
                    {
                        return;
                    }

                    if (!RequireTrustedDeviceForSensitiveAction("Manage Device Trust"))
                    {
                        return;
                    }

                    if (!ConfirmVaultCodeForDeviceTrust())
                    {
                        return;
                    }

                    selected.IsTrusted = false;
                    selected.IsHiddenFromTrustList = false;
                    selected.RemovedFromTrustListAtUtc = null;
                    selected.TrustedChangedAtUtc = DateTime.UtcNow;
                    selected.TrustNote = "Marked untrusted from " + localDeviceName + ".";

                    AddSafetyTimelineEvent(
                        "Device untrusted",
                        selected.DeviceName + " was marked as untrusted from " + localDeviceName + "."
                    );

                    QueueBackgroundVaultSync("Device trust updated.");

                    RefreshDeviceList();
                    UpdateDetail();
                    UpdateDeviceTrustActionButtons();
                };
                void UpdateDeviceTrustActionButtons()
                {
                    KnownVaultDevice? selected = GetSelectedDevice();
                    bool canManageDeviceTrust = !IsRestrictedModeActive();

                    trustButton.Enabled = canManageDeviceTrust && selected != null && !selected.IsTrusted;
                    untrustButton.Enabled = canManageDeviceTrust && selected != null && selected.IsTrusted;
                }

                deviceList.SelectedIndexChanged += (s, e) => UpdateDeviceTrustActionButtons();

                Button closeButton = new Button();
                closeButton.Text = "Close";
                closeButton.Left = 570;
                closeButton.Top = 395;
                closeButton.Width = 110;
                closeButton.Height = 34;
                StyleActionButton(closeButton, true);
                closeButton.Click += (s, e) => dialog.Close();

                Label warningLabel = new Label();
                warningLabel.Left = 20;
                warningLabel.Top = 440;
                warningLabel.Width = 660;
                warningLabel.Height = 42;
                warningLabel.ForeColor = Color.FromArgb(255, 190, 90);
                warningLabel.BackColor = Color.Transparent;
                warningLabel.Font = new Font("Segoe UI", 8, FontStyle.Regular);
                warningLabel.Text = IsRestrictedModeActive()
                    ? "Device Trust is read-only on this untrusted device. To regain full access, approve this device from another trusted device. If this device is suspicious, check your Google Account security first."
                    : "Trusted devices have full vault access and can approve other devices. Only trust devices you own and control. If you see an unknown device, untrust it, check your Google Account security, then rotate your QuickForge vault code and recovery key.";

                dialog.Controls.Add(titleLabel);
                dialog.Controls.Add(introLabel);
                dialog.Controls.Add(deviceList);
                dialog.Controls.Add(detailLabel);
                dialog.Controls.Add(trustButton);
                dialog.Controls.Add(untrustButton);
                dialog.Controls.Add(closeButton);
                dialog.Controls.Add(warningLabel);

                RefreshDeviceList();
                    UpdateDetail();
                    UpdateDeviceTrustActionButtons();

                dialog.ShowDialog(this);
            }
        }
        private void ShowVaultSelfCheckDialog()
        {
            List<string> passed = new List<string>();
            List<string> warnings = new List<string>();

            void Pass(string message)
            {
                passed.Add("PASS: " + message);
            }

            void Warn(string message)
            {
                warnings.Add("CHECK: " + message);
            }

            if (isVaultUnlocked)
            {
                Pass("Vault is unlocked.");
            }
            else
            {
                Warn("Vault is locked. Unlock it before trusting the current state.");
            }

            if (currentDriveService != null)
            {
                Pass("Google Drive is connected.");
            }
            else
            {
                Warn("Google Drive is not connected.");
            }

            if (currentDataKey != null)
            {
                Pass("Vault data key is available in memory for this unlocked session.");
            }
            else
            {
                Warn("Vault data key is missing.");
            }

            if (currentEncryptedVaultFile != null)
            {
                Pass("Encrypted vault wrapper is loaded.");
            }
            else
            {
                Warn("Encrypted vault wrapper is missing.");
            }

            if (currentEncryptedVaultFile?.RecoveryKeyWrapper != null)
            {
                Pass("Recovery key wrapper exists.");
            }
            else
            {
                Warn("Recovery key wrapper is missing.");
            }

            if (lastCloudSaveUtc.HasValue)
            {
                Pass("Last cloud save timestamp exists: " + lastCloudSaveUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else
            {
                Warn("No last cloud save timestamp yet. Press Sync after testing.");
            }

            if (!string.IsNullOrWhiteSpace(lastKnownCloudFingerprint))
            {
                Pass("Cloud fingerprint is known for conflict detection.");
            }
            else
            {
                Warn("Cloud fingerprint is not known yet. Refresh or Sync once after unlock.");
            }

            if (currentDataKey != null && currentEncryptedVaultFile != null)
            {
                try
                {
                    string wrapperJson = System.Text.Json.JsonSerializer.Serialize(currentEncryptedVaultFile);

                    EncryptedVaultFile? wrapperClone =
                        System.Text.Json.JsonSerializer.Deserialize<EncryptedVaultFile>(wrapperJson);

                    if (wrapperClone == null)
                    {
                        throw new InvalidOperationException("Could not clone encrypted vault wrapper.");
                    }

                    VaultData checkData = new VaultData
                    {
                        Entries = new List<VaultEntry>(vaultEntries),
                        Settings = currentVaultSettings,
                        UpdatedAt = DateTime.UtcNow
                    };

                    string encryptedJson = VaultCryptoService.EncryptVaultDataWithExistingKeys(
                        checkData,
                        currentDataKey,
                        wrapperClone
                    );

                    if (string.IsNullOrWhiteSpace(encryptedJson))
                    {
                        Warn("Encryption self-check returned empty encrypted JSON.");
                    }
                    else
                    {
                        Pass("Vault can be serialized and encrypted.");
                    }

                    List<string> leakedPlatforms = vaultEntries
                        .Where(entry =>
                            !string.IsNullOrWhiteSpace(entry.Secret) &&
                            entry.Secret.Length >= 4 &&
                            encryptedJson.Contains(entry.Secret, StringComparison.Ordinal)
                        )
                        .Select(entry => string.IsNullOrWhiteSpace(entry.Platform) ? "Unnamed entry" : entry.Platform)
                        .Take(5)
                        .ToList();

                    if (leakedPlatforms.Count == 0)
                    {
                        Pass("Encrypted JSON does not contain plaintext secrets.");
                    }
                    else
                    {
                        Warn("Encrypted JSON appears to contain plaintext secret text for: " + string.Join(", ", leakedPlatforms));
                    }
                }
                catch (Exception ex)
                {
                    Warn("Serialization/encryption self-check failed: " + ex.Message);
                }
            }
            else
            {
                Warn("Encryption self-check skipped because vault crypto state is incomplete.");
            }

            Warn("Encrypted backup existence cannot be verified automatically. Export a fresh encrypted backup before real data.");

            string title = warnings.Count == 1
                ? "Vault self-check passed with reminder"
                : "Vault self-check needs attention";

            string report =
                title + Environment.NewLine + Environment.NewLine +
                "Passed checks:" + Environment.NewLine +
                (passed.Count == 0 ? "- None" : "- " + string.Join(Environment.NewLine + "- ", passed)) +
                Environment.NewLine + Environment.NewLine +
                "Warnings / reminders:" + Environment.NewLine +
                (warnings.Count == 0 ? "- None" : "- " + string.Join(Environment.NewLine + "- ", warnings)) +
                Environment.NewLine + Environment.NewLine +
                "QuickForge Sync has passed local controlled personal beta readiness tests." + Environment.NewLine +
                "Local readiness tests have passed for controlled personal beta use. QuickForge has not received an external security audit.";

            MessageBox.Show(
                report,
                "Vault self-check",
                MessageBoxButtons.OK,
                warnings.Count <= 1 ? MessageBoxIcon.Information : MessageBoxIcon.Warning
            );
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
            VaultEntry? previouslySelectedEntry = GetSelectedEntry();
            string searchText = vaultSearchTextBox.Text.Trim();

            IEnumerable<VaultEntry> entriesToShow = vaultEntries;

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                entriesToShow = entriesToShow.Where(entry =>
                    entry.GetDisplayName().Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (entry.Platform ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (entry.Username ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (entry.Website ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (entry.Note ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase)
                );
            }

            List<VaultEntry> sortedEntries = entriesToShow
                .OrderByDescending(entry => entry.IsFavorite)
                .ThenBy(entry => entry.GetDisplayName(), StringComparer.CurrentCultureIgnoreCase)
                .ThenByDescending(entry => entry.UpdatedAt)
                .ToList();

            vaultListBox.BeginUpdate();

            try
            {
                vaultListBox.Items.Clear();
                visibleVaultEntries.Clear();

                foreach (VaultEntry entry in sortedEntries)
                {
                    visibleVaultEntries.Add(entry);

                    string listText = entry.IsFavorite
                        ? "\u2605 " + entry.GetDisplayName()
                        : "  " + entry.GetDisplayName();

                    vaultListBox.Items.Add(listText);
                }
            }
            finally
            {
                vaultListBox.EndUpdate();
            }

            if (previouslySelectedEntry != null)
            {
                int selectedIndex = visibleVaultEntries.IndexOf(previouslySelectedEntry);

                if (selectedIndex >= 0 && selectedIndex < vaultListBox.Items.Count)
                {
                    vaultListBox.SelectedIndex = selectedIndex;
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

            await Task.Delay(100);

            SetForegroundWindow(quickFillTargetWindow);

            await Task.Delay(150);

            SendKeys.SendWait("^v");

            _ = ClearClipboardLaterAsync(password, 20000);
        }

        private void ShowCreatePasswordDialog(PasswordGeneratorTarget target)
        {
            string currentPassword = "";

            using (Form dialog = new Form())
            {
                dialog.Width = 480;
                dialog.Height = 365;
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
                subtitleLabel.Text = "New password - not saved yet.";
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
                    statusLabel.Text = "New password - not saved yet.";
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

        private Button AttachPasswordVisibilityToggle(Control parent, TextBox targetTextBox)
        {
            const int buttonWidth = 34;
            const int gap = 6;
            const string showIcon = "\U0001F441";
            const string hideIcon = "\U0001F648";

            targetTextBox.UseSystemPasswordChar = true;

            if (targetTextBox.Width > 120)
            {
                targetTextBox.Width -= buttonWidth + gap;
            }

            Button toggleButton = new Button();
            toggleButton.Text = showIcon;
            toggleButton.Left = targetTextBox.Right + gap;
            toggleButton.Top = targetTextBox.Top;
            toggleButton.Width = buttonWidth;
            toggleButton.Height = targetTextBox.Height;
            toggleButton.TabStop = false;
            toggleButton.Cursor = Cursors.Hand;
            toggleButton.FlatStyle = FlatStyle.Flat;
            toggleButton.UseVisualStyleBackColor = false;
            toggleButton.BackColor = Color.FromArgb(35, 40, 60);
            toggleButton.ForeColor = Color.White;
            toggleButton.Font = new Font("Segoe UI Emoji", 9, FontStyle.Regular);
            toggleButton.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 150);
            toggleButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 52, 75);
            toggleButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(25, 30, 48);
            toggleButton.AccessibleName = "Show or hide sensitive text";

            bool isVisible = false;

            toggleButton.Click += (s, e) =>
            {
                isVisible = !isVisible;
                targetTextBox.UseSystemPasswordChar = !isVisible;
                toggleButton.Text = isVisible ? hideIcon : showIcon;

                targetTextBox.Focus();
                targetTextBox.SelectionStart = targetTextBox.Text.Length;
            };

            targetTextBox.VisibleChanged += (s, e) =>
            {
                toggleButton.Visible = targetTextBox.Visible;
            };

            parent.Controls.Add(toggleButton);
            toggleButton.Visible = targetTextBox.Visible;
            toggleButton.BringToFront();

            return toggleButton;
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

            passwordStrengthLabel.Text = "Strength: " + result.Title + " - " + result.Hint;
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

            if (currentVaultSettings.AutoRefreshMinutes == 1)
            {
                autoRefreshComboBox.SelectedIndex = 1;
            }
            else if (currentVaultSettings.AutoRefreshMinutes == 15)
            {
                autoRefreshComboBox.SelectedIndex = 3;
            }
            else if (currentVaultSettings.AutoRefreshMinutes == 30)
            {
                autoRefreshComboBox.SelectedIndex = 4;
            }
            else if (currentVaultSettings.AutoRefreshMinutes <= 0)
            {
                autoRefreshComboBox.SelectedIndex = 0;
            }
            else
            {
                autoRefreshComboBox.SelectedIndex = 2;
                currentVaultSettings.AutoRefreshMinutes = 5;
            }

            ConfigureAutoRefreshTimer();
            UpdateAnimationState();
        }


        private int GetAutoRefreshMinutesFromSelection()
        {
            if (autoRefreshComboBox.SelectedIndex == 1)
            {
                return 1;
            }

            if (autoRefreshComboBox.SelectedIndex == 2)
            {
                return 5;
            }

            if (autoRefreshComboBox.SelectedIndex == 3)
            {
                return 15;
            }

            if (autoRefreshComboBox.SelectedIndex == 4)
            {
                return 30;
            }

            return 0;
        }

        private void ConfigureAutoRefreshTimer()
        {
            autoRefreshTimer.Stop();

            if (!isVaultUnlocked)
            {
                return;
            }

            int minutes = currentVaultSettings.AutoRefreshMinutes;

            if (minutes <= 0)
            {
                return;
            }

            autoRefreshTimer.Interval = Math.Max(1, minutes) * 60 * 1000;
            autoRefreshTimer.Start();
        }

        private async void AutoRefreshTimer_Tick(object? sender, EventArgs e)
        {
            autoRefreshTimer.Stop();

            try
            {
                await RunAutoRefreshAsync();
            }
            finally
            {
                ConfigureAutoRefreshTimer();
            }
        }

        private async Task RunAutoRefreshAsync()
        {
            if (!isVaultUnlocked ||
                currentDriveService == null ||
                currentDataKey == null ||
                currentEncryptedVaultFile == null)
            {
                return;
            }

            if (autoRefreshRunning ||
                backgroundVaultSyncRunning ||
                backgroundVaultSyncRequested ||
                hasUnsyncedLocalChanges ||
                editingEntry != null)
            {
                return;
            }

            autoRefreshRunning = true;

            try
            {
                GoogleDriveVaultMetadata? cloudMetadata =
                    await GoogleDriveVaultService.GetVaultMetadataAsync(currentDriveService);

                if (cloudMetadata == null ||
                    string.IsNullOrWhiteSpace(cloudMetadata.Fingerprint))
                {
                    return;
                }

                bool cloudLooksSame =
                    !string.IsNullOrWhiteSpace(lastKnownCloudFingerprint) &&
                    string.Equals(
                        cloudMetadata.Fingerprint,
                        lastKnownCloudFingerprint,
                        StringComparison.Ordinal
                    );

                SetSyncStatus(cloudLooksSame ? "Auto-refresh checking..." : "Auto-refreshing...");

                await LoadVaultFromCloudAsync();

                RegisterCurrentDeviceForVault(false);
                ApplyRecoverySettingsToUi();
                ApplyPerformanceSettingsToUi();
                ApplyDeviceTrustRestrictionsToUi();
                ShowRestrictedModeWarningIfNeeded();

                SetSyncStatus(cloudLooksSame ? "Auto-refresh OK" : "Auto-refresh updated", success: true);

                selectedPreviewLabel.Text =
                    (cloudLooksSame ? "Auto-refresh check completed." : "Auto-refresh completed.") + Environment.NewLine +
                    "Latest encrypted vault and Device Trust status loaded from Google Drive.";
            }
            catch (Exception ex)
            {
                SetSyncStatus("Auto-refresh failed", error: true);
                selectedPreviewLabel.Text =
                    "Auto-refresh failed." + Environment.NewLine +
                    "Manual Refresh is still available." + Environment.NewLine +
                    "Error: " + ex.Message;
            }
            finally
            {
                autoRefreshRunning = false;
            }
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

            if (idleMinutes < minutes)
            {
                return;
            }

            if (backgroundVaultSyncRunning || backgroundVaultSyncRequested || hasUnsyncedLocalChanges)
            {
                SetSyncStatus("Auto-lock waiting for sync", error: true);

                selectedPreviewLabel.Text =
                    "Auto-lock is ready, but QuickForge is waiting for pending sync to finish." + Environment.NewLine +
                    "Use Sync pending or wait for background sync before locking." + Environment.NewLine +
                    "This avoids losing local changes before they are encrypted and synced.";

                return;
            }

            LockVaultForSafety("Vault locked for safety.");
        }

        private void SecurelyClearCurrentDataKey()
        {
            if (currentDataKey == null)
            {
                return;
            }

            try
            {
                CryptographicOperations.ZeroMemory(currentDataKey);
            }
            catch
            {
                Array.Clear(currentDataKey, 0, currentDataKey.Length);
            }

            currentDataKey = null;
        }

        private void ClearUnlockedVaultSessionSecrets()
        {
            quickFillForm?.Hide();
            ClearSecretAccessWindow();

            vaultCode = "";

            try
            {
                vaultCodeTextBox.Clear();
                confirmVaultCodeTextBox.Clear();
                secretTextBox.Clear();
            }
            catch
            {
                // Ignore UI clear errors during lock.
            }

            SecurelyClearCurrentDataKey();
            currentEncryptedVaultFile = null;

            vaultEntries.Clear();
            visibleVaultEntries.Clear();

            editingEntry = null;
            editingEntryIndex = -1;
        }

        private void LockVaultForSafety(string message)
        {
            autoRefreshTimer.Stop();

            isVaultUnlocked = false;
            ClearUnlockedVaultSessionSecrets();

            RefreshVaultList();
            ClearEntryInputs();
            SetEntryEditMode(false);

            selectedPreviewLabel.Text =
                message + Environment.NewLine +
                "Unlocked vault secrets were cleared from this session.";

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

            if (IsRestrictedModeActive())
            {
                ShowMainWindow();
                RequireTrustedDeviceForSensitiveAction("QuickFill");
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
                if (!RequireTrustedDeviceForSensitiveAction("QuickFill password generator"))
                {
                    SetQuickFillStatus("Blocked: this device is untrusted.");
                    quickFillForm?.Hide();
                    return;
                }

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
            if (!RequireTrustedDeviceForSensitiveAction("QuickFill copy username"))
            {
                SetQuickFillStatus("Blocked: this device is untrusted.");
                quickFillForm?.Hide();
                return;
            }

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
            if (!RequireTrustedDeviceForSensitiveAction("QuickFill copy password"))
            {
                SetQuickFillStatus("Blocked: this device is untrusted.");
                quickFillForm?.Hide();
                return;
            }

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
            if (!RequireTrustedDeviceForSensitiveAction("QuickFill auto-fill"))
            {
                SetQuickFillStatus("Blocked: this device is untrusted.");
                quickFillForm?.Hide();
                return;
            }

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

            SetQuickFillStatus("Filling username and password...");

            await Task.Delay(100);

            Clipboard.SetText(entry.Username);
            SendKeys.SendWait("^v");

            await Task.Delay(120);

            SendKeys.SendWait("{TAB}");

            await Task.Delay(120);

            Clipboard.SetText(entry.Secret);
            SendKeys.SendWait("^v");

            SetQuickFillStatus("Auto-fill completed. Clipboard clears in 20 seconds.");

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
                string prefix = Entry.IsFavorite ? "[F] " : "";

                if (!string.IsNullOrWhiteSpace(Entry.Platform) &&
                    !string.IsNullOrWhiteSpace(Entry.Username))
                {
                    return prefix + Entry.Platform + " - " + Entry.Username;
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
        public string Id { get; set; } = "";
        public string Platform { get; set; } = "";
        public string Username { get; set; } = "";
        public string Secret { get; set; } = "";
        public string Website { get; set; } = "";
        public string Note { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
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

















































































































































