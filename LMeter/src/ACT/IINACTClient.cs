using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Logging;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin;
using LMeter.Config;
using LMeter.Helpers;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System;


namespace LMeter.ACT;

public enum SubscriptionStatus
{
    NotConnected,
    Connecting,
    ConnectionFailed,
    Connected,
    SubscriptionRequested,
    Subscribed,
    Unsubscribing,
    ShuttingDown
}

public class IINACTClient : IACTClient
{
    private readonly ACTConfig _config;
    private readonly DalamudPluginInterface _dpi;
    private ACTEvent? _lastEvent;
    private readonly ICallGateProvider<JObject, bool> subscriptionReceiver;

    private const string LMeterSubscriptionIpcEndpoint = "LMeter.SubscriptionReceiver";
    private const string IINACTListeningIpcEndpoint = "IINACT.Server.Listening";
    private const string IINACTSubscribeIpcEndpoint = "IINACT.CreateSubscriber";
    private const string IINACTUnsubscribeIpcEndpoint = "IINACT.Unsubscribe";
    private const string IINACTProviderEditEndpoint = "IINACT.IpcProvider." + LMeterSubscriptionIpcEndpoint;
    private static readonly JObject SubscriptionMessageObject = JObject.Parse(ACTClient.SubscriptionMessage);

    private SubscriptionStatus _status;
    public string Status => _status.ToString();

    public List<ACTEvent> PastEvents { get; private set; }

    public IINACTClient(ACTConfig config, DalamudPluginInterface dpi)
    {
        _config = config;
        _dpi = dpi;
        _status = SubscriptionStatus.NotConnected;
        PastEvents = new List<ACTEvent>();

        subscriptionReceiver = _dpi.GetIpcProvider<JObject, bool>(LMeterSubscriptionIpcEndpoint);
        subscriptionReceiver.RegisterFunc(ReceiveIpcMessage);
    }

    public bool ClientReady() =>
        _status == SubscriptionStatus.Subscribed;

    public bool ConnectionIncompleteOrFailed() =>
        _status == SubscriptionStatus.NotConnected || _status == SubscriptionStatus.ConnectionFailed;

    public ACTEvent? GetEvent(int index = -1)
    {
        if (index >= 0 && index < PastEvents.Count)
        {
            return PastEvents[index];
        }
            
        return _lastEvent;
    }

    public void EndEncounter()
    {
        ChatGui chat = Singletons.Get<ChatGui>();
        XivChatEntry message = new XivChatEntry()
        {
            Message = "end",
            Type = XivChatType.Echo
        };

        chat.PrintChat(message);
    }

    public void Clear()
    {
        _lastEvent = null;
        PastEvents = new List<ACTEvent>();
        if (_config.ClearACT)
        {
            ChatGui chat = Singletons.Get<ChatGui>();
            XivChatEntry message = new XivChatEntry()
            {
                Message = "clear",
                Type = XivChatType.Echo
            };

            chat.PrintChat(message);
        }
    }

    public void RetryConnection()
    {
        Reset();
        Start();
    }

    public void Start()
    {
        if (_status != SubscriptionStatus.NotConnected)
        {
            PluginLog.Error("Cannot start, IINACTClient needs to be reset!");
            return;
        }

        if (!Connect()) return;
        _status = SubscriptionStatus.Subscribed;;
        PluginLog.Information("Successfully subscribed to IINACT");
    }

    private bool Connect()
    {
        _status = SubscriptionStatus.Connecting;

        try
        {
            var connectSuccess = _dpi.GetIpcSubscriber<bool>(IINACTListeningIpcEndpoint).InvokeFunc();
            PluginLog.Verbose("Check if IINACT installed and running: " + connectSuccess);
            if (!connectSuccess) return false;
        }
        catch (Exception ex)
        {
            _status = SubscriptionStatus.ConnectionFailed;
            PluginLog.Information("IINACT server was not found or was not finished starting");
            PluginLog.Verbose(ex.ToString());
            return false;
        }
        _status = SubscriptionStatus.Connected;

        try
        {
            var subscribeSuccess = _dpi
                .GetIpcSubscriber<string, bool>(IINACTSubscribeIpcEndpoint)
                .InvokeFunc(LMeterSubscriptionIpcEndpoint);
            PluginLog.Verbose("Setup default empty IINACT subscription successfully: " + subscribeSuccess);
            if (!subscribeSuccess) return false;
        }
        catch (Exception ex)
        {
            _status = SubscriptionStatus.ConnectionFailed;
            PluginLog.Information("Failed to setup IINACT subscription!");
            PluginLog.Verbose(ex.ToString());
            return false;
        }
        _status = SubscriptionStatus.SubscriptionRequested;

        try
        {
            // no way to check this, hoping blindly that it always works ¯\_(ツ)_/¯
            PluginLog.Verbose($"""Updating subscription using endpoint: `{IINACTProviderEditEndpoint}`""");
            _dpi
                .GetIpcSubscriber<JObject, bool>(IINACTProviderEditEndpoint)
                .InvokeAction(SubscriptionMessageObject);
            PluginLog.Verbose($"""Subscription update message sent""");
            return true;
        }
        catch (Exception ex)
        {
            _status = SubscriptionStatus.ConnectionFailed;
            PluginLog.Information("Failed to finalize IINACT subscription!");
            PluginLog.Verbose(ex.ToString());
            return false;
        }
    }

    private bool ReceiveIpcMessage(JObject data)
    {
        try
        {
            ACTEvent? newEvent = data.ToObject<ACTEvent?>();

            if 
            (
                newEvent?.Encounter is not null &&
                newEvent?.Combatants is not null &&
                newEvent.Combatants.Any() &&
                (CharacterState.IsInCombat() || !newEvent.IsEncounterActive())
            )
            {
                var lastEventIsDifferentEncounterOrInvalid =
                (
                    _lastEvent is not null &&
                    _lastEvent.IsEncounterActive() == newEvent.IsEncounterActive() &&
                    _lastEvent.Encounter is not null &&
                    _lastEvent.Encounter.Duration.Equals(newEvent.Encounter.Duration)
                );

                if (!lastEventIsDifferentEncounterOrInvalid)
                {
                    if (!newEvent.IsEncounterActive())
                    {
                        PastEvents.Add(newEvent);

                        while (PastEvents.Count > _config.EncounterHistorySize)
                        {
                            PastEvents.RemoveAt(0);
                        }
                    }

                    newEvent.Timestamp = DateTime.UtcNow;
                    _lastEvent = newEvent;
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Verbose(ex.ToString());
            return false;
        }

        return true;
    }
        
    public void Shutdown()
    {
        _status = SubscriptionStatus.Unsubscribing;
        try
        {
            var success = _dpi
                .GetIpcSubscriber<string, bool>(IINACTUnsubscribeIpcEndpoint)
                .InvokeFunc(LMeterSubscriptionIpcEndpoint);

            PluginLog.Information(
                success
                    ? "Successfully unsubscribed from IINACT"
                    : "Failed to unsubscribe from IINACT"
            );
        }
        catch (Exception)
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

    public void Dispose()
    {
        subscriptionReceiver.UnregisterFunc();
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
