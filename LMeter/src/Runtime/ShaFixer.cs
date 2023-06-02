using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;


namespace LMeter.Runtime;

public static class ShaFixer
{
    private static bool? _shaIsFunctional;
    public static bool ValidateSha1IsFunctional()
    {
        if (_shaIsFunctional != null) return _shaIsFunctional.Value;

        try
        {
            var bytes = new byte[128];
            System.Security.Cryptography.SHA1.TryHashData(bytes, bytes, out var _);
            _shaIsFunctional = true;
            return _shaIsFunctional.Value;
        }
        catch (AccessViolationException)
        {
            _shaIsFunctional = false;
            return _shaIsFunctional.Value;
        }
    }

    public static bool ModifyRuntimeWithShaFix()
    {
        var originalDllPath = System.Reflection.Assembly.GetAssembly(typeof(System.Security.Cryptography.SHA1))?.Location;
        if (originalDllPath == null) return false;

        var dllDir = Path.GetDirectoryName(originalDllPath);
        var newDllPath = Path.Join(dllDir, "System.Security.Cryptography2.dll");
        var jsonPath = dllDir + Path.DirectorySeparatorChar + "..\\..\\..\\hashes-7.0.0.json";
        var hashJsonDict = JsonSerializer.Deserialize<Dictionary<string, string?>>(File.ReadAllText(jsonPath));
        if (hashJsonDict == null) return false;

        if (File.Exists(newDllPath)) File.Delete(newDllPath);
        using (var httpClient = new HttpClient())
        {
            using (var stream = httpClient.GetStreamAsync(MagicValues.PatchedCryptographyDllUrl).GetAwaiter().GetResult())
            {
                using (var fileStream = new FileStream(newDllPath, FileMode.CreateNew))
                {
                    stream.CopyTo(fileStream);
                }
            }
        }

        var md5 = MonoMD5CryptoServiceProvider.Create();
        var hashed = md5.ComputeHash(File.ReadAllBytes(newDllPath));
        var sb = new StringBuilder();
        foreach (var bt in hashed)
        {
            sb.Append(bt.ToString("x2"));
        }

        hashJsonDict["shared\\Microsoft.NETCore.App\\7.0.0\\System.Security.Cryptography.dll"] = sb
            .ToString()
            .ToUpperInvariant();
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(hashJsonDict));

        ProcessLauncher.LaunchInstallFixDll(newDllPath, originalDllPath);
        return true;
    }
}
