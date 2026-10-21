using System;
using System.Collections.Generic;
using TPS.Core.Entities;
using TPS.Game;
using Unity.Entities;

namespace TPMQoL;

// Frozen exhibits from any theme may melt when their icyness/snowyness attribute
// reaches zero. Target only placed exhibits that actually have these attributes.
// Verified on Item_Exhibit_Bob_Sculpture_Illusion: Snowyness 92.667 -> 100.
internal static class FrozenExhibitPreservationRuntime
{
    private const long IcynessAttributeId = -911725493L;
    private const long SnowynessAttributeId = -1731548983L;
    private const long DecorationSnowynessAttributeId = -1165407409L;

    private static readonly List<Entity> FrozenEntities = new();
    private static long _levelPointer = long.MinValue;
    private static bool _ready;
    private static int _refreshTicks;
    private static bool _reportedDisabled;
    private static int _lastCount = -1;

    internal static void EnsureApplied()
    {
        try
        {
            if (!Plugin.ArtNeverMelts.Value)
            {
                if (!_reportedDisabled)
                {
                    _reportedDisabled = true;
                    FrozenEntities.Clear();
                    _ready = false;
                    _refreshTicks = 0;
                    Plugin.Log.LogInfo("Frozen-exhibit melt protection disabled (vanilla).");
                }
                return;
            }

            _reportedDisabled = false;
            var level = LevelState.Instance;
            if (level?.LevelConfig == null)
                return;
            var pointer = level.Pointer.ToInt64();
            if (pointer != _levelPointer)
            {
                _levelPointer = pointer;
                FrozenEntities.Clear();
                _ready = false;
                _refreshTicks = 0;
                _lastCount = -1;
            }

            if (!_ready || ++_refreshTicks >= 30)
            {
                DiscoverFrozenExhibits();
                _refreshTicks = 0;
            }

            foreach (var entity in FrozenEntities)
            {
                SetAttributeToMax(entity, IcynessAttributeId);
                SetAttributeToMax(entity, SnowynessAttributeId);
                SetAttributeToMax(entity, DecorationSnowynessAttributeId);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Frozen-exhibit protection skipped: {ex.Message}");
        }
    }

    internal static void Reapply()
    {
        _ready = false;
        _refreshTicks = 0;
        _reportedDisabled = false;
        EnsureApplied();
    }

    private static void DiscoverFrozenExhibits()
    {
        FrozenEntities.Clear();
        var maximumId = ManagedEntityDirectory.NextLevelID;
        var listed = 0;
        for (long id = 1; id < maximumId; id++)
        {
            GameItemInstance item;
            if (!ManagedEntityDirectory.TryGetReference<GameItemInstance>(id, out item, false) ||
                item?.Definition == null || !item.AddedToWorld)
                continue;

            var name = item.Definition.SafeName ?? string.Empty;
            if (!name.StartsWith("Item_Exhibit_", StringComparison.OrdinalIgnoreCase))
                continue;

            Entity entity;
            if (!ManagedEntityDirectory.TryGetEntity(id, out entity, false) || entity == Entity.Null)
                entity = item.GetStatusIconEntity();
            if (entity == Entity.Null)
                continue;

            GenericAttributeValue ice, snow, snowDecor;
            if (!GenericAttributeLogic.TryGetValueByID(entity, IcynessAttributeId, out ice) &&
                !GenericAttributeLogic.TryGetValueByID(entity, SnowynessAttributeId, out snow) &&
                !GenericAttributeLogic.TryGetValueByID(entity, DecorationSnowynessAttributeId, out snowDecor))
                continue;

            FrozenEntities.Add(entity);
            if (listed++ < 6)
                Plugin.Log.LogInfo($"Frozen exhibit detected: {name} ({entity})");
        }

        _ready = true;
        if (_lastCount != FrozenEntities.Count)
        {
            _lastCount = FrozenEntities.Count;
            Plugin.Log.LogInfo($"Frozen-exhibit protection active on {_lastCount} ice/snow exhibit entities.");
        }
    }

    private static void SetAttributeToMax(Entity entity, long attributeId)
    {
        GenericAttributeValue value;
        if (!GenericAttributeLogic.TryGetValueByID(entity, attributeId, out value))
            return;

        var maximum = value.MaxValue;
        if (value.Value < maximum - 0.001f)
            GenericAttributeLogic.TrySetValueByID(entity, attributeId, ref maximum);
    }
}
