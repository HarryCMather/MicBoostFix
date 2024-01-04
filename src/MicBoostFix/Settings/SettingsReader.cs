using System;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using SettingsModel = MicBoostFix.Models.Settings;

namespace MicBoostFix.Settings;

public static class SettingsReader
{
    private const string MicSettingsFileName = "MicSettings.json";

    public static async Task<SettingsModel> ReadSettings()
    {
        if (await ApplicationData.Current.LocalFolder.TryGetItemAsync(MicSettingsFileName) is null)
        {
            return null;
        }

        StorageFolder folder = ApplicationData.Current.LocalFolder;
        StorageFile file = await folder.GetFileAsync(MicSettingsFileName);
        string settingsJson = await FileIO.ReadTextAsync(file);

        SettingsModel settings = JsonSerializer.Deserialize<SettingsModel>(settingsJson);
        if (!await settings.ReadValidation())
        {
            Console.WriteLine("Error: Invalid settings were detected. Settings will be re-created.");
            return null;
        }

        return settings;
    }
}
