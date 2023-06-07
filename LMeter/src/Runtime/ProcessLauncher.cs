using Dalamud.Logging;
using System;
using System.Diagnostics;
using System.Linq;


namespace LMeter.Runtime;

public static class ProcessLauncher
{
    public static void LaunchTotallyNotCef(string exePath, string cactbotUrl, ushort httpPort, bool enableAudio)
    {
        if (Process.GetProcessesByName("TotallyNotCef").Any()) return;

        var process = new Process();
        process.EnableRaisingEvents = true;
        process.OutputDataReceived += new DataReceivedEventHandler(OnStdOutMessage);
        process.ErrorDataReceived += new DataReceivedEventHandler(OnStdErrMessage);
        process.Exited += (_, _) => PluginLog.Log($"{exePath} exited with code {process?.ExitCode}");

        process.StartInfo.FileName = exePath;
        process.StartInfo.Arguments = cactbotUrl + " " + httpPort + " " + (enableAudio ? 1 : 0);

        PluginLog.Log($"EXE : {process.StartInfo.FileName}");
        PluginLog.Log($"ARGS: {process.StartInfo.Arguments}");

        process.StartInfo.EnvironmentVariables["DOTNET_ROOT"] = Environment.GetEnvironmentVariable("DALAMUD_RUNTIME");
        process.StartInfo.EnvironmentVariables.Remove("DOTNET_BUNDLE_EXTRACT_BASE_DIR");
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardOutput = true;

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch
        {
            // Prefer not crashing to not starting
        }
    }

    public static void LaunchInstallFixDll(string winNewDllPath, string winOldDllPath)
    {
        var linNewDllPath = WineChecker.WindowsFullPathToLinuxPath(winNewDllPath);
        var linOldDllPath = WineChecker.WindowsFullPathToLinuxPath(winNewDllPath);
        if (linNewDllPath == null || linOldDllPath == null)
        {
            PluginLog.LogError("Could not install DLL fix.");
        }

        var process = new Process();
        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => PluginLog.Log($"Process exited with code {process?.ExitCode}");

        process.StartInfo.FileName = "/usr/bin/env";
        process.StartInfo.Arguments = $"mv {linNewDllPath} {linOldDllPath}";

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardError = false;
        process.StartInfo.RedirectStandardOutput = false;

        try
        {
            process.Start();
        }
        catch
        {
            // Prefer not crashing to not starting
        }
    }

    private static void OnStdErrMessage(object? sender, DataReceivedEventArgs e) =>
        PluginLog.Debug($"STDERR: {e.Data}\n");

    private static void OnStdOutMessage(object? sender, DataReceivedEventArgs e) =>
        PluginLog.Verbose($"STDOUT: {e.Data}\n");
}
