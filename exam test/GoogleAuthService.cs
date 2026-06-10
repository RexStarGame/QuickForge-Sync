using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace exam_test
{
    public static class GoogleAuthService
    {
        private static readonly string[] Scopes =
        {
            DriveService.Scope.DriveAppdata
        };

        private const string ApplicationName = "QuickForge Sync";

        public static async Task<DriveService> LoginAsync()
        {
            string? credentialsPath = FindCredentialsPath();

            if (credentialsPath == null)
            {
                throw new FileNotFoundException(
                    GetCredentialsSetupMessage(),
                    GetStoredCredentialsPath()
                );
            }

            using FileStream stream = new FileStream(
                credentialsPath,
                FileMode.Open,
                FileAccess.Read
            );

            string tokenFolderPath = GetTokenFolderPath();
            LocalFileHardeningService.TryHardenDirectory(tokenFolderPath);

            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(tokenFolderPath, true)
            );

            LocalFileHardeningService.TryHardenDirectory(tokenFolderPath);

            DriveService service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });

            return service;
        }

        public static async Task<string> GetUserEmailAsync(DriveService driveService)
        {
            var request = driveService.About.Get();
            request.Fields = "user";

            var about = await request.ExecuteAsync();

            if (about.User != null && !string.IsNullOrWhiteSpace(about.User.EmailAddress))
            {
                return about.User.EmailAddress;
            }

            return "Google account connected";
        }

        public static void Logout()
        {
            string tokenFolder = GetTokenFolderPath();

            if (Directory.Exists(tokenFolder))
            {
                Directory.Delete(tokenFolder, true);
            }
        }

        public static string GetStoredCredentialsPath()
        {
            return Path.Combine(
                GetGoogleSetupFolderPath(),
                "credentials.json"
            );
        }

        public static string GetCredentialsSetupMessage()
        {
            return
                "Google setup is missing." + Environment.NewLine + Environment.NewLine +
                "To use Google Drive sync, choose a Google OAuth Desktop credentials.json file." + Environment.NewLine + Environment.NewLine +
                "QuickForge will save it here:" + Environment.NewLine +
                GetStoredCredentialsPath() + Environment.NewLine + Environment.NewLine +
                "Do not upload credentials.json to GitHub.";
        }

        public static void InstallCredentialsFile(string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException("The selected credentials.json file could not be found.");
            }

            ValidateGoogleCredentialsFile(sourceFilePath);

            string setupFolderPath = GetGoogleSetupFolderPath();
            string storedCredentialsPath = GetStoredCredentialsPath();

            Directory.CreateDirectory(setupFolderPath);
            LocalFileHardeningService.TryHardenDirectory(setupFolderPath);

            File.Copy(
                sourceFilePath,
                storedCredentialsPath,
                true
            );

            LocalFileHardeningService.TryHardenFile(storedCredentialsPath);
            LocalFileHardeningService.TryHardenDirectory(setupFolderPath);
        }

        private static void ValidateGoogleCredentialsFile(string sourceFilePath)
        {
            try
            {
                using FileStream stream = new FileStream(
                    sourceFilePath,
                    FileMode.Open,
                    FileAccess.Read
                );

                ClientSecrets secrets = GoogleClientSecrets.FromStream(stream).Secrets;

                if (secrets == null || string.IsNullOrWhiteSpace(secrets.ClientId))
                {
                    throw new InvalidOperationException("Missing Google OAuth client id.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "The selected file does not look like a valid Google OAuth credentials.json file.",
                    ex
                );
            }
        }

        private static string? FindCredentialsPath()
        {
            string storedCredentialsPath = GetStoredCredentialsPath();

            if (File.Exists(storedCredentialsPath))
            {
                return storedCredentialsPath;
            }

            string bundledCredentialsPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "credentials.json"
            );

            if (File.Exists(bundledCredentialsPath))
            {
                return bundledCredentialsPath;
            }

            return null;
        }

        private static string GetGoogleSetupFolderPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "QuickForge",
                "Google"
            );
        }

        private static string GetTokenFolderPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "QuickForge",
                "GoogleTokens"
            );
        }
    }
}

