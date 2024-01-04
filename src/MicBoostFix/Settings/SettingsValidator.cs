using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using SettingsModel = MicBoostFix.Models.Settings;

namespace MicBoostFix.Settings;

public static class SettingsValidator
{
    public static async Task<bool> ReadValidation(this SettingsModel settings)
    {
        if (!settings.MicrophoneLevel.IsValidMicrophoneLevelRange() ||
            !(await IsValidMicrophoneId(settings.MicrophoneId)))
        {
            return false;
        }

        return true;
    }

    public static float? MicrophoneLevelValidation(this string microphoneLevelInput)
    {
        if (!float.TryParse(microphoneLevelInput, out float microphoneLevel) ||
            !IsValidMicrophoneLevelRange(microphoneLevel))
        {
            return null;
        }

        return microphoneLevel;
    }

    public static async Task<int?> ValidateMicrophoneId(this string microphoneIdInput, DeviceInformationCollection microphones)
    {
        bool validMicrophoneId = int.TryParse(microphoneIdInput, out int microphoneIdIndex);

        // Subtract 1 because the prompt to the user started at 1, and indexing starts at 0:
        microphoneIdIndex--;
        if (microphoneIdIndex < 0 || microphoneIdIndex >= microphones.Count)
        {
            validMicrophoneId = false;
        }

        if (!validMicrophoneId || !(await IsValidMicrophoneId(microphoneIdInput)))
        {
            return null;
        }

        return microphoneIdIndex;
    }

    private static bool IsValidMicrophoneLevelRange(this float microphoneLevel)
    {
        return microphoneLevel is >= 0.0f and <= 100.0f;
    }

    private static async Task<bool> IsValidMicrophoneId(string microphoneId, DeviceInformationCollection microphones = null)
    {
        microphones ??= await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
        return microphones?.Any(microphone => microphone?.Id == microphoneId) ?? false;
    }
}
