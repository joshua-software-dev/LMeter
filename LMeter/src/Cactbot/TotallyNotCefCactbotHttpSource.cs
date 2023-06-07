using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Dalamud.Logging;
using LMeter.Runtime;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;


namespace LMeter.Cactbot;

public class TotallyNotCefCactbotHttpSource : IDisposable
{
    public bool LastPollSuccessful { get; private set; } = false;
    public TotallyNotCefBrowserState WebBrowserState = TotallyNotCefBrowserState.NotStarted;
    public int PollingRate { get; set; } = 1000; // milliseconds
    public readonly CactbotState CactbotState;
    private readonly string _cactbotUrl;
    private readonly CancellationTokenSource _cancelTokenSource;
    private readonly bool _enableAudio;
    private readonly bool _forceStart;
    private readonly HttpClient _httpClient;
    private readonly HtmlParser _htmlParser;
    private readonly string _httpUrl;
    private readonly ushort _httpPort;
    private IHtmlDocument? _parsedResponse = null;

    public TotallyNotCefCactbotHttpSource(string cactbotUrl, ushort httpPort, bool enableAudio, bool forceStart)
    {
        CactbotState = new ();
        _cactbotUrl = cactbotUrl;
        _enableAudio = enableAudio;
        _forceStart = forceStart;
        _htmlParser = new ();
        _httpPort = httpPort;
        _httpUrl = $"http://127.0.0.1:{httpPort}";
        _httpClient = new ();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("curl/8.1.2");
        _cancelTokenSource = new ();
    }

    private async Task GetCactbotHtml()
    {
        _parsedResponse = null;

        try
        {
            var response = await _httpClient.GetAsync(_httpUrl, cancellationToken: _cancelTokenSource.Token);
            if (response.Content == null) return;
            var rawHtml = await response.Content.ReadAsStringAsync(_cancelTokenSource.Token);
            if (rawHtml == null) return;
            _parsedResponse = await _htmlParser.ParseDocumentAsync(rawHtml, _cancelTokenSource.Token);
            CactbotState.UpdateState(_parsedResponse);
            WebBrowserState = TotallyNotCefBrowserState.Connected;
        }
        catch (Exception e) when
        (
            e is OperationCanceledException ||
            e is TaskCanceledException ||
            e is HttpRequestException
        ) { }
    }

    private class GithubTagResponse
    {
        #pragma warning disable CS0649
        // JSON reflection is annoying
        [JsonProperty("name")]
        public string? Name;
        #pragma warning restore CS0649
    }

    private async Task<bool> IsTotallyNotCefUpToDate(string exePath)
    {
        FileVersionInfo localVersion;
        try
        {
            if (!exePath.EndsWith(".exe")) return false;
            var dllPath = exePath.Substring(0, exePath.Length - 3) + "dll";

            localVersion = FileVersionInfo.GetVersionInfo(dllPath);
            // don't bother checking the web if the file isn't even present / correct
            if (localVersion == null || localVersion.FileVersion == null) return false;
        }
        catch
        {
            return false;
        }

        PluginLog.Debug($"TotallyNotCef Version: {localVersion.FileVersion}");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync
            (
                MagicValues.TotallyNotCefUpdateCheckUrl,
                cancellationToken: _cancelTokenSource.Token
            );

            // assume up to date, github hasn't responded correctly.
            if (!response.IsSuccessStatusCode) return true;
        }
        catch (Exception e) when
        (
            e is OperationCanceledException ||
            e is TaskCanceledException ||
            e is HttpRequestException ||
            e is SocketException
        )
        {
            // same here.
            return true;
        }

        // same here too.
        if (response?.Content == null) return true;

        var rawJson = await response.Content.ReadAsStringAsync(_cancelTokenSource.Token);
        // same here three.
        if (rawJson == null) return true;

        GithubTagResponse[]? parsedJson;
        try
        {
            parsedJson = JsonConvert.DeserializeObject<GithubTagResponse[]>(rawJson);
            // same here four.
            if (parsedJson == null || parsedJson.Length < 1) return true;
        }
        catch (JsonSerializationException)
        {
            // same here five.
            return true;
        }

        var latestVersion = parsedJson[0].Name?.Replace("v", string.Empty);
        // same here six.
        if (latestVersion == null) return true;
        PluginLog.Debug($"Latest Version: {latestVersion}");

        return localVersion.FileVersion == latestVersion;
    }

    private async Task DownloadTotallyNotCef(string cefDirPath)
    {
        WebBrowserState = TotallyNotCefBrowserState.Downloading;
        PluginLog.Log("Downloading TotallyNotCef...");
        try
        {
            PluginLog.Log(MagicValues.TotallyNotCefDownloadUrl);
            using var response = await _httpClient.GetAsync
            (
                MagicValues.TotallyNotCefDownloadUrl,
                cancellationToken: _cancelTokenSource.Token
            );
            if (!response.IsSuccessStatusCode) return;

            using var streamToReadFrom = await response.Content.ReadAsStreamAsync(_cancelTokenSource.Token);
            using var zip = new ZipArchive(streamToReadFrom);
            zip.ExtractToDirectory(cefDirPath);
            PluginLog.Log("Finished extracting TotallyNotCef");
        }
        catch (Exception e) when
        (
            e is OperationCanceledException ||
            e is TaskCanceledException ||
            e is HttpRequestException ||
            e is SocketException
        )
        {
            return;
        }
    }

    private async Task DeleteTotallyNotCefInstall(string exePath)
    {
        var installDir = Path.GetDirectoryName(exePath);
        if (installDir == null) return;

        foreach (var process in Process.GetProcessesByName("TotallyNotCef"))
        {
            try
            {
                process.Kill();
            }
            catch
            {
                // Do not crash
            }
        }

        await Task.Delay(1000);

        try
        {
            Directory.Delete(installDir, recursive: true);
            return;
        }
        catch
        {
            return;
        }
    }

    private async Task StartTotallyNotCefProcess()
    {
        var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (pluginDir == null) return;
        var cefDirPath = Path.GetFullPath(Path.Combine(pluginDir, "../"));
        var cefExePath = Path.Combine(cefDirPath, $"TotallyNotCef{Path.DirectorySeparatorChar}TotallyNotCef.exe");

        if (File.Exists(cefExePath))
        {
            if (!(await IsTotallyNotCefUpToDate(cefExePath)))
            {
                await DeleteTotallyNotCefInstall(cefExePath);
                await DownloadTotallyNotCef(cefDirPath);
            }
            else
            {
                PluginLog.Log("TotallyNotCef is up to date.");
            }
        }
        else
        {
            await DownloadTotallyNotCef(cefDirPath);
        }

        WebBrowserState = TotallyNotCefBrowserState.Starting;
        ProcessLauncher.LaunchTotallyNotCef(cefExePath, _cactbotUrl, _httpPort, _enableAudio);
        WebBrowserState = TotallyNotCefBrowserState.Started;
    }

    private async Task PollCactbot()
    {
        if ((PluginManager.Instance?.CactbotConfig?.AutomaticallyStartBackgroundWebBrowser ?? false) || _forceStart)
        {
            await StartTotallyNotCefProcess();
        }
        else if (PluginManager.Instance?.CactbotConfig?.EnableConnection ?? false)
        {
            WebBrowserState = TotallyNotCefBrowserState.WaitingForConnection;
        }

        WebBrowserState = WebBrowserState switch
        {
            TotallyNotCefBrowserState.NotStarted => TotallyNotCefBrowserState.NotStarted,
            TotallyNotCefBrowserState.Started => TotallyNotCefBrowserState.Started,
            _ => TotallyNotCefBrowserState.WaitingForConnection
        };

        while (!_cancelTokenSource.IsCancellationRequested)
        {
            try
            {
                if (PluginManager.Instance?.CactbotConfig?.EnableConnection ?? false)
                {
                    await GetCactbotHtml();
                }
                else
                {
                    _parsedResponse = null;
                }

                if
                (
                    WebBrowserState != TotallyNotCefBrowserState.NotStarted &&
                    WebBrowserState != TotallyNotCefBrowserState.Started &&
                    WebBrowserState != TotallyNotCefBrowserState.WaitingForConnection
                )
                {
                    LastPollSuccessful = _parsedResponse != null;
                    WebBrowserState = LastPollSuccessful
                        ? TotallyNotCefBrowserState.Connected
                        : TotallyNotCefBrowserState.Disconnected;
                }

                await Task.Delay(PollingRate, _cancelTokenSource.Token);
            }
            catch (Exception e) when (e is OperationCanceledException || e is TaskCanceledException)
            {
                // Do not crash
            }
        }
    }

    private void PollingAsyncThread(object? _)
    {
        PollCactbot().GetAwaiter().GetResult();
    }

    public void StartBackgroundPollingThread()
    {
        ThreadPool.QueueUserWorkItem(PollingAsyncThread);
    }

    public void SendShutdownCommand()
    {
        try
        {
            _httpClient
                .GetAsync(_httpUrl + "/shutdown", cancellationToken: _cancelTokenSource.Token)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception e) when
        (
            e is OperationCanceledException ||
            e is TaskCanceledException ||
            e is HttpRequestException ||
            e is SocketException
        ) { }
    }

    public void Dispose()
    {
        SendShutdownCommand();

        try
        {
            _cancelTokenSource.Cancel();
        }
        catch { }
    }
}
