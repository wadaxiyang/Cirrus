#if CIRRUS_PREVIEW
using System.Text.Json;
using Cirrus.Base.Services.Abstract;

namespace Cirrus.Services;

/// <summary>
/// Small identity-free preferences store for the unpackaged preview.
/// Keeps the original MSIX PreferenceAccessService unchanged.
/// </summary>
public sealed class UnpackagedPreferenceAccessService : IPreferenceAccessService
{
    private readonly object _sync = new();
    private readonly string _settingsPath;
    private readonly string _largeValueDirectory;
    private readonly Dictionary<string, JsonElement> _settings;

    public UnpackagedPreferenceAccessService()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Cirrus", "Preview");

        Directory.CreateDirectory(root);
        _largeValueDirectory = Path.Combine(root, "Preferences");
        Directory.CreateDirectory(_largeValueDirectory);
        _settingsPath = Path.Combine(root, "settings.json");

        // A malformed preferences file should not prevent the preview from launching.
        if (File.Exists(_settingsPath))
        {
            try
            {
                _settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                    File.ReadAllText(_settingsPath)) ?? new();
                return;
            }
            catch (JsonException)
            {
                // Start with defaults. The original file remains until a setting is changed.
            }
        }

        _settings = new();
    }

    public bool TryGetValue<T>(string preferencePath, out T? value, bool isLarge = false)
    {
        lock (_sync)
        {
            if (isLarge)
            {
                var filePath = GetLargeValuePath(preferencePath);
                if (typeof(T) == typeof(string) && File.Exists(filePath))
                {
                    value = (T?)(object?)File.ReadAllText(filePath);
                    return true;
                }

                value = default;
                return false;
            }

            if (_settings.TryGetValue(preferencePath, out var stored))
            {
                try
                {
                    value = JsonSerializer.Deserialize<T>(stored.GetRawText());
                    return true;
                }
                catch (JsonException)
                {
                    // Corrupt individual values fall back to the application's defaults.
                }
            }

            value = default;
            return false;
        }
    }

    public void SetValue<T>(string preferencePath, T? value, bool isLarge = false)
    {
        lock (_sync)
        {
            if (isLarge)
            {
                if (typeof(T) != typeof(string)) return;
                var path = GetLargeValuePath(preferencePath);
                if (value is string text)
                {
                    File.WriteAllText(path, text);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
                return;
            }

            _settings[preferencePath] = JsonSerializer.SerializeToElement(value);
            SaveSettings();
        }
    }

    public void RemoveValue(string preferencePath, bool isLarge = false)
    {
        lock (_sync)
        {
            if (isLarge)
            {
                var filePath = GetLargeValuePath(preferencePath);
                if (File.Exists(filePath)) File.Delete(filePath);
                return;
            }

            if (_settings.Remove(preferencePath)) SaveSettings();
        }
    }

    private string GetLargeValuePath(string preferencePath)
    {
        var fileName = preferencePath.Replace('/', '.').Replace('\\', '.');
        return Path.Combine(_largeValueDirectory, fileName);
    }

    private void SaveSettings()
    {
        var tempFile = _settingsPath + ".tmp";
        File.WriteAllText(tempFile, JsonSerializer.Serialize(_settings));
        File.Move(tempFile, _settingsPath, overwrite: true);
    }
}
#endif
