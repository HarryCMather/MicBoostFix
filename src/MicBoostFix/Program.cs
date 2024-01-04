using System;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.Devices;
using MicBoostFix.ConsoleWindowManager;
using MicBoostFix.Settings;
using MicBoostFix.Models;
using SettingsModel = MicBoostFix.Models.Settings;

namespace MicBoostFix;

internal class Program
{
    private static async Task Main()
    {
        SettingsModel settings = await SettingsReader.ReadSettings();
        settings ??= await SettingsWriter.SetupInitialOptions();

        try
        {
            AudioDeviceController audioController = await GetAudioDeviceController(settings.MicrophoneId);
            ConsoleWindowStatusChanger.ChangeStatus(WindowStatus.Hide);
            await LevelCheckerLoop(audioController, settings);
        }
        catch (Exception ex)
        {
            ConsoleWindowStatusChanger.ChangeStatus(WindowStatus.Show);
            Console.WriteLine("An error occurred:  This likely means you have not granted the application permission to access your microphone in Windows Settings -> Privacy -> Microphone" +
                              $"{ex.Message}\n");
            Console.ReadKey();
        }
    }

    private static async Task LevelCheckerLoop(AudioDeviceController audioController, SettingsModel settings)
    {
        while (true)
        {
            if (audioController.VolumePercent > settings.MicrophoneLevel)
            {
                audioController.VolumePercent = settings.MicrophoneLevel;
            }
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
    
    private static async Task<AudioDeviceController> GetAudioDeviceController(string microphoneId)
    {
        MediaCapture mediaCapture = new();

        await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
        {
            MediaCategory = MediaCategory.Speech,
            StreamingCaptureMode = StreamingCaptureMode.Audio,
            AudioDeviceId = microphoneId
        });

        return mediaCapture.AudioDeviceController;
    }
}
