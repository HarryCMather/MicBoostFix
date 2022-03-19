using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Storage;

namespace MicBoostFix2
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Settings settings = await GetSettings() ?? await SetupInitialOptions();

            try
            {
                AudioDeviceController audioController = await GetAudioDeviceController(settings);
                HideConsoleWindow();
                LevelCheckerLoop(audioController, settings);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred:  This likely means you have not granted the application permission to access your microphone in Windows Settings -> Privacy -> Microphone" +
                                  $"{ex.Message}\n");
                Console.ReadKey(); 
            }
        }

        private static void LevelCheckerLoop(AudioDeviceController audioController, Settings settings)
        {
            while (true)
            {
                if (audioController.VolumePercent > settings.MicrophoneLevel)
                {
                    audioController.VolumePercent = settings.MicrophoneLevel;
                }
                Thread.Sleep(1000);
            }
        }

        private static async Task<string> GetMicrophoneId()
        {
            DeviceInformationCollection microphones = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
            Console.WriteLine("Enter the number corresponding to the microphone you wish to control:");
            for (int count = 0; count < microphones.Count; count++)
            {
                Console.WriteLine($"{count + 1} - {microphones[count].Name}");
            }

            if (int.TryParse(Console.ReadLine(), out int selectedMicrophoneIndex) && ((selectedMicrophoneIndex - 1) >= 0 && selectedMicrophoneIndex - 1 < microphones.Count))
            {
                return microphones[selectedMicrophoneIndex - 1].Id;
            }

            return null;
        }

        private static async Task<bool> CheckValidMicrophoneId(string inputId)
        {
            string[] microphoneIds = (await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture)).Select(microphone => microphone.Id).ToArray();
            return microphoneIds.Contains(inputId);
        }

        private static async Task<Settings> SetupInitialOptions()
        {
            string microphoneId = await GetMicrophoneId();

            if (microphoneId is null)
            {
                ExitProgram("id");
            }

            Console.WriteLine("Enter the microphone level you wish to set in %:");
            if (!float.TryParse(Console.ReadLine(), out float microphoneLevel))
            {
                ExitProgram("level");
            }
            else
            {
                if (microphoneLevel < 0.0 || microphoneLevel > 100.0)
                {
                    ExitProgram("level");
                }
            }

            Settings settings = new Settings
            {
                MicrophoneId = microphoneId,
                MicrophoneLevel = Convert.ToSingle(Math.Round(Convert.ToDouble(microphoneLevel), 0, MidpointRounding.AwayFromZero))
            };

            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.CreateFileAsync("MicSettings.txt");
            await FileIO.WriteTextAsync(file, $"{settings.MicrophoneId}\n{settings.MicrophoneLevel}");

            return settings;
        }

        private static void ExitProgram(string messageVariation)
        {
            Console.WriteLine($"You did not enter a valid microphone {messageVariation}, the program will exit in 5 seconds...");
            Thread.Sleep(5000);
            Environment.Exit(0);
        }

        private static async Task<Settings> GetSettings()
        {
            if (await ApplicationData.Current.LocalFolder.TryGetItemAsync("MicSettings.txt") == null)
            {
                return null;
            }

            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("MicSettings.txt");
            string[] contents = (await FileIO.ReadTextAsync(file)).Split('\n');

            if (!float.TryParse(contents[1], out float microphoneLevel))
            {
                return null;
            }

            if (microphoneLevel < 0.0 || microphoneLevel > 100.0)
            {
                ExitProgram("level");
            }

            if (!await CheckValidMicrophoneId(contents[0]))
            {
                ExitProgram("ID in the file - this was likely manually modified");
            }

            return new Settings
            {
                MicrophoneId = contents[0],
                MicrophoneLevel = microphoneLevel
            };
        }

        private static async Task<AudioDeviceController> GetAudioDeviceController(Settings settings)
        {
            MediaCapture mediaCapture = new MediaCapture();

            await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
            {
                MediaCategory = MediaCategory.Speech,
                StreamingCaptureMode = StreamingCaptureMode.Audio,
                AudioDeviceId = settings.MicrophoneId
            });

            return mediaCapture.AudioDeviceController;
        }

        private static void HideConsoleWindow()
        {
            IntPtr consoleWindowHandle = GetConsoleWindow();
            ShowWindow(consoleWindowHandle, SW_HIDE);
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;  //not used
    }
}
