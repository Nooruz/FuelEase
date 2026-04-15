using System.Reflection;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KIT.GasStation.Licensing.Models;

namespace KIT.GasStation.Licensing.Protection;

/// <summary>
/// Проверка целостности сборок — обнаружение модификации исполняемых файлов.
/// Хеши вычисляются при первом запуске и сохраняются. При каждом старте — сравнение.
/// </summary>
public sealed class AntiTamper
{
    private const string HashesFileName = "assembly_hashes.dat";
    private readonly LicensingOptions _options;
    private readonly ILogger<AntiTamper> _logger;
    private readonly string _hashesPath;

    public AntiTamper(IOptions<LicensingOptions> options, ILogger<AntiTamper> logger)
    {
        _options = options.Value;
        _logger = logger;

        var basePath = string.IsNullOrEmpty(_options.StoragePath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "KIT-AZS", "License")
            : _options.StoragePath;

        _hashesPath = Path.Combine(basePath, HashesFileName);
    }

    /// <summary>
    /// Проверяет целостность защищённых сборок.
    /// </summary>
    public TamperCheckResult Verify()
    {
        var currentHashes = ComputeCurrentHashes();

        if (currentHashes.Count == 0)
        {
            _logger.LogWarning("Не найдено защищённых сборок для проверки");
            return new TamperCheckResult { IsValid = true, Message = "Нет сборок для проверки" };
        }

        // Загружаем сохранённые хеши
        var savedHashes = LoadSavedHashes();

        if (savedHashes.Count == 0)
        {
            // Первый запуск — сохраняем хеши
            SaveHashes(currentHashes);
            _logger.LogInformation("Записаны хеши {Count} защищённых сборок", currentHashes.Count);
            return new TamperCheckResult { IsValid = true, Message = "Хеши сборок инициализированы" };
        }

        // Сравниваем
        var tampered = new List<string>();
        foreach (var (assembly, hash) in currentHashes)
        {
            if (savedHashes.TryGetValue(assembly, out var savedHash))
            {
                if (!string.Equals(hash, savedHash, StringComparison.Ordinal))
                {
                    tampered.Add(assembly);
                    _logger.LogWarning(
                        "Сборка {Assembly} модифицирована: ожидался {Expected}, получен {Actual}",
                        assembly, savedHash[..16], hash[..16]);
                }
            }
            else
            {
                _logger.LogInformation("Новая защищённая сборка: {Assembly}", assembly);
            }
        }

        if (tampered.Count > 0)
        {
            return new TamperCheckResult
            {
                IsValid = false,
                TamperedAssemblies = tampered,
                Message = $"Обнаружена модификация сборок: {string.Join(", ", tampered)}"
            };
        }

        return new TamperCheckResult { IsValid = true, Message = "Целостность сборок подтверждена" };
    }

    /// <summary>
    /// Обновляет сохранённые хеши (после легитимного обновления ПО).
    /// </summary>
    public void UpdateHashes()
    {
        var currentHashes = ComputeCurrentHashes();
        SaveHashes(currentHashes);
        _logger.LogInformation("Хеши сборок обновлены ({Count} сборок)", currentHashes.Count);
    }

    private Dictionary<string, string> ComputeCurrentHashes()
    {
        var hashes = new Dictionary<string, string>();
        var baseDir = AppContext.BaseDirectory;

        // Защищаемые сборки из конфигурации + текущая сборка
        var assemblies = new HashSet<string>(_options.ProtectedAssemblies, StringComparer.OrdinalIgnoreCase);

        // Добавляем все KIT.GasStation.* сборки
        foreach (var dll in Directory.GetFiles(baseDir, "KIT.GasStation.*.dll"))
        {
            assemblies.Add(Path.GetFileName(dll));
        }

        foreach (var assemblyName in assemblies)
        {
            var path = Path.Combine(baseDir, assemblyName);
            if (!File.Exists(path)) continue;

            try
            {
                var bytes = File.ReadAllBytes(path);
                var hash = SHA256.HashData(bytes);
                hashes[assemblyName] = Convert.ToHexString(hash).ToLowerInvariant();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось вычислить хеш сборки {Assembly}", assemblyName);
            }
        }

        return hashes;
    }

    private Dictionary<string, string> LoadSavedHashes()
    {
        var result = new Dictionary<string, string>();
        try
        {
            if (!File.Exists(_hashesPath))
                return result;

            var lines = File.ReadAllLines(_hashesPath);
            foreach (var line in lines)
            {
                var parts = line.Split('|', 2);
                if (parts.Length == 2)
                    result[parts[0]] = parts[1];
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось загрузить сохранённые хеши сборок");
        }

        return result;
    }

    private void SaveHashes(Dictionary<string, string> hashes)
    {
        try
        {
            var dir = Path.GetDirectoryName(_hashesPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var lines = hashes.Select(kv => $"{kv.Key}|{kv.Value}");
            File.WriteAllLines(_hashesPath, lines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось сохранить хеши сборок");
        }
    }
}

public sealed class TamperCheckResult
{
    public bool IsValid { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<string> TamperedAssemblies { get; init; } = new();
}
