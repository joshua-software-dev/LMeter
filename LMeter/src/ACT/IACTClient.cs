using Dalamud.Logging;
using Dalamud.Plugin;
using LMeter.Config;
using LMeter.Helpers;
using System.Collections.Generic;
using System.Linq;
using System;


namespace LMeter.ACT;

public interface IACTClient : IPluginDisposable
{
    public static IACTClient Current => 
        Singletons.Get<LMeterConfig>().ACTConfig.IINACTMode 
            ? Singletons.Get<IINACTClient>() 
            : Singletons.Get<ACTClient>();

    public static IACTClient GetNewClient()
    {
        Singletons.DeleteActClients();

        ACTConfig config = Singletons.Get<LMeterConfig>().ACTConfig;
        DalamudPluginInterface dpi = Singletons.Get<DalamudPluginInterface>();

        IACTClient client = config.IINACTMode
            ? new IINACTClient(config, dpi)
            : new ACTClient(config, dpi);
        Singletons.Register(client);
        return client;
    }

    public ACTEvent? LastEvent { get; set; }
    public List<ACTEvent> PastEvents { get; }

    public void Clear();
    public bool ClientReady();
    public bool ConnectionIncompleteOrFailed();
    public void DrawConnectionStatus();
    public void EndEncounter();
    public ACTEvent? GetEvent(int index = -1);
    public void Start();
    public void RetryConnection();

    public bool ParseNewEvent(ACTEvent? newEvent, int encounterHistorySize)
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
