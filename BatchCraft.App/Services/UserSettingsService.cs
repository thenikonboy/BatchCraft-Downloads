using System.IO;
using System.Text.Json;

namespace BatchCraft.App.Services;

public sealed class UserSettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BatchCraft", "settings.json");

    public UserSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new UserSettings();
            return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(SettingsPath)) ?? new UserSettings();
        }
        catch
        {
            return new UserSettings();
        }
    }

    public void Save(UserSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        var temporaryPath = SettingsPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporaryPath, SettingsPath, true);
    }
}

public sealed class UserSettings
{
    public string Language { get; set; } = "th";
    public string Theme { get; set; } = "dark";
    public string LastInputFolder { get; set; } = "";
    public string LastOutputFolder { get; set; } = "";
    public string DefaultOutputFolder { get; set; } = "";
}
