using System;
using System.Collections.Generic;
using TPS.Game;

namespace TPMQoL;

internal static class ExpeditionSurveyRuntime
{
    private sealed class ObservedPoi
    {
        internal int LastExpeditionCount;
        internal int PendingPolls;
    }

    private static readonly Dictionary<long, ObservedPoi> Observed = new();

    internal static void Observe(WorldMapLocationPOI poi)
    {
        if (poi?.Definition == null)
            return;

        var id = poi.Definition.ID;
        if (id == 0)
            return;

        if (!Observed.TryGetValue(id, out var state))
        {
            state = new ObservedPoi
            {
                LastExpeditionCount = poi.ExpeditionCount,
                PendingPolls = -1
            };
            Observed[id] = state;
            Plugin.Log.LogInfo(
                $"Expedition POI observed: poi={id}, count={state.LastExpeditionCount}");
        }
        else if (state.PendingPolls < 0 && poi.ExpeditionCount < state.LastExpeditionCount)
        {
            state.LastExpeditionCount = poi.ExpeditionCount;
        }
    }

    internal static void EnsureApplied()
    {
        if (Observed.Count == 0)
            return;

        var worldMap = WorldMap.Instance;
        if (worldMap == null)
            return;

        foreach (var pair in Observed)
        {
            var poiId = pair.Key;
            var state = pair.Value;

            var id = new WorldMapLocationPOIDefinitionID(poiId);
            WorldMapLocationPOI poi;
            if (!worldMap.GetWorldMapLocationPOI(id, out poi) || poi == null)
                continue;

            var count = poi.ExpeditionCount;
            if (count > state.LastExpeditionCount)
            {
                Plugin.Log.LogInfo(
                    $"Expedition completion detected by POI count: poi={poiId}, " +
                    $"count {state.LastExpeditionCount} -> {count}");
                state.LastExpeditionCount = count;
                state.PendingPolls = 4;
            }

            if (state.PendingPolls > 0)
            {
                state.PendingPolls--;
                continue;
            }

            if (state.PendingPolls == 0)
            {
                state.PendingPolls = -1;

                if (!Plugin.ExpeditionMaxSurveyAfterOne.Value)
                    continue;

                var xp = poi.SurveyXP;
                var beforeLevel = xp.Level;
                var beforeProgress = xp.Progress;
                var max = xp.MaxLevel;

                if (beforeLevel < max || !xp.IsMaxLevelAndXP())
                {
                    xp.SetLevel(max);
                    poi.SurveyXP = xp;
                }

                var persisted = poi.SurveyXP;
                Plugin.Log.LogInfo(
                    $"Expedition max survey applied by polling: poi={poiId}, " +
                    $"level {beforeLevel} ({beforeProgress:P0}) -> {persisted.Level} ({persisted.Progress:P0}), max={persisted.MaxLevel}");
            }
        }
    }
}
