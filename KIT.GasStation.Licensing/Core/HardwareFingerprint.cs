using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace KIT.GasStation.Licensing.Core;

/// <summary>
/// Сбор аппаратного отпечатка — CPU, материнская плата, диск, MAC-адрес.
/// Отпечаток стабилен при перезагрузках и не меняется при обновлении ОС.
/// </summary>
public static class HardwareFingerprint
{
    /// <summary>
    /// Генерирует SHA-256 хеш аппаратного отпечатка.
    /// </summary>
    public static string Generate()
    {
        var components = new StringBuilder();

        components.Append(GetCpuId());
        components.Append('|');
        components.Append(GetMotherboardSerial());
        components.Append('|');
        components.Append(GetDiskSerial());
        components.Append('|');
        components.Append(GetMacAddress());

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(components.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Возвращает отдельные компоненты отпечатка (для диагностики).
    /// </summary>
    public static Dictionary<string, string> GetComponents()
    {
        return new Dictionary<string, string>
        {
            ["CPU"] = GetCpuId(),
            ["Motherboard"] = GetMotherboardSerial(),
            ["Disk"] = GetDiskSerial(),
            ["MAC"] = GetMacAddress()
        };
    }

    private static string GetCpuId()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                var id = obj["ProcessorId"]?.ToString();
                if (!string.IsNullOrEmpty(id))
                    return id;
            }
        }
        catch
        {
            // WMI недоступен — fallback
        }

        return "CPU_UNKNOWN";
    }

    private static string GetMotherboardSerial()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
            foreach (var obj in searcher.Get())
            {
                var serial = obj["SerialNumber"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(serial) && serial != "To be filled by O.E.M." && serial != "Default string")
                    return serial;
            }
        }
        catch
        {
            // fallback
        }

        // Альтернатива: Product UUID из BIOS
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct");
            foreach (var obj in searcher.Get())
            {
                var uuid = obj["UUID"]?.ToString();
                if (!string.IsNullOrEmpty(uuid) && uuid != "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")
                    return uuid;
            }
        }
        catch { }

        return "MB_UNKNOWN";
    }

    private static string GetDiskSerial()
    {
        try
        {
            // Берём серийный номер системного диска
            using var searcher = new ManagementObjectSearcher(
                "SELECT SerialNumber FROM Win32_DiskDrive WHERE Index=0");
            foreach (var obj in searcher.Get())
            {
                var serial = obj["SerialNumber"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(serial))
                    return serial;
            }
        }
        catch { }

        return "DISK_UNKNOWN";
    }

    private static string GetMacAddress()
    {
        try
        {
            // Берём первый физический Ethernet/Wi-Fi адаптер (исключаем виртуальные)
            var nic = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.NetworkInterfaceType is NetworkInterfaceType.Ethernet
                                                   or NetworkInterfaceType.Wireless80211)
                .Where(n => !n.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase))
                .Where(n => !n.Description.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase))
                .Where(n => !n.Description.Contains("VPN", StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n.Name)
                .FirstOrDefault();

            if (nic != null)
                return nic.GetPhysicalAddress().ToString();
        }
        catch { }

        return "MAC_UNKNOWN";
    }
}
