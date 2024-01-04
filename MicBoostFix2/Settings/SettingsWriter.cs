using System;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Storage;
using SettingsModel = MicBoostFix.Models.Settings;

namespace MicBoostFix.Settings;

public static class SettingsWriter
{
    public static async Task<SettingsModel> SetupInitialOptions()
    {
        SettingsModel settings = await GetSettings();

        await WriteSettings(settings);

        return settings;
    }

    private static async Task<SettingsModel> GetSettings()
    {
        string microphoneId = await GetMicrophoneId();
        float? microphoneLevel;

        Console.WriteLine("Enter the microphone level you wish to set in %, this value must be between 0 and 100 (decimal values are allowed):");
        while (true)
        {
            string microphoneLevelInput = Console.ReadLine();
            microphoneLevel = microphoneLevelInput.MicrophoneLevelValidation();

            if (microphoneLevel is not null)
            {
                break;
            }

            Console.WriteLine("Error: An invalid Microphone Level was supplied. Try again.");
        }

        return new SettingsModel
        {
            MicrophoneId = microphoneId,
            MicrophoneLevel = microphoneLevel.Value
        };
    }

    private static async Task WriteSettings(SettingsModel settings)
    {
        string settingsJson = JsonSerializer.Serialize(settings);
        
        StorageFolder folder = ApplicationData.Current.LocalFolder;
        StorageFile file = await folder.CreateFileAsync("MicSettings.txt");
        await FileIO.WriteTextAsync(file, settingsJson);
    }
    
    private static async Task<string> GetMicrophoneId()
    {
        DeviceInformationCollection microphones = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);

        while (true)
        {
            Console.WriteLine("Enter the number corresponding to the microphone you wish to control:");
            for (int count = 0; count < microphones.Count; count++)
            {
                Console.WriteLine($"{count + 1} - {microphones[count].Name}");
            }

            int? microphoneIdIndex = await Console.ReadLine().ValidateMicrophoneId(microphones);
            if (microphoneIdIndex is not null)
            {
                return microphones[microphoneIdIndex.Value].Id;
            }

            Console.WriteLine("Error: An invalid Microphone ID was supplied. Try again.");
        }
    }
}
