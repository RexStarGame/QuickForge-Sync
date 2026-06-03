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
            string credentialsPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "credentials.json"
            );

            if (!File.Exists(credentialsPath))
            {
                throw new FileNotFoundException(
                    "credentials.json blev ikke fundet. Sørg for at den ligger i projektet og har Copy if newer.",
                    credentialsPath
                );
            }

            using FileStream stream = new FileStream(
                credentialsPath,
                FileMode.Open,
                FileAccess.Read
            );

            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(GetTokenFolderPath(), true)
            );

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