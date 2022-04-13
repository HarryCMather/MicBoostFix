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

            Console.WriteLine("Error: You did not select a valid microphone");
            return await GetMicrophoneId();
        }

        /// <summary>
        /// Prompt the user to input a microphone level and ensure it is between 0 and 100 (percent)
        /// </summary>
        /// <returns>Float of the microphone level between 0 and 100</returns>
        private static float GetMicrophoneLevel()
        {
            bool success = false;
            float microphoneLevel = -1f;
            while (!success)
            {
                Console.WriteLine("Enter the microphone level you wish to set in %:");
                success = float.TryParse(Console.ReadLine(), out microphoneLevel) && (microphoneLevel >= 0.0 && microphoneLevel <= 100.0);
            }
            return microphoneLevel;
        }

        /// <summary>
        /// See if the microphone inputted from file was a valid microphone
        /// </summary>
        /// <param name="inputId"></param>
        /// <returns>True if the microphone id is in the users list of microphones, otherwise false</returns>
        private static async Task<bool> CheckValidMicrophoneId(string inputId)
        {
            return (await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture)).Any(microphone => microphone.Id == inputId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static async Task<Settings> SetupInitialOptions()
        {
            //Output the microphone ids of every input device plugged into the machine, prompt the user to select one
            string microphoneId = await GetMicrophoneId();

            //Prompt the user to input a microphone level and ensure it is between 0 and 100 (percent)
            float microphoneLevel = GetMicrophoneLevel();

            //If the value is not a whole number, round it to the nearest whole number:
            microphoneLevel = Convert.ToSingle(Math.Round(Convert.ToDouble(microphoneLevel), 0, MidpointRounding.AwayFromZero));

            //Write the valid values to the 'MicSettings.txt' file:
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.CreateFileAsync("MicSettings.txt");
            await FileIO.WriteTextAsync(file, $"{microphoneId}\n{microphoneLevel}");

            //Create a new instance of 'Settings' with the valid settings:
            return new Settings
            {
                MicrophoneId = microphoneId,
                MicrophoneLevel = microphoneLevel
            };
        }

        /// <summary>
        /// Used to display an error message, wait 5 seconds then exit the program
        /// </summary>
        /// <param name="messageVariation">Where the error occurred in the programs execution</param>
        private static void ExitProgram(string messageVariation)
        {
            Console.WriteLine($"You did not enter a valid microphone {messageVariation}, the program will exit in 5 seconds...");
            Thread.Sleep(5000);
            Environment.Exit(0);
        }

        /// <summary>
        /// Attempts to load settings from the 'MicSettings.txt' file in AppData
        /// </summary>
        /// <returns>A new instance of 'Settings' if loaded values were valid and file exists, otherwise null</returns>
        private static async Task<Settings> GetSettings()
        {
            //Check if the settings file exist, if it doesn't don't try to load it:
            if (await ApplicationData.Current.LocalFolder.TryGetItemAsync("MicSettings.txt") == null)
            {
                return null;
            }

            //Otherwise, specify the file and load the files contents into a string array:
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("MicSettings.txt");
            string[] contents = (await FileIO.ReadTextAsync(file)).Split('\n');

            //If the second item (microphone level) cannot be converted to a float, do not continue:
            if (!float.TryParse(contents[1], out float microphoneLevel))
            {
                return null;
            }

            //If the microphone level (loaded from txt file) is parseable as a float, but invalid then exit the program with an error regarding the microphone level:
            if (microphoneLevel < 0.0 || microphoneLevel > 100.0)
            {
                ExitProgram("level");
            }

            //If the microphone id (loaded from txt file) is not a currently available microphone, then exit the program with an error regarding the microphone id:
            if (!await CheckValidMicrophoneId(contents[0]))
            {
                ExitProgram("ID in the file - this was likely manually modified");
            }

            //If the settings were successfully loaded with none of the above errors, create a new instance of Settings with the contents loaded from the file:
            return new Settings
            {
                MicrophoneId = contents[0],
                MicrophoneLevel = microphoneLevel
            };
        }

        /// <summary>
        /// Creates a new instance of 'AudioDeviceController', used to adjust the levels for a given microphone
        /// </summary>
        /// <param name="settings">Used to specify which microphone to control</param>
        /// <returns>New instance of 'AudioDeviceController' for a given microphone</returns>
        private static async Task<AudioDeviceController> GetAudioDeviceController(Settings settings)
        {
            //Create new instance of 'MediaCapture'
            MediaCapture mediaCapture = new();

            //Initialize the mediaCapture instance with the given microphone id
            await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
            {
                MediaCategory = MediaCategory.Speech,
                StreamingCaptureMode = StreamingCaptureMode.Audio,
                AudioDeviceId = settings.MicrophoneId
            });

            //Return the initialized audio controller:
            return mediaCapture.AudioDeviceController;
        }

        /// <summary>
        /// Get the console window's handle, then uses it to hide the window
        /// </summary>
        private static void HideConsoleWindow()
        {
            IntPtr consoleWindowHandle = GetConsoleWindow();
            ShowWindow(consoleWindowHandle, 0);
        }

        /// <summary>
        /// Returns a handle referring to the current console window through the use of a Win32 API call
        /// </summary>
        /// <returns>A pointer specifying the handle of the current program</returns>
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        /// <summary>
        /// Creates new call to Win32 API for 'ShowWindow' allowing you to show or hide the console window
        /// </summary>
        /// <param name="hWnd">A pointer specifying the handle of the current program</param>
        /// <param name="nCmdShow">An integer whether to show (5) or hide (0) the console window</param>
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
