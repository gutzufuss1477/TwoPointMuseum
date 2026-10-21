using System;
using System.Collections.Generic;
using TPS.Core.Entities;
using TPS.Game;
using Unity.Collections;
using Unity.Entities;

namespace TPMQoL;

// Keep these independent of the generic exhibit-grubbiness protection.
// Art maintenance is a 0..100 ECMaintenance level; frozen exhibits are protected separately.
internal static class ArtGhostPreservationRuntime
{
    private const long GhostHappinessAttributeId = -1790720386L;

    private static readonly HashSet<string> ArtItemSafeNames = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<Entity> ArtEntities = new();
    private static readonly List<Entity> GhostEntities = new();

    private static long _exhibitDatabasePointer = long.MinValue;
    private static bool _definitionsReady;
    private static bool _entitiesReady;
    private static int _refreshTicks;
    private static bool _haveLastSettings;
    private static bool _lastArtMaintenance;
    private static bool _lastArtMelting;
    private static bool _lastGhostPeace;

    internal static void EnsureApplied()
    {
        var artMaintenance = Plugin.ArtMaintenanceProtected.Value;
        var artMelting = Plugin.ArtNeverMelts.Value;
        var ghostPeace = Plugin.GhostInnerPeaceProtected.Value;
        try
        {
            if (!EnsureDefinitions())
                return;

            if (artMaintenance || ghostPeace)
            {
                if (!_entitiesReady || ++_refreshTicks >= 120)
                {
                    DiscoverEntities(artMaintenance, ghostPeace);
                    _refreshTicks = 0;
                }

                if (artMaintenance)
                    ApplyArtMaintenance();

                if (ghostPeace)
                    ApplyGhostPeace();
            }

            if (!_haveLastSettings ||
                artMaintenance != _lastArtMaintenance ||
                artMelting != _lastArtMelting ||
                ghostPeace != _lastGhostPeace)
            {
                _lastArtMaintenance = artMaintenance;
                _lastArtMelting = artMelting;
                _lastGhostPeace = ghostPeace;
                _haveLastSettings = true;
                Plugin.Log.LogInfo(
                    $"Art/ghost preservation: artMaintenance100={artMaintenance}, " +
                    $"artNeverMelts={artMelting}, ghostPeace100={ghostPeace}; " +
                    $"artEntities={ArtEntities.Count}, ghostEntities={GhostEntities.Count}");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Art/ghost preservation skipped: {ex.Message}");
        }
    }

    internal static void Reapply()
    {
        _entitiesReady = false;
        _refreshTicks = 0;
        _haveLastSettings = false;
        EnsureApplied();
    }

    private static bool EnsureDefinitions()
    {
        var database = LevelState.Instance?.LevelConfig?.ExhibitDatabase?.Get();
        var definitions = database?._definitions;
        if (database == null || definitions == null || definitions.Count == 0)
            return false;

        var pointer = database.Pointer.ToInt64();
        if (pointer != _exhibitDatabasePointer)
        {
            _exhibitDatabasePointer = pointer;
            ArtItemSafeNames.Clear();
            ArtEntities.Clear();
            GhostEntities.Clear();
            _definitionsReady = false;
            _entitiesReady = false;
            _refreshTicks = 0;
            _haveLastSettings = false;
        }

        if (_definitionsReady)
            return true;

        var matchingThemes = new HashSet<long>();
        for (var i = 0; i < definitions.Count; i++)
        {
            var exhibit = definitions[i];
            var itemExhibit = exhibit?.TryCast<ExhibitItemDefinition>();
            var item = itemExhibit?.ItemDefinition;
            if (item == null)
                continue;

            if (!IsArtExhibit(exhibit, matchingThemes))
                continue;

            var itemName = item.SafeName;
            if (!string.IsNullOrEmpty(itemName) && ArtItemSafeNames.Add(itemName) && ArtItemSafeNames.Count <= 12)
                Plugin.Log.LogInfo($"Art exhibit definition found: {exhibit.SafeName} -> {itemName}");
        }

        _definitionsReady = true;
        Plugin.Log.LogInfo(
            $"Art preservation definitions discovered: themes={matchingThemes.Count}, " +
            $"itemDefinitions={ArtItemSafeNames.Count}");
        return true;
    }

    private static bool IsArtExhibit(ExhibitDefinition exhibit, HashSet<long> matchingThemes)
    {
        var themeId = exhibit.ExhibitThemeID.ID;
        if (matchingThemes.Contains(themeId))
            return true;

        // Prefer the actual exhibit theme over guessing from the individual item name.
        try
        {
            var theme = LevelDatabaseUtils.Get(exhibit.ExhibitThemeID);
            if (theme != null && IsArtName(theme.SafeName, theme.DisplayName))
            {
                matchingThemes.Add(themeId);
                return true;
            }
        }
        catch
        {
            // The theme database may be loading when the exhibit database first appears.
        }

        return IsArtName(exhibit.SafeName, null);
    }

    private static bool IsArtName(string safeName, string displayName)
    {
        if (!string.IsNullOrEmpty(displayName) &&
            (displayName.Equals("Art", StringComparison.OrdinalIgnoreCase) ||
             displayName.Equals("Kunst", StringComparison.OrdinalIgnoreCase) ||
             displayName.Equals("Fine Art", StringComparison.OrdinalIgnoreCase)))
            return true;

        if (string.IsNullOrEmpty(safeName))
            return false;

        return safeName.Equals("Art", StringComparison.OrdinalIgnoreCase) ||
               safeName.Equals("Kunst", StringComparison.OrdinalIgnoreCase) ||
               safeName.Equals("ThemeArt", StringComparison.OrdinalIgnoreCase) ||
               safeName.IndexOf("_Art_", StringComparison.OrdinalIgnoreCase) >= 0 ||
               safeName.EndsWith("_Art", StringComparison.OrdinalIgnoreCase) ||
               safeName.StartsWith("Art_", StringComparison.OrdinalIgnoreCase) ||
               safeName.IndexOf("FineArt", StringComparison.OrdinalIgnoreCase) >= 0 ||
               safeName.IndexOf("ArtGallery", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void DiscoverEntities(bool includeArt, bool includeGhosts)
    {
        ArtEntities.Clear();
        GhostEntities.Clear();

        if (includeArt)
        {
            var maxId = ManagedEntityDirectory.NextLevelID;
            for (long id = 1; id < maxId; id++)
            {
                GameItemInstance item;
                if (!ManagedEntityDirectory.TryGetReference<GameItemInstance>(id, out item, false) ||
                    item?.Definition == null || !item.AddedToWorld ||
                    (!ArtItemSafeNames.Contains(item.Definition.SafeName ?? string.Empty) &&
                     !(item.Definition.SafeName ?? string.Empty).StartsWith("Item_Studio_", StringComparison.OrdinalIgnoreCase) &&
                     !(item.Definition.SafeName ?? string.Empty).StartsWith("Item_Exhibit_Bob_Interactive_", StringComparison.OrdinalIgnoreCase)))
                    continue;

                Entity entity;
                if (!ManagedEntityDirectory.TryGetEntity(id, out entity, false) || entity == Entity.Null)
                    entity = item.GetStatusIconEntity();
                if (entity != Entity.Null)
                    ArtEntities.Add(entity);
            }
        }

        if (includeGhosts)
        {
            NativeArray<Entity> entities = default;
            try
            {
                entities = TPS.Core.Entities.EntityManager.Active.GetAllEntities(Allocator.Temp);
                for (var i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    ECGhost ghost;
                    if (TPS.Core.Entities.EntityManager.TryGetComponentData<ECGhost>(entity, out ghost) &&
                        ghost.RageAttribute.ID == GhostHappinessAttributeId)
                        GhostEntities.Add(entity);
                }
            }
            finally
            {
                if (entities.IsCreated)
                    entities.Dispose();
            }
        }

        var maintenanceCount = 0;
        var firstMaintenanceLevel = -1.0f;
        foreach (var art in ArtEntities)
        {
            ECMaintenance maintenance;
            if (!TPS.Core.Entities.EntityManager.TryGetComponentData<ECMaintenance>(art, out maintenance))
                continue;
            maintenanceCount++;
            if (firstMaintenanceLevel < 0.0f)
                firstMaintenanceLevel = maintenance.Level.Value;
        }
        if (maintenanceCount > 0)
            Plugin.Log.LogInfo($"Art maintenance discovery: {maintenanceCount} entities, sampleLevel={firstMaintenanceLevel:0.###}/100");

        if (GhostEntities.Count > 0)
        {
            var ghostEntity = GhostEntities[0];
            ECGhost ghost;
            if (TPS.Core.Entities.EntityManager.TryGetComponentData<ECGhost>(ghostEntity, out ghost))
            {
                var rageId = ghost.RageAttribute.ID;
                GenericAttributeValue rage;
                if (GenericAttributeLogic.TryGetValueByID(ghostEntity, rageId, out rage))
                    Plugin.Log.LogInfo(
                        $"Ghost sample: rageAttribute={rageId}, value={rage.Value:0.####}, " +
                        $"min={rage.MinValue:0.####}, max={rage.MaxValue:0.####}");
            }
        }

        _entitiesReady = true;
        Plugin.Log.LogInfo(
            $"Art/ghost preservation entities discovered: art={ArtEntities.Count}, ghosts={GhostEntities.Count}");
    }

    private static void ApplyArtMaintenance()
    {
        for (var i = 0; i < ArtEntities.Count; i++)
        {
            var entity = ArtEntities[i];
            ECMaintenance maintenance;
            if (!TPS.Core.Entities.EntityManager.TryGetComponentData<ECMaintenance>(entity, out maintenance))
                continue;

            // The real art-maintenance range is 0..100, not 0..1.
            // Keeping this component at full level was verified on the live save.
            if (maintenance.Level.Value < 99.999f)
            {
                maintenance.Level.Set(100.0f);
                TPS.Core.Entities.EntityManager.Active.SetComponentData(entity, maintenance);
            }
        }
    }

    private static void ApplyGhostPeace()
    {
        for (var i = 0; i < GhostEntities.Count; i++)
        {
            var entity = GhostEntities[i];
            ECGhost ghost;
            if (!TPS.Core.Entities.EntityManager.TryGetComponentData<ECGhost>(entity, out ghost))
                continue;

            // Despite the misleading field name RageAttribute, this resolves to
            // Attribute_Ghost_Happiness (-1790720386): 100 = full inner peace.
            // The initial 1.0.4 preview mistakenly wrote MinValue (zero).
            if (ghost.RageAttribute.ID != GhostHappinessAttributeId)
                continue;

            GenericAttributeValue value;
            if (!GenericAttributeLogic.TryGetValueByID(entity, GhostHappinessAttributeId, out value))
                continue;

            var fullInnerPeace = value.MaxValue;
            if (value.Value < fullInnerPeace - 0.001f)
                GenericAttributeLogic.TrySetValueByID(entity, GhostHappinessAttributeId, ref fullInnerPeace);
        }
    }

}
