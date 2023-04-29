using Dalamud.Logging;
using Dalamud.Plugin;
using LMeter.Config;
using LMeter.Helpers;
using System.Collections.Generic;
using System;
using System.Linq;


namespace LMeter.Act;

public interface IActClient : IPluginDisposable
{
    public static IActClient Current =>
        Singletons.Get<LMeterConfig>().ActConfig.IinactMode 
            ? Singletons.Get<IinactClient>() 
            : Singletons.Get<ActWebSocketClient>();

    public static IActClient GetNewClient()
    {
        Singletons.DeleteActClients();

        ActConfig config = Singletons.Get<LMeterConfig>().ActConfig;
        DalamudPluginInterface dpi = Singletons.Get<DalamudPluginInterface>();

        IActClient client = config.IinactMode
            ? new IinactClient(config, dpi)
            : new ActWebSocketClient(config, dpi);
        Singletons.Register(client);
        return client;
    }

    public ActEvent? LastEvent { get; set; }
    public List<ActEvent> PastEvents { get; }

    public void Clear();
    public bool ClientReady();
    public bool ConnectionIncompleteOrFailed();
    public void DrawConnectionStatus();
    public void EndEncounter();
    public ActEvent? GetEvent(int index = -1);
    public void Start();
    public void RetryConnection();

    public bool ParseNewEvent(ActEvent? newEvent, int encounterHistorySize)
    {
        try
        {
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
                    LastEvent is not null &&
                    LastEvent.IsEncounterActive() == newEvent.IsEncounterActive() &&
                    LastEvent.Encounter is not null &&
                    LastEvent.Encounter.Duration.Equals(newEvent.Encounter.Duration)
                );

                if (!lastEventIsDifferentEncounterOrInvalid)
                {
                    if (!newEvent.IsEncounterActive())
                    {
                        PastEvents.Add(newEvent);

                        while (PastEvents.Count > encounterHistorySize)
                        {
                            PastEvents.RemoveAt(0);
                        }
                    }

                    newEvent.Timestamp = DateTime.UtcNow;
                    LastEvent = newEvent;
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
}
