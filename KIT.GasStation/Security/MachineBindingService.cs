using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace KIT.GasStation.Security
{
    internal sealed class MachineBindingService
    {
        #region Private Members

        private const string BindingFileName = "machine.id";
        private const string ProductFolderName = "KIT-GasStation";
        private const string RegistryPath = @"SOFTWARE\KIT-GasStation";
        private const string RegistryValueName = "MachineBinding";

        #endregion

        #region Public Voids

        public bool EnsureMachineBinding(out string? errorMessage)
        {
            errorMessage = null;
            string fingerprint = GetMachineFingerprint();
            if (string.IsNullOrWhiteSpace(fingerprint))
            {
                errorMessage = "Не удалось определить идентификатор оборудования.";
                return false;
            }

            string bindingPath = GetBindingFilePath();
            string encryptedFingerprint = Protect(fingerprint);

            try
            {
                string? registryBinding = ReadRegistryBinding();
                bool fileExists = File.Exists(bindingPath);

                if (!fileExists && string.IsNullOrWhiteSpace(registryBinding))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(bindingPath)!);
                    WriteBindingFile(bindingPath, encryptedFingerprint);
                    WriteRegistryBinding(encryptedFingerprint);
                    return true;
                }

                if (!fileExists || string.IsNullOrWhiteSpace(registryBinding))
                {
                    errorMessage = "Обнаружено вмешательство в данные привязки. Запуск приложения запрещен.";
                    return false;
                }

                string storedFingerprint = Unprotect(File.ReadAllText(bindingPath).Trim());
                string registryFingerprint = Unprotect(registryBinding);

                if (!string.Equals(storedFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(registryFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = "Приложение привязано к другому компьютеру. Запуск на данном устройстве запрещен.";
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                errorMessage = "Не удалось проверить привязку к оборудованию.";
                return false;
            }
        }

        #endregion

        #region Private Voids

        private static string GetBindingFilePath()
        {
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return Path.Combine(basePath, ProductFolderName, BindingFileName);
        }

        private static string GetMachineFingerprint()
        {
            string[] parts =
            {
                GetWmiValue("Win32_ComputerSystemProduct", "UUID"),
                GetWmiValue("Win32_BaseBoard", "SerialNumber"),
                GetWmiValue("Win32_BIOS", "SerialNumber")
            };

            string joined = string.Join("|", parts.Where(value => !string.IsNullOrWhiteSpace(value)));
            if (string.IsNullOrWhiteSpace(joined))
            {
                return string.Empty;
            }

            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(joined));
            var builder = new StringBuilder(hash.Length * 2);
            foreach (byte value in hash)
            {
                builder.Append(value.ToString("x2"));
            }

            return builder.ToString();
        }

        private static void WriteBindingFile(string path, string value)
        {
            File.WriteAllText(path, value);
            var attributes = File.GetAttributes(path);
            File.SetAttributes(path, attributes | FileAttributes.Hidden | FileAttributes.ReadOnly);
        }

        private static string Protect(string value)
        {
            byte[] data = Encoding.UTF8.GetBytes(value);
            byte[] protectedData = ProtectedData.Protect(data, null, DataProtectionScope.LocalMachine);
            return Convert.ToBase64String(protectedData);
        }

        private static string Unprotect(string value)
        {
            byte[] data = Convert.FromBase64String(value);
            byte[] unprotectedData = ProtectedData.Unprotect(data, null, DataProtectionScope.LocalMachine);
            return Encoding.UTF8.GetString(unprotectedData);
        }

        private static void WriteRegistryBinding(string value)
        {
            using var key = Registry.LocalMachine.CreateSubKey(RegistryPath, true);
            key?.SetValue(RegistryValueName, value, RegistryValueKind.String);
        }

        private static string? ReadRegistryBinding()
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegistryPath, false);
            return key?.GetValue(RegistryValueName) as string;
        }

        private static string GetWmiValue(string className, string propertyName)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {className}");
                foreach (ManagementObject managementObject in searcher.Get())
                {
                    var value = managementObject[propertyName]?.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value.Trim();
                    }
                }
            }
            catch (ManagementException)
            {
                return string.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                return string.Empty;
            }

            return string.Empty;
        }

        #endregion
    }
}
