using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace exam_test
{
    public static class VaultCryptoService
    {
        private const int SaltSize = 16;
        private const int NonceSize = 12;
        private const int TagSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 600000;

        public static string GenerateRecoveryKey()
        {
            string part1 = RandomPart();
            string part2 = RandomPart();
            string part3 = RandomPart();
            string part4 = RandomPart();

            return $"QF-{part1}-{part2}-{part3}-{part4}";
        }

        public static string CreateEncryptedVault(
            VaultData vaultData,
            string vaultCode,
            string recoveryKey,
            out byte[] dataKey,
            out EncryptedVaultFile encryptedVaultFile)
        {
            dataKey = RandomNumberGenerator.GetBytes(KeySize);

            encryptedVaultFile = new EncryptedVaultFile
            {
                Version = 2,
                MasterKeyWrapper = WrapDataKey(dataKey, vaultCode),
                RecoveryKeyWrapper = WrapDataKey(dataKey, recoveryKey)
            };

            string encryptedJson = EncryptVaultDataWithExistingKeys(
                vaultData,
                dataKey,
                encryptedVaultFile
            );

            encryptedVaultFile = JsonSerializer.Deserialize<EncryptedVaultFile>(encryptedJson)
                ?? throw new InvalidOperationException("Could not create encrypted vault.");

            return encryptedJson;
        }

        public static VaultData DecryptVault(
            string encryptedJson,
            string unlockCode,
            out byte[] dataKey,
            out EncryptedVaultFile encryptedVaultFile)
        {
            encryptedVaultFile = JsonSerializer.Deserialize<EncryptedVaultFile>(encryptedJson)
                ?? throw new InvalidOperationException("Encrypted vault file could not be read.");

            if (encryptedVaultFile.Version != 2)
            {
                throw new InvalidOperationException("Unsupported vault version.");
            }

            if (!TryUnwrapDataKey(encryptedVaultFile.MasterKeyWrapper, unlockCode, out dataKey))
            {
                if (!TryUnwrapDataKey(encryptedVaultFile.RecoveryKeyWrapper, unlockCode, out dataKey))
                {
                    throw new CryptographicException("Wrong vault code or recovery key.");
                }
            }

            byte[] nonce = Convert.FromBase64String(encryptedVaultFile.VaultNonce);
            byte[] tag = Convert.FromBase64String(encryptedVaultFile.VaultTag);
            byte[] cipherBytes = Convert.FromBase64String(encryptedVaultFile.VaultCipherText);

            byte[] plainBytes = new byte[cipherBytes.Length];

            using (AesGcm aes = new AesGcm(dataKey, TagSize))
            {
                aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
            }

            string plainJson = Encoding.UTF8.GetString(plainBytes);

            VaultData? vaultData = JsonSerializer.Deserialize<VaultData>(plainJson);

            if (vaultData == null)
            {
                throw new InvalidOperationException("Vault data could not be decrypted.");
            }

            return vaultData;
        }

        public static EncryptedVaultFile ReadEncryptedVaultFile(string encryptedJson)
        {
            return JsonSerializer.Deserialize<EncryptedVaultFile>(encryptedJson)
                ?? throw new InvalidOperationException("Encrypted vault file could not be read.");
        }

        public static bool CanUnlockWithVaultCode(
            EncryptedVaultFile encryptedVaultFile,
            string unlockCode)
        {
            if (string.IsNullOrWhiteSpace(unlockCode))
            {
                return false;
            }

            return TryUnwrapDataKey(encryptedVaultFile.MasterKeyWrapper, unlockCode, out _);
        }

        public static bool CanUnlockWithRecoveryKey(
            EncryptedVaultFile encryptedVaultFile,
            string unlockCode)
        {
            if (string.IsNullOrWhiteSpace(unlockCode))
            {
                return false;
            }

            return TryUnwrapDataKey(encryptedVaultFile.RecoveryKeyWrapper, unlockCode, out _);
        }

        public static VaultData DecryptVaultWithVaultCode(
            string encryptedJson,
            string vaultCode,
            out byte[] dataKey,
            out EncryptedVaultFile encryptedVaultFile)
        {
            encryptedVaultFile = ReadEncryptedVaultFile(encryptedJson);

            if (encryptedVaultFile.Version != 2)
            {
                throw new InvalidOperationException("Unsupported vault version.");
            }

            if (!TryUnwrapDataKey(encryptedVaultFile.MasterKeyWrapper, vaultCode, out dataKey))
            {
                throw new CryptographicException("Wrong vault code.");
            }

            return DecryptVaultPayload(encryptedVaultFile, dataKey);
        }

        public static VaultData DecryptVaultWithRecoveryKey(
            string encryptedJson,
            string recoveryKey,
            out byte[] dataKey,
            out EncryptedVaultFile encryptedVaultFile)
        {
            encryptedVaultFile = ReadEncryptedVaultFile(encryptedJson);

            if (encryptedVaultFile.Version != 2)
            {
                throw new InvalidOperationException("Unsupported vault version.");
            }

            if (!TryUnwrapDataKey(encryptedVaultFile.RecoveryKeyWrapper, recoveryKey, out dataKey))
            {
                throw new CryptographicException("Wrong recovery key.");
            }

            return DecryptVaultPayload(encryptedVaultFile, dataKey);
        }

        public static bool TryDecryptVaultWithVaultCode(
            string encryptedJson,
            string vaultCode,
            out VaultData? vaultData,
            out byte[]? dataKey,
            out EncryptedVaultFile? encryptedVaultFile)
        {
            vaultData = null;
            dataKey = null;
            encryptedVaultFile = null;

            try
            {
                EncryptedVaultFile loadedEncryptedVaultFile = ReadEncryptedVaultFile(encryptedJson);

                if (loadedEncryptedVaultFile.Version != 2)
                {
                    return false;
                }

                if (!TryUnwrapDataKey(loadedEncryptedVaultFile.MasterKeyWrapper, vaultCode, out byte[] loadedDataKey))
                {
                    return false;
                }

                vaultData = DecryptVaultPayload(loadedEncryptedVaultFile, loadedDataKey);
                dataKey = loadedDataKey;
                encryptedVaultFile = loadedEncryptedVaultFile;
                return true;
            }
            catch
            {
                vaultData = null;
                dataKey = null;
                encryptedVaultFile = null;
                return false;
            }
        }

        public static bool TryDecryptVaultWithRecoveryKey(
            string encryptedJson,
            string recoveryKey,
            out VaultData? vaultData,
            out byte[]? dataKey,
            out EncryptedVaultFile? encryptedVaultFile)
        {
            vaultData = null;
            dataKey = null;
            encryptedVaultFile = null;

            try
            {
                EncryptedVaultFile loadedEncryptedVaultFile = ReadEncryptedVaultFile(encryptedJson);

                if (loadedEncryptedVaultFile.Version != 2)
                {
                    return false;
                }

                if (!TryUnwrapDataKey(loadedEncryptedVaultFile.RecoveryKeyWrapper, recoveryKey, out byte[] loadedDataKey))
                {
                    return false;
                }

                vaultData = DecryptVaultPayload(loadedEncryptedVaultFile, loadedDataKey);
                dataKey = loadedDataKey;
                encryptedVaultFile = loadedEncryptedVaultFile;
                return true;
            }
            catch
            {
                vaultData = null;
                dataKey = null;
                encryptedVaultFile = null;
                return false;
            }
        }
        private static VaultData DecryptVaultPayload(
            EncryptedVaultFile encryptedVaultFile,
            byte[] dataKey)
        {
            byte[] nonce = Convert.FromBase64String(encryptedVaultFile.VaultNonce);
            byte[] tag = Convert.FromBase64String(encryptedVaultFile.VaultTag);
            byte[] cipherBytes = Convert.FromBase64String(encryptedVaultFile.VaultCipherText);

            byte[] plainBytes = new byte[cipherBytes.Length];

            using (AesGcm aes = new AesGcm(dataKey, TagSize))
            {
                aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
            }

            string plainJson = Encoding.UTF8.GetString(plainBytes);

            VaultData? vaultData = JsonSerializer.Deserialize<VaultData>(plainJson);

            if (vaultData == null)
            {
                throw new InvalidOperationException("Vault data could not be decrypted.");
            }

            return vaultData;
        }
        public static string EncryptVaultDataWithExistingKeys(
            VaultData vaultData,
            byte[] dataKey,
            EncryptedVaultFile encryptedVaultFile)
        {
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);

            string plainJson = JsonSerializer.Serialize(vaultData);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainJson);

            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[TagSize];

            using (AesGcm aes = new AesGcm(dataKey, TagSize))
            {
                aes.Encrypt(nonce, plainBytes, cipherBytes, tag);
            }

            encryptedVaultFile.VaultNonce = Convert.ToBase64String(nonce);
            encryptedVaultFile.VaultTag = Convert.ToBase64String(tag);
            encryptedVaultFile.VaultCipherText = Convert.ToBase64String(cipherBytes);

            return JsonSerializer.Serialize(
                encryptedVaultFile,
                new JsonSerializerOptions { WriteIndented = true }
            );
        }

        public static void ChangeVaultCode(
            EncryptedVaultFile encryptedVaultFile,
            byte[] dataKey,
            string newVaultCode)
        {
            encryptedVaultFile.MasterKeyWrapper = WrapDataKey(dataKey, newVaultCode);
        }
        public static void RotateRecoveryKey(
            EncryptedVaultFile encryptedVaultFile,
            byte[] dataKey,
            string newRecoveryKey)
        {
            encryptedVaultFile.RecoveryKeyWrapper = WrapDataKey(dataKey, newRecoveryKey);
        }
        public static bool CanUnlockVault(
            EncryptedVaultFile encryptedVaultFile,
            string unlockCode)
        {
            return
                CanUnlockWithVaultCode(encryptedVaultFile, unlockCode) ||
                CanUnlockWithRecoveryKey(encryptedVaultFile, unlockCode);
        }

        private static KeyWrapper WrapDataKey(byte[] dataKey, string unlockCode)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);

            byte[] wrappingKey = Rfc2898DeriveBytes.Pbkdf2(
                unlockCode,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize
            );

            byte[] encryptedDataKey = new byte[dataKey.Length];
            byte[] tag = new byte[TagSize];

            using (AesGcm aes = new AesGcm(wrappingKey, TagSize))
            {
                aes.Encrypt(nonce, dataKey, encryptedDataKey, tag);
            }

            return new KeyWrapper
            {
                Kdf = "PBKDF2-SHA256",
                Iterations = Iterations,
                Salt = Convert.ToBase64String(salt),
                Nonce = Convert.ToBase64String(nonce),
                Tag = Convert.ToBase64String(tag),
                EncryptedDataKey = Convert.ToBase64String(encryptedDataKey)
            };
        }

        private static bool TryUnwrapDataKey(
            KeyWrapper wrapper,
            string unlockCode,
            out byte[] dataKey)
        {
            dataKey = Array.Empty<byte>();

            try
            {
                byte[] salt = Convert.FromBase64String(wrapper.Salt);
                byte[] nonce = Convert.FromBase64String(wrapper.Nonce);
                byte[] tag = Convert.FromBase64String(wrapper.Tag);
                byte[] encryptedDataKey = Convert.FromBase64String(wrapper.EncryptedDataKey);

                byte[] wrappingKey = Rfc2898DeriveBytes.Pbkdf2(
                    unlockCode,
                    salt,
                    wrapper.Iterations,
                    HashAlgorithmName.SHA256,
                    KeySize
                );

                byte[] plainDataKey = new byte[encryptedDataKey.Length];

                using (AesGcm aes = new AesGcm(wrappingKey, TagSize))
                {
                    aes.Decrypt(nonce, encryptedDataKey, tag, plainDataKey);
                }

                dataKey = plainDataKey;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string RandomPart()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

            char[] result = new char[4];

            for (int i = 0; i < result.Length; i++)
            {
                int index = RandomNumberGenerator.GetInt32(chars.Length);
                result[i] = chars[index];
            }

            return new string(result);
        }
    }
}



