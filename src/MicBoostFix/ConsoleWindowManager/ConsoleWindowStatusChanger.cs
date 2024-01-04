using System;
using System.Runtime.InteropServices;
using MicBoostFix.Models;

namespace MicBoostFix.ConsoleWindowManager;

public static class ConsoleWindowStatusChanger
{
    public static void ChangeStatus(WindowStatus windowState)
    {
        IntPtr consoleWindowHandle = GetConsoleWindow();
        ShowWindow(consoleWindowHandle, (int)windowState);
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
