using Dalamud.Logging;
using LMeter.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;


namespace LMeter.Act;

public class ActEventParser
{
    public ActEvent? LastEvent { get; set; }
    public List<ActEvent> PastEvents { get; set; } = null!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
