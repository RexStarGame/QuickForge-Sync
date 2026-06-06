using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace exam_test
{
    public sealed class GoogleDriveVaultMetadata
    {
        public string FileId { get; init; } = "";
        public DateTime? ModifiedTime { get; init; }
        public string Md5Checksum { get; init; } = "";
        public long? Version { get; init; }

        public string Fingerprint
        {
            get
            {
                string modifiedTicks = ModifiedTime.HasValue
                    ? ModifiedTime.Value.ToUniversalTime().Ticks.ToString()
                    : "";

                return FileId + "|" + (Version?.ToString() ?? "") + "|" + modifiedTicks + "|" + Md5Checksum;
            }
        }
    }

    public sealed class GoogleDriveVaultDownload
    {
        public string EncryptedJson { get; init; } = "";
        public GoogleDriveVaultMetadata Metadata { get; init; } = new GoogleDriveVaultMetadata();
    }

    public static class GoogleDriveVaultService
    {
        private const string VaultFileName = "encrypted_vault_v2.json";

        public static async Task<bool> VaultExistsAsync(DriveService driveService)
        {
            DriveFile? file = await FindVaultFileAsync(driveService);
            return file != null;
        }

        public static async Task<GoogleDriveVaultMetadata?> GetVaultMetadataAsync(DriveService driveService)
        {
            DriveFile? file = await FindVaultFileAsync(driveService);

            if (file == null || string.IsNullOrWhiteSpace(file.Id))
            {
                return null;
            }

            return CreateMetadata(file);
        }

        public static async Task<GoogleDriveVaultDownload?> DownloadEncryptedVaultWithMetadataAsync(
            DriveService driveService)
        {
            DriveFile? file = await FindVaultFileAsync(driveService);

            if (file == null || string.IsNullOrWhiteSpace(file.Id))
            {
                return null;
            }

            using MemoryStream stream = new MemoryStream();
            await driveService.Files.Get(file.Id).DownloadAsync(stream);

            return new GoogleDriveVaultDownload
            {
                EncryptedJson = Encoding.UTF8.GetString(stream.ToArray()),
                Metadata = CreateMetadata(file)
            };
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

        public static async Task<GoogleDriveVaultMetadata?> UploadEncryptedVaultAsync(
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

                createRequest.Fields = "id, modifiedTime, md5Checksum, version";
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

                updateRequest.Fields = "id, modifiedTime, md5Checksum, version";
                await updateRequest.UploadAsync();
            }

            return await GetVaultMetadataAsync(driveService);
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
            listRequest.Fields = "files(id, name, modifiedTime, md5Checksum, version)";

            var result = await listRequest.ExecuteAsync();

            return result.Files?.FirstOrDefault();
        }

        private static GoogleDriveVaultMetadata CreateMetadata(DriveFile file)
        {
            return new GoogleDriveVaultMetadata
            {
                FileId = file.Id ?? "",
                ModifiedTime = file.ModifiedTimeDateTimeOffset?.DateTime,
                Md5Checksum = file.Md5Checksum ?? "",
                Version = file.Version
            };
        }
    }
}

