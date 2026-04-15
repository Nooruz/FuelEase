using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KIT.GasStation.Licensing.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KIT.GasStation.Licensing.Storage;

/// <summary>
/// Защищённое локальное хранилище лицензии и состояния.
/// Использует несколько реплик + HMAC для контроля целостности.
/// </summary>
public sealed class LicenseStore
{
    private const string LicenseFileName = "license.dat";
    private const string StateFileName = "state.dat";
    private const int ReplicaCount = 3;

    private readonly string _basePath;
    private readonly byte[] _hmacKey;
    private readonly ILogger<LicenseStore> _logger;
    private readonly object _lock = new();

    public LicenseStore(IOptions<LicensingOptions> options, ILogger<LicenseStore> logger)
    {
        _logger = logger;
        var opts = options.Value;

        _basePath = string.IsNullOrEmpty(opts.StoragePath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "KIT-AZS", "License")
            : opts.StoragePath;

        // HMAC-ключ уникален для каждого устройства (привязка к железу)
        var fingerprint = Core.HardwareFingerprint.Generate();
        _hmacKey = SHA256.HashData(Encoding.UTF8.GetBytes($"KIT_LICENSE_HMAC_{fingerprint}"));

        EnsureDirectories();
    }

    #region License File

    /// <summary>Сохраняет файл лицензии во все реплики.</summary>
    public void SaveLicense(LicenseFile license)
    {
        var json = JsonSerializer.Serialize(license);
        var encrypted = Protect(json);

        lock (_lock)
        {
            for (int i = 0; i < ReplicaCount; i++)
            {
                var path = GetReplicaPath(LicenseFileName, i);
                try
                {
                    File.WriteAllBytes(path, encrypted);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось записать реплику лицензии #{Replica}", i);
                }
            }
        }
    }

    /// <summary>Загружает файл лицензии (из первой целой реплики).</summary>
    public LicenseFile? LoadLicense()
    {
        lock (_lock)
        {
            for (int i = 0; i < ReplicaCount; i++)
            {
                var path = GetReplicaPath(LicenseFileName, i);
                try
                {
                    if (!File.Exists(path)) continue;

                    var encrypted = File.ReadAllBytes(path);
                    var json = Unprotect(encrypted);
                    if (json == null) continue;

                    var license = JsonSerializer.Deserialize<LicenseFile>(json);
                    if (license != null)
                    {
                        // Восстанавливаем повреждённые реплики
                        RepairReplicas(LicenseFileName, encrypted, i);
                        return license;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Реплика лицензии #{Replica} повреждена", i);
                }
            }
        }

        return null;
    }

    #endregion

    #region License State

    /// <summary>Сохраняет состояние лицензии во все реплики с HMAC.</summary>
    public void SaveState(LicenseState state)
    {
        // Вычисляем HMAC перед сохранением
        state.IntegrityHmac = ComputeStateHmac(state);

        var json = JsonSerializer.Serialize(state);
        var encrypted = Protect(json);

        lock (_lock)
        {
            for (int i = 0; i < ReplicaCount; i++)
            {
                var path = GetReplicaPath(StateFileName, i);
                try
                {
                    File.WriteAllBytes(path, encrypted);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось записать реплику состояния #{Replica}", i);
                }
            }
        }
    }

    /// <summary>Загружает состояние лицензии, проверяя HMAC целостность.</summary>
    public LicenseState? LoadState()
    {
        lock (_lock)
        {
            for (int i = 0; i < ReplicaCount; i++)
            {
                var path = GetReplicaPath(StateFileName, i);
                try
                {
                    if (!File.Exists(path)) continue;

                    var encrypted = File.ReadAllBytes(path);
                    var json = Unprotect(encrypted);
                    if (json == null) continue;

                    var state = JsonSerializer.Deserialize<LicenseState>(json);
                    if (state == null) continue;

                    // Проверка HMAC
                    var expectedHmac = ComputeStateHmac(state);
                    if (!string.Equals(state.IntegrityHmac, expectedHmac, StringComparison.Ordinal))
                    {
                        _logger.LogWarning("HMAC реплики состояния #{Replica} не совпадает — данные изменены", i);
                        continue;
                    }

                    RepairReplicas(StateFileName, encrypted, i);
                    return state;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Реплика состояния #{Replica} повреждена", i);
                }
            }
        }

        return null;
    }

    #endregion

    #region Encryption (DPAPI + AES fallback)

    private byte[] Protect(string data)
    {
        var plainBytes = Encoding.UTF8.GetBytes(data);

        // Используем AES с ключом, привязанным к оборудованию
        using var aes = Aes.Create();
        aes.Key = _hmacKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // IV + encrypted data
        var result = new byte[aes.IV.Length + encrypted.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

        return result;
    }

    private string? Unprotect(byte[] data)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = _hmacKey;

            var iv = new byte[aes.BlockSize / 8];
            Buffer.BlockCopy(data, 0, iv, 0, iv.Length);
            aes.IV = iv;

            var encrypted = new byte[data.Length - iv.Length];
            Buffer.BlockCopy(data, iv.Length, encrypted, 0, encrypted.Length);

            using var decryptor = aes.CreateDecryptor();
            var decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region HMAC & Integrity

    private string ComputeStateHmac(LicenseState state)
    {
        // HMAC вычисляется по всем полям, кроме самого HMAC
        var data = $"{state.Status}|{state.LastOnlineCheckUtc:O}|{state.LastServerTimeUtc:O}|" +
                   $"{state.LastLocalTimeUtc:O}|{state.LastTickCount}|{state.InstanceId}|" +
                   $"{state.LeaseToken}|{state.LeaseExpiryUtc:O}|{state.GracePeriodStartUtc:O}|" +
                   $"{state.ConsecutiveFailedOnlineChecks}";

        using var hmac = new HMACSHA256(_hmacKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    #endregion

    #region Replica Management

    private void EnsureDirectories()
    {
        for (int i = 0; i < ReplicaCount; i++)
        {
            var dir = GetReplicaDirectory(i);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }

    private string GetReplicaDirectory(int index) => index switch
    {
        0 => Path.Combine(_basePath, "primary"),
        1 => Path.Combine(_basePath, "secondary"),
        2 => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KIT-AZS", "LicenseBackup"),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    private string GetReplicaPath(string fileName, int index)
        => Path.Combine(GetReplicaDirectory(index), fileName);

    private void RepairReplicas(string fileName, byte[] validData, int validIndex)
    {
        for (int i = 0; i < ReplicaCount; i++)
        {
            if (i == validIndex) continue;

            var path = GetReplicaPath(fileName, i);
            try
            {
                if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(validData))
                {
                    File.WriteAllBytes(path, validData);
                    _logger.LogInformation("Реплика #{Replica} файла {File} восстановлена", i, fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось восстановить реплику #{Replica}", i);
            }
        }
    }

    #endregion
}
