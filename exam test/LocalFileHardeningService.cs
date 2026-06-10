using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace exam_test
{
    public static class LocalFileHardeningService
    {
        public static bool TryHardenDirectory(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(folderPath);

                if (!OperatingSystem.IsWindows())
                {
                    return true;
                }

                DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
                DirectorySecurity security = directoryInfo.GetAccessControl();

                security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

                string currentUser = WindowsIdentity.GetCurrent().Name;

                FileSystemAccessRule rule = new FileSystemAccessRule(
                    currentUser,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow
                );

                security.ResetAccessRule(rule);
                directoryInfo.SetAccessControl(security);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryHardenFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            try
            {
                if (!OperatingSystem.IsWindows())
                {
                    return true;
                }

                FileInfo fileInfo = new FileInfo(filePath);
                FileSecurity security = fileInfo.GetAccessControl();

                security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

                string currentUser = WindowsIdentity.GetCurrent().Name;

                FileSystemAccessRule rule = new FileSystemAccessRule(
                    currentUser,
                    FileSystemRights.FullControl,
                    AccessControlType.Allow
                );

                security.ResetAccessRule(rule);
                fileInfo.SetAccessControl(security);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
