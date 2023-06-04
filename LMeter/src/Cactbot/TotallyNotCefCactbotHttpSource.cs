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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;


namespace LMeter.Cactbot;

public class TotallyNotCefCactbotHttpSource : IDisposable
{
    public bool LastPollSuccessful { get; private set; } = false;
    public int PollingRate { get; set; } = 1000; // milliseconds
    public bool PollingStarted = false;
    public readonly CactbotState CactbotState;
    private readonly string _cactbotUrl;
    private readonly CancellationTokenSource _cancelTokenSource;
    private readonly HttpClient _httpClient;
    private readonly HtmlParser _htmlParser;
    private readonly string _httpUrl;
    private readonly ushort _httpPort;
    private IHtmlDocument? _parsedResponse = null;

    public TotallyNotCefCactbotHttpSource(string cactbotUrl, ushort httpPort)
    {
        CactbotState = new ();
        _cactbotUrl = cactbotUrl;
        _htmlParser = new ();
        _httpPort = httpPort;
        _httpUrl = $"http://127.0.0.1:{httpPort}";
        _httpClient = new ();
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
            localVersion = FileVersionInfo.GetVersionInfo(exePath);
            // don't both checking the web if the file isn't even present / correct
            if (localVersion == null || localVersion.FileVersion == null) return false;
        }
        catch
        {
            return false;
        }

        var response = await _httpClient.GetAsync(_httpUrl, cancellationToken: _cancelTokenSource.Token);
        // assume up to date, github hasn't responded correctly.
        if (response.Content == null) return true;

        var rawJson = await response.Content.ReadAsStringAsync(_cancelTokenSource.Token);
        // same here.
        if (rawJson == null) return true;

        var parsedJson = JsonConvert.DeserializeObject<GithubTagResponse[]>(rawJson);
        // same here too.
        if (parsedJson == null || parsedJson.Length < 1) return true;

        var latestVersion = parsedJson[0].Name?.Replace("v", string.Empty);
        // same here three.
        if (latestVersion == null) return true;

        return localVersion.FileVersion == latestVersion;
    }

    private async Task DownloadTotallyNotCef(string cefDirPath)
    {
        PluginLog.Log("Downloading TotallyNotCef...");
        try
        {
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
            e is HttpRequestException
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
        var cefExePath = Path.Combine(cefDirPath, "TotallyNotCef", "TotallyNotCef.exe");

        if (File.Exists(cefExePath))
        {
            if (!(await IsTotallyNotCefUpToDate(cefExePath)))
            {
                await DeleteTotallyNotCefInstall(cefExePath);
                await DownloadTotallyNotCef(cefDirPath);
            }
        }
        else
        {
            await DownloadTotallyNotCef(cefDirPath);
        }

        ProcessLauncher.LaunchTotallyNotCef(cefExePath, _cactbotUrl, _httpPort);
    }

    private async Task PollCactbot()
    {
        await StartTotallyNotCefProcess();

        while (!_cancelTokenSource.IsCancellationRequested)
        {
            try
            {
                if (PluginManager.Instance?.CactbotConfig?.Enabled ?? false)
                {
                    await GetCactbotHtml();
                    LastPollSuccessful = _parsedResponse != null;
                }
                else
                {
                    LastPollSuccessful = false;
                }

                await Task.Delay(PollingRate, _cancelTokenSource.Token);
            }
            catch (Exception e) when (e is OperationCanceledException || e is TaskCanceledException)
            {
                // Do not crash
            }
        }
    }

    private void ThreadStartAsyncPolling(object? _)
    {
        PollCactbot().GetAwaiter().GetResult();
    }

    public void StartBackgroundPolling()
    {
        PollingStarted = true;
        ThreadPool.QueueUserWorkItem(ThreadStartAsyncPolling);
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
            e is HttpRequestException
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
