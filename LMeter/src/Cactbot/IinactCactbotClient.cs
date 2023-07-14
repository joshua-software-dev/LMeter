using Dalamud.Game.ClientState;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using LMeter.Act;
using LMeter.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace LMeter.Cactbot;

public class IinactCactbotClient : IActClient
{
    private readonly bool _bypassWebSocket;
    private readonly ClientState _clientState;
    private readonly CancellationTokenSource _cancelTokenSource;
    private readonly DalamudPluginInterface _dpi;
    private readonly HttpClient _httpClient;
    private readonly ICallGateProvider<JObject, bool>? subscriptionReceiver;
    private readonly string _totallyNotCefUrl;

    private const string LMeterCactbotSubscriptionIpcEndpoint = "LMeter.Cactbot.SubscriptionReceiver";
    private const string IinactListeningIpcEndpoint = IinactClient.IinactListeningIpcEndpoint;
    private const string IinactSubscribeIpcEndpoint = IinactClient.IinactSubscribeIpcEndpoint;
    private const string IinactUnsubscribeIpcEndpoint = IinactClient.IinactUnsubscribeIpcEndpoint;
    private const string IinactProviderEditEndpoint = "IINACT.IpcProvider." + LMeterCactbotSubscriptionIpcEndpoint;
    private const string RseqMessage0 = """{"data":{"general":{"DisplayLanguage":"en"}},"rseq":0}""";
    private const string RseqMessage1 = """{"detail":{"userLocation":"","localUserFiles":{},"parserLanguage":"en","systemLocale":"en-US","displayLanguage":"en","language":"en"},"rseq":1}""";
    private const string RseqMessage2 = """{"$isNull":true,"rseq":2}""";
    private static readonly JObject SubscriptionMessageObject =
        JObject.Parse
        (
            """
            {
                "call": "subscribe",
                "events": [
                    "ChangeZone",
                    "LogLine",
                    "onForceReload",
                    "onInCombatChangedEvent",
                    "onLogEvent",
                    "onPlayerChangedEvent",
                    "onUserFileChanged",
                    "PartyChanged"
                ]
            }
            """
        );

    public List<ActEvent> PastEvents
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    private bool _fakeHandshakeComplete = false;
    private SubscriptionStatus _status;
    private string? _lastErrorMessage;

    public IinactCactbotClient
    (
        bool bypassWebSocket,
        ClientState clientState,
        CancellationTokenSource cts,
        DalamudPluginInterface dpi,
        HttpClient httpClient,
        string totallyNotCefUrl
    )
    {
        _bypassWebSocket = bypassWebSocket;
        _clientState = clientState;
        _cancelTokenSource = cts;
        _dpi = dpi;
        _httpClient = httpClient;
        _status = SubscriptionStatus.NotConnected;
        _totallyNotCefUrl = totallyNotCefUrl;

        _clientState.Login += HandleOnLogin;

        try
        {
            subscriptionReceiver = _dpi.GetIpcProvider<JObject, bool>(LMeterCactbotSubscriptionIpcEndpoint);
            subscriptionReceiver.RegisterFunc(ReceiveIpcMessage);
        }
        catch { }
    }

    private void HandleOnLogin(object? sender, EventArgs args)
    {
        if (_bypassWebSocket && !_fakeHandshakeComplete)
        {
            RetryConnection();
        }
    }

    private void SendHttpPostRequest(string message)
    {
        _httpClient
            .PostAsync
            (
                _totallyNotCefUrl,
                new StringContent
                (
                    message,
                    Encoding.UTF8,
                    "application/json"
                ),
                _cancelTokenSource.Token
            )
            .GetAwaiter()
            .GetResult();
    }

    private bool ReceiveIpcMessage(JObject data)
    {
        if (!_fakeHandshakeComplete || !_bypassWebSocket)
        {
            return false;
        }

        try
        {
            SendHttpPostRequest(data.ToString(Formatting.None));
        }
        catch (Exception e) when
        (
            e is OperationCanceledException ||
            e is TaskCanceledException ||
            e is HttpRequestException ||
            e is SocketException
        )
        {
            return false;
        }

        return true;
    }

    public void Clear() =>
        throw new NotImplementedException();

    public bool ClientReady() =>
        throw new NotImplementedException();

    public bool ConnectionIncompleteOrFailed() =>
        throw new NotImplementedException();

    public void DrawConnectionStatus() =>
        throw new NotImplementedException();

    public void EndEncounter() =>
        throw new NotImplementedException();

    public ActEvent? GetEvent(int index = -1) =>
        throw new NotImplementedException();

    private bool Connect()
    {
        _status = SubscriptionStatus.Connecting;

        try
        {
            var connectSuccess = _dpi.GetIpcSubscriber<bool>(IinactListeningIpcEndpoint).InvokeFunc();
            PluginLog.Verbose("CACTBOT | Check if IINACT installed and running: " + connectSuccess);
            if (!connectSuccess) return false;
        }
        catch (Exception ex)
        {
            _status = SubscriptionStatus.ConnectionFailed;
            _lastErrorMessage = "CACTBOT | IINACT server was not found or was not finished starting.";
            PluginLog.Information(_lastErrorMessage);
            _lastErrorMessage = _lastErrorMessage + "\n\n" + ex;
            PluginLog.Verbose(_lastErrorMessage);
            return false;
        }
        _status = SubscriptionStatus.Connected;
        PluginLog.Information("CACTBOT | Successfully discovered IINACT IPC endpoint");

        try
        {
            var subscribeSuccess = _dpi
                .GetIpcSubscriber<string, bool>(IinactSubscribeIpcEndpoint)
                .InvokeFunc(LMeterCactbotSubscriptionIpcEndpoint);
            PluginLog.Verbose("CACTBOT | Setup default empty IINACT subscription successfully: " + subscribeSuccess);
            if (!subscribeSuccess) return false;
        }
        catch (Exception ex)
        {
            _status = SubscriptionStatus.ConnectionFailed;
            _lastErrorMessage = "CACTBOT | Failed to setup IINACT subscription!";
            PluginLog.Information(_lastErrorMessage);
            _lastErrorMessage = _lastErrorMessage + "\n\n" + ex;
            PluginLog.Verbose(_lastErrorMessage);
            return false;
        }
        _status = SubscriptionStatus.Subscribing;

        try
        {
            // no way to check this, hoping blindly that it always works ¯\_(ツ)_/¯
            PluginLog.Verbose($"""CACTBOT | Updating subscription using endpoint: `{IinactProviderEditEndpoint}`""");
            _dpi
                .GetIpcSubscriber<JObject, bool>(IinactProviderEditEndpoint)
                .InvokeAction(SubscriptionMessageObject);
            PluginLog.Verbose($"""CACTBOT | Subscription update message sent""");
            _status = SubscriptionStatus.Subscribed;
            PluginLog.Information("CACTBOT | Successfully subscribed to combat events from IINACT IPC");
            return true;
        }
        catch (Exception ex)
        {
            _status = SubscriptionStatus.ConnectionFailed;
            _lastErrorMessage = "CACTBOT | Failed to finalize IINACT subscription!";
            PluginLog.Information(_lastErrorMessage);
            _lastErrorMessage = _lastErrorMessage + "\n\n" + ex;
            PluginLog.Verbose(_lastErrorMessage);
            return false;
        }
    }

    public void Start()
    {
        if (_status != SubscriptionStatus.NotConnected)
        {
            PluginLog.Error("CACTBOT | Cannot start, IINACTCactbotClient needs to be reset!");
            return;
        }
        else if (!_bypassWebSocket)
        {
            PluginLog.Information("CACTBOT | Bypass WebSocket disabled by config");
            return;
        }

        if (Connect())
        {
            try
            {
                for (var i = 0; i < 10; i++)
                {
                    try
                    {
                        SendHttpPostRequest(RseqMessage0);
                        break;
                    }
                    catch (Exception e) when
                    (
                        e is OperationCanceledException ||
                        e is TaskCanceledException ||
                        e is HttpRequestException ||
                        e is SocketException
                    )
                    {
                        PluginLog.Log($"CACTBOT | Connection attempt #{i} failed, trying again after a delay...");
                        Task.Delay(1000, _cancelTokenSource.Token).GetAwaiter().GetResult();
                    }
                }

                SendHttpPostRequest(RseqMessage1);

                var (locId, locName) = CharacterState.GetCharacterLocation();
                if (locId == 0 || locName == null)
                {
                    PluginLog.Error("CACTBOT | Failed to get current player location");
                    _status = SubscriptionStatus.ConnectionFailed;
                    _fakeHandshakeComplete = false;
                    return;
                }

                SendHttpPostRequest
                (
                    $$"""
                    {"type":"ChangeZone","zoneID":{{locId}},"zoneName":"{{locName}}"}
                    """
                );
                SendHttpPostRequest(RseqMessage2);
                PluginLog.Log("CACTBOT | Fake Handshake Complete");
            }
            catch (Exception e) when
            (
                e is OperationCanceledException ||
                e is TaskCanceledException ||
                e is HttpRequestException ||
                e is SocketException
            )
            {
                PluginLog.Error("CACTBOT | Fake Handshake Failed");
                _fakeHandshakeComplete = false;
                return;
            }

            _fakeHandshakeComplete = true;
        }
    }

    public void Shutdown()
    {
        _fakeHandshakeComplete = false;
        _status = SubscriptionStatus.Unsubscribing;

        try
        {
            var success = _dpi
                .GetIpcSubscriber<string, bool>(IinactUnsubscribeIpcEndpoint)
                .InvokeFunc(LMeterCactbotSubscriptionIpcEndpoint);

            PluginLog.Information(
                success
                    ? "CACTBOT | Successfully unsubscribed from IINACT IPC"
                    : "CACTBOT | Failed to unsubscribe from IINACT IPC"
            );
        }
        catch
        {
            // don't throw when closing
        }

        _status = SubscriptionStatus.ShuttingDown;
    }

    public void Reset()
    {
        this.Shutdown();
        _status = SubscriptionStatus.NotConnected;
    }

    public void RetryConnection()
    {
        Reset();
        Start();
    }

    public void Dispose()
    {
        _fakeHandshakeComplete = false;
        _clientState.Login -= HandleOnLogin;
        subscriptionReceiver?.UnregisterFunc();
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.Shutdown();
        }
    }
}
