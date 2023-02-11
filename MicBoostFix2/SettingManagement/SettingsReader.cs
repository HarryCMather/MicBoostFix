using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Storage;
using MicBoostFix2.ConsoleWindowManager;
using Newtonsoft.Json;

namespace MicBoostFix2.SettingManagement;

public static class SettingsReader
{
    public static async Task<Settings> ReadSettings()
    {
        if (await ApplicationData.Current.LocalFolder.TryGetItemAsync("MicSettings.txt") is null)
        {
            return null;
        }

        StorageFolder folder = ApplicationData.Current.LocalFolder;
        StorageFile file = await folder.GetFileAsync("MicSettings.txt");
        string settingsJson = await FileIO.ReadTextAsync(file);

        Settings settings = JsonConvert.DeserializeObject<Settings>(settingsJson);
        await ValidateSettings(settings);
        
        return settings;
    }
    
    private static async Task ValidateSettings(Settings settings)
    {
        if (settings.MicrophoneLevel is < 0.0f or > 100.0f)
        {
            ConsoleWindowHider.DisplayConsoleWindow(true);
            throw new ArgumentOutOfRangeException(nameof(settings.MicrophoneLevel), "Error: Microphone level was invalid. Exiting program.");
        }

        if (!await CheckValidMicrophoneId(settings.MicrophoneId))
        {
            ConsoleWindowHider.DisplayConsoleWindow(true);
            throw new ArgumentOutOfRangeException(nameof(settings.MicrophoneId), "Error: An invalid Microphone ID was specified. Exiting program.");
        }
    }

    private static async Task<bool> CheckValidMicrophoneId(string microphoneId)
    {
        return (await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture))
            .Select(microphone => microphone.Id)
            .Contains(microphoneId);
    }
}
