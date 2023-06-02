using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;


namespace LMeter.Runtime;

public static class WineChecker
{
    private static bool? _isRunningOnWine = null;
    public static bool IsRunningOnWine
    {
        get
        {
            if (_isRunningOnWine != null) return _isRunningOnWine.Value;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _isRunningOnWine = false;
                return _isRunningOnWine.Value;
            }

            using var registry = Registry.LocalMachine;
            _isRunningOnWine = registry.OpenSubKey("""Software\Wine""") != null;
            return _isRunningOnWine.Value;
        }
    }

    /// <summary>
    /// Split a directory in its components.
    /// Input e.g: a/b/c/d.
    /// Output: d, c, b, a.
    /// </summary>
    /// <param name="Dir"></param>
    /// <returns></returns>
    public static IEnumerable<string> DirectorySplit(this DirectoryInfo Dir)
    {
        while (Dir != null)
        {
            yield return Dir.Name;
            Dir = Dir!.Parent!;
        }
    }

    private static string? _linuxPrefixPath = null;
    public static string? WindowsFullPathToLinuxPath(string? inputPath)
    {
        if (string.IsNullOrEmpty(inputPath) || !Path.Exists(inputPath)) return null;

        if (_linuxPrefixPath == null)
        {
            var winePrefixPath = Environment.GetEnvironmentVariable("WINEPREFIX");
            if (winePrefixPath == null) return null;
            _linuxPrefixPath = winePrefixPath + "/dosdevices/";
        }

        var dirList = DirectorySplit(new DirectoryInfo(inputPath)).ToList();
        if (dirList.Count < 1) return null;
        dirList.Reverse();
        dirList[0] = dirList[0].ToLowerInvariant().Replace("\\", string.Empty); // transforms `C:\` to `c:`

        return _linuxPrefixPath + string.Join('/', dirList);
    }
}
