using System;
using System.Collections.Generic;
using System.Linq;

namespace exam_test
{
    public class DeviceTrustRegistrationResult
    {
        public bool IsNewDevice { get; set; }
        public bool IsFirstDevice { get; set; }
        public bool IsTrusted { get; set; }
        public KnownVaultDevice? Device { get; set; }
    }

    public static class DeviceTrustPolicy
    {
        public static bool IsCurrentDeviceTrusted(
            VaultSettings settings,
            string localDeviceId,
            bool isVaultUnlocked)
        {
            if (!isVaultUnlocked)
            {
                return true;
            }

            if (settings == null ||
                string.IsNullOrWhiteSpace(localDeviceId))
            {
                return false;
            }

            settings.KnownDevices ??= new List<KnownVaultDevice>();

            KnownVaultDevice? currentDevice = settings.KnownDevices
                .FirstOrDefault(device =>
                    string.Equals(device.DeviceId, localDeviceId, StringComparison.OrdinalIgnoreCase));

            return currentDevice != null &&
                   !currentDevice.IsHiddenFromTrustList &&
                   currentDevice.IsTrusted;
        }

        public static DeviceTrustRegistrationResult RegisterOrRefreshDevice(
            VaultSettings settings,
            string localDeviceId,
            string localDeviceName,
            DateTime utcNow)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (string.IsNullOrWhiteSpace(localDeviceId))
            {
                throw new ArgumentException("Device ID is required.", nameof(localDeviceId));
            }

            settings.KnownDevices ??= new List<KnownVaultDevice>();

            string cleanDeviceId = localDeviceId.Trim();
            string cleanDeviceName = string.IsNullOrWhiteSpace(localDeviceName)
                ? "Unknown device"
                : localDeviceName.Trim();

            KnownVaultDevice? device = settings.KnownDevices
                .FirstOrDefault(item =>
                    string.Equals(item.DeviceId, cleanDeviceId, StringComparison.OrdinalIgnoreCase));

            bool isNewDevice = device == null;
            bool isFirstDevice = settings.KnownDevices.Count == 0;

            if (device == null)
            {
                device = new KnownVaultDevice
                {
                    DeviceId = cleanDeviceId,
                    DeviceName = cleanDeviceName,
                    FirstSeenAtUtc = utcNow,
                    LastSeenAtUtc = utcNow,
                    SyncCount = 0,
                    IsTrusted = isFirstDevice,
                    TrustedChangedAtUtc = utcNow,
                    TrustNote = isFirstDevice
                        ? "First device automatically trusted."
                        : "This device needs approval.",
                    IsHiddenFromTrustList = false,
                    RemovedFromTrustListAtUtc = null
                };

                settings.KnownDevices.Add(device);
            }
            else
            {
                device.DeviceName = cleanDeviceName;
                device.LastSeenAtUtc = utcNow;

                if (device.IsHiddenFromTrustList)
                {
                    device.IsHiddenFromTrustList = false;
                    device.RemovedFromTrustListAtUtc = null;
                    device.IsTrusted = false;
                    device.TrustedChangedAtUtc = utcNow;
                    device.TrustNote = "Device reopened after being removed; approval required.";
                }
            }

            return new DeviceTrustRegistrationResult
            {
                IsNewDevice = isNewDevice,
                IsFirstDevice = isFirstDevice,
                IsTrusted = device.IsTrusted,
                Device = device
            };
        }
    }
}
