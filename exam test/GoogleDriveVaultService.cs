using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace exam_test
{
    public static class GoogleDriveVaultService
    {
        private const string VaultFileName = "encrypted_vault_v2.json";

        public static async Task<bool> VaultExistsAsync(DriveService driveService)
        {
            DriveFile? file = await FindVaultFileAsync(driveService);
            return file != null;
        }

        public static async Task<string?> DownloadEncryptedVaultAsync(DriveService driveService)
        {
            DriveFile? file = await FindVaultFileAsync(driveService);

            if (file == null || string.IsNullOrWhiteSpace(file.Id))
            {
                return null;
            }

            using MemoryStream stream = new MemoryStream();

            await driveService.Files.Get(file.Id).DownloadAsync(stream);

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        public static async Task UploadEncryptedVaultAsync(
            DriveService driveService,
            string encryptedJson)
        {
            DriveFile? existingFile = await FindVaultFileAsync(driveService);

            byte[] bytes = Encoding.UTF8.GetBytes(encryptedJson);

            using MemoryStream stream = new MemoryStream(bytes);

            if (existingFile == null || string.IsNullOrWhiteSpace(existingFile.Id))
            {
                DriveFile metadata = new DriveFile
                {
                    Name = VaultFileName,
                    Parents = new[] { "appDataFolder" }
                };

                var createRequest = driveService.Files.Create(
                    metadata,
                    stream,
                    "application/json"
                );

                createRequest.Fields = "id";

                await createRequest.UploadAsync();
            }
            else
            {
                DriveFile metadata = new DriveFile
                {
                    Name = VaultFileName
                };

                var updateRequest = driveService.Files.Update(
                    metadata,
                    existingFile.Id,
                    stream,
                    "application/json"
                );

                updateRequest.Fields = "id";

                await updateRequest.UploadAsync();
            }
        }
        public static async Task DeleteVaultAsync(DriveService driveService)
        {
            DriveFile? file = await FindVaultFileAsync(driveService);

            if (file == null || string.IsNullOrWhiteSpace(file.Id))
            {
                return;
            }

            await driveService.Files.Delete(file.Id).ExecuteAsync();
        }
        private static async Task<DriveFile?> FindVaultFileAsync(DriveService driveService)
        {
            var listRequest = driveService.Files.List();

            listRequest.Spaces = "appDataFolder";
            listRequest.Q = $"name='{VaultFileName}' and trashed=false";
            listRequest.Fields = "files(id, name)";

            var result = await listRequest.ExecuteAsync();

            return result.Files?.FirstOrDefault();
        }
    }
}