using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;


namespace LMeter.Helpers;

public static class CharacterState
{
    public static readonly uint[] _goldenSaucerIDs =
    {
        144, // The Gold Saucer | Open world zone
        388, // Chocobo Square | ???
        389, // Chocobo Square | ???
        390, // Chocobo Square | ???
        391, // Chocobo Square | ???
        579, // The Battlehall | ???
        792, // The Fall of Belah'dia | Jump puzzles
        831, // The Manderville Tables | Mojang
        899, // The Falling City of Nym | Jump puzzles
        941, // The Battlehall | ???
    };

    public static bool IsCharacterBusy() =>
        PluginManager.Instance.Condition[ConditionFlag.WatchingCutscene] ||
        PluginManager.Instance.Condition[ConditionFlag.WatchingCutscene78] ||
        PluginManager.Instance.Condition[ConditionFlag.OccupiedInCutSceneEvent] ||
        PluginManager.Instance.Condition[ConditionFlag.CreatingCharacter] ||
        PluginManager.Instance.Condition[ConditionFlag.BetweenAreas] ||
        PluginManager.Instance.Condition[ConditionFlag.BetweenAreas51] ||
        PluginManager.Instance.Condition[ConditionFlag.OccupiedSummoningBell];

    public static bool IsInCombat() =>
        PluginManager.Instance.Condition[ConditionFlag.InCombat];

    public static bool IsInDuty() =>
        PluginManager.Instance.Condition[ConditionFlag.BoundByDuty];

    public static bool IsPerforming() =>
        PluginManager.Instance.Condition[ConditionFlag.Performing];

    public static bool IsInGoldenSaucer()
    {
        var territoryId = PluginManager.Instance.ClientState.TerritoryType;
        foreach (var id in _goldenSaucerIDs)
        {
            if (id == territoryId) return true;
        }

        return false;
    }

    public static Job GetCharacterJob()
    {
        var player = PluginManager.Instance.ClientState.LocalPlayer;
        if (player is null) return Job.UKN;

        unsafe
        {
            return (Job)((Character*)player.Address)->CharacterData.ClassJob;
        }
    }

    public static (ushort territoryId, string? territoryName) GetCharacterLocation()
    {
        var locationId = PluginManager.Instance?.ClientState.TerritoryType;
        if (locationId == null || locationId < 4) return (0, null);

        var locationRow = PluginManager
            .Instance?
            .DataManager
            .GetExcelSheet<TerritoryType>()?
            .GetRow(locationId.Value);

        var instanceContentName = locationRow?.ContentFinderCondition.Value?.Name?.ToString();
        var placeName = locationRow?.PlaceName.Value?.Name?.ToString();

        return
        (
            locationId.Value,
            string.IsNullOrEmpty(instanceContentName)
                ? placeName
                : instanceContentName
        );
    }

    public static bool IsJobType(Job job, JobType type, IEnumerable<Job>? jobList = null) => type switch
    {
        JobType.All => true,
        JobType.Tanks => job is Job.GLA or Job.MRD or Job.PLD or Job.WAR or Job.DRK or Job.GNB,
        JobType.Casters => job is Job.THM or Job.ACN or Job.BLM or Job.SMN or Job.RDM or Job.BLU,
        JobType.Melee => job is Job.PGL or Job.LNC or Job.ROG or Job.MNK or Job.DRG or Job.NIN or Job.SAM or Job.RPR,
        JobType.Ranged => job is Job.ARC or Job.BRD or Job.MCH or Job.DNC,
        JobType.Healers => job is Job.CNJ or Job.WHM or Job.SCH or Job.AST or Job.SGE,
        JobType.DoH => job is Job.CRP or Job.BSM or Job.ARM or Job.GSM or Job.LTW or Job.WVR or Job.ALC or Job.CUL,
        JobType.DoL => job is Job.MIN or Job.BOT or Job.FSH,
        JobType.Combat => IsJobType(job, JobType.DoW) || IsJobType(job, JobType.DoM),
        JobType.DoW => IsJobType(job, JobType.Tanks) || IsJobType(job, JobType.Melee) || IsJobType(job, JobType.Ranged),
        JobType.DoM => IsJobType(job, JobType.Casters) || IsJobType(job, JobType.Healers),
        JobType.Crafters => IsJobType(job, JobType.DoH) || IsJobType(job, JobType.DoL),
        JobType.Custom => jobList is not null && jobList.Contains(job),
        _ => false
    };
}
