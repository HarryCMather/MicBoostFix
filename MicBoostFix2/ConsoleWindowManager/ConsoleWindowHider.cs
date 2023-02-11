using System;
using System.Runtime.InteropServices;

namespace MicBoostFix2.ConsoleWindowManager;

public static class ConsoleWindowHider
{
    private const int SwHide = 0;
    private const int SwShow = 5;
    
    public static void DisplayConsoleWindow(bool showWindow)
    {
        IntPtr consoleWindowHandle = GetConsoleWindow();
        ShowWindow(consoleWindowHandle, showWindow ? SwShow : SwHide);
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
