using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Storage;
using MicBoostFix2.ConsoleWindowManager;
using Newtonsoft.Json;

namespace MicBoostFix2.SettingManagement;

public static class SettingsWriter
{
    public static async Task<Settings> SetupInitialOptions()
    {
        Settings settings = await GetSettings();

        await WriteSettings(settings);

        return settings;
    }

    private static async Task<Settings> GetSettings()
    {
        string microphoneId = await GetMicrophoneId();

        Console.WriteLine("Enter the microphone level you wish to set in %:");
        string microphoneLevelInput = Console.ReadLine();
        if (!float.TryParse(microphoneLevelInput, out float microphoneLevel))
        {
            ConsoleWindowHider.DisplayConsoleWindow(true);
            throw new ArgumentOutOfRangeException(nameof(microphoneLevel), "Error: Microphone Level was invalid. Exiting program.");
        }

        if (microphoneLevel < 0.0 || microphoneLevel > 100.0)
        {
            ConsoleWindowHider.DisplayConsoleWindow(true);
            throw new ArgumentOutOfRangeException(nameof(microphoneId), "Error: Microphone Level was invalid. Exiting program.");
        }

        return new Settings
        {
            MicrophoneId = microphoneId,
            MicrophoneLevel = Convert.ToSingle(Math.Floor(Convert.ToDouble(microphoneLevel)))
        };
    }

    private static async Task WriteSettings(Settings settings)
    {
        string settingsJson = JsonConvert.SerializeObject(settings);
        
        StorageFolder folder = ApplicationData.Current.LocalFolder;
        StorageFile file = await folder.CreateFileAsync("MicSettings.txt");
        await FileIO.WriteTextAsync(file, settingsJson);
    }
    
    private static async Task<string> GetMicrophoneId()
    {
        DeviceInformationCollection microphones = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
        Console.WriteLine("Enter the number corresponding to the microphone you wish to control:");
        for (int count = 0; count < microphones.Count; count++)
        {
            Console.WriteLine($"{count + 1} - {microphones[count].Name}");
        }

        if (int.TryParse(Console.ReadLine(), out int selectedMicrophoneIndex) && (selectedMicrophoneIndex - 1 >= 0 && selectedMicrophoneIndex - 1 < microphones.Count))
        {
            return microphones[selectedMicrophoneIndex - 1].Id;
        }

        ConsoleWindowHider.DisplayConsoleWindow(true);
        throw new ArgumentOutOfRangeException(nameof(selectedMicrophoneIndex), "Error: Microphone ID was invalid. Exiting program.");
    }
}
