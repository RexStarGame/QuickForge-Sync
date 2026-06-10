using System;
using exam_test;
using Xunit;

namespace QuickForge.Tests
{
    public class DeviceTrustPolicyTests
    {
        [Fact]
        public void IsCurrentDeviceTrusted_ReturnsFalse_WhenUnlockedDeviceIsMissing()
        {
            var settings = new VaultSettings();

            bool trusted = DeviceTrustPolicy.IsCurrentDeviceTrusted(
                settings,
                "device-1",
                isVaultUnlocked: true);

            Assert.False(trusted);
        }

        [Fact]
        public void RegisterOrRefreshDevice_TrustsFirstDevice()
        {
            var settings = new VaultSettings();
            DateTime now = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);

            var result = DeviceTrustPolicy.RegisterOrRefreshDevice(
                settings,
                "device-1",
                "Main PC",
                now);

            Assert.True(result.IsNewDevice);
            Assert.True(result.IsFirstDevice);
            Assert.True(result.IsTrusted);
            Assert.Single(settings.KnownDevices);
            Assert.True(settings.KnownDevices[0].IsTrusted);
        }

        [Fact]
        public void RegisterOrRefreshDevice_MarksSecondDeviceUntrusted()
        {
            var settings = new VaultSettings();
            DateTime now = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);

            DeviceTrustPolicy.RegisterOrRefreshDevice(settings, "device-1", "Main PC", now);
            var secondResult = DeviceTrustPolicy.RegisterOrRefreshDevice(
                settings,
                "device-2",
                "Laptop",
                now.AddMinutes(1));

            Assert.True(secondResult.IsNewDevice);
            Assert.False(secondResult.IsFirstDevice);
            Assert.False(secondResult.IsTrusted);
            Assert.Equal(2, settings.KnownDevices.Count);
            Assert.False(settings.KnownDevices[1].IsTrusted);
            Assert.Equal("This device needs approval.", settings.KnownDevices[1].TrustNote);
        }

        [Fact]
        public void IsCurrentDeviceTrusted_ReturnsFalse_ForHiddenOrUntrustedDevice()
        {
            var settings = new VaultSettings();

            settings.KnownDevices.Add(new KnownVaultDevice
            {
                DeviceId = "device-1",
                DeviceName = "Laptop",
                IsTrusted = true,
                IsHiddenFromTrustList = true
            });

            settings.KnownDevices.Add(new KnownVaultDevice
            {
                DeviceId = "device-2",
                DeviceName = "Old PC",
                IsTrusted = false,
                IsHiddenFromTrustList = false
            });

            Assert.False(DeviceTrustPolicy.IsCurrentDeviceTrusted(settings, "device-1", true));
            Assert.False(DeviceTrustPolicy.IsCurrentDeviceTrusted(settings, "device-2", true));
        }
    }
}
