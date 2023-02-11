using System;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.Devices;
using MicBoostFix2.ConsoleWindowManager;
using MicBoostFix2.SettingManagement;

namespace MicBoostFix2;

class Program
{
    private static async Task Main()
    {
        Settings settings = await SettingsReader.ReadSettings() ?? await SettingsWriter.SetupInitialOptions();

        try
        {
            AudioDeviceController audioController = await GetAudioDeviceController(settings.MicrophoneId);
            ConsoleWindowHider.DisplayConsoleWindow(false);
            await LevelCheckerLoop(audioController, settings);
        }
        catch (Exception ex)
        {
            ConsoleWindowHider.DisplayConsoleWindow(true);
            Console.WriteLine("An error occurred:  This likely means you have not granted the application permission to access your microphone in Windows Settings -> Privacy -> Microphone" +
                              $"{ex.Message}\n");
            Console.ReadKey(); 
        }
    }

    private static async Task LevelCheckerLoop(AudioDeviceController audioController, Settings settings)
    {
        while (true)
        {
            if (audioController.VolumePercent > settings.MicrophoneLevel)
            {
                audioController.VolumePercent = settings.MicrophoneLevel;
            }
            await Task.Delay(1000);
        }
    }
    
    private static async Task<AudioDeviceController> GetAudioDeviceController(string microphoneId)
    {
        MediaCapture mediaCapture = new MediaCapture();

        await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
        {
            MediaCategory = MediaCategory.Speech,
            StreamingCaptureMode = StreamingCaptureMode.Audio,
            AudioDeviceId = microphoneId
        });

        return mediaCapture.AudioDeviceController;
    }
}
