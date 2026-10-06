using System;
using System.Collections.Generic;
using TPS.Core.Entities;
using TPS.Game;
using Unity.Collections;
using Unity.Entities;

namespace TPMQoL;

internal static class ExhibitPreservationRuntime
{
    private const long GrubbinessAttributeId = -1834447109L;

    private static readonly Dictionary<long, float> DefinitionVanillaRates = new();
    private static readonly HashSet<long> BotanyVitalAttributeIds = new();
    private static readonly List<Entity> GrubbinessEntities = new();

    private static long _levelDatabasePointer = long.MinValue;
    private static bool _definitionsReady;
    private static bool _grubbinessReady;
    private static int _grubbinessRefreshTicks;

    private static AquariumManagerConfig _aquariumConfig;
    private static long _aquariumConfigPointer;
    private static float _vanillaAquariumCleanlinessRate;

    private static bool _lastExhibitsEnabled;
    private static bool _lastPlantsEnabled;
    private static bool _lastAquariumsEnabled;
    private static bool _haveLastSettings;

    internal static void EnsureApplied()
    {
        try
        {
            if (!EnsureDefinitions())
                return;

            ApplyDefinitionRates();

            if (Plugin.ImmortalPlants.Value)
                ApplyBotany();

            if (Plugin.ExhibitsNeverDeteriorate.Value)
            {
                if (!_grubbinessReady || ++_grubbinessRefreshTicks >= 120)
                {
                    DiscoverGrubbinessEntities();
                    _grubbinessRefreshTicks = 0;
                }

                ApplyGrubbiness();
            }

            ApplyAquariums();
            LogSettingsIfChanged();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Exhibit preservation skipped: {ex.Message}");
        }
    }

    internal static void Reapply()
    {
        _grubbinessReady = false;
        _grubbinessRefreshTicks = 0;
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
        if (pointer != _levelDatabasePointer)
        {
            _levelDatabasePointer = pointer;
            DefinitionVanillaRates.Clear();
            BotanyVitalAttributeIds.Clear();
            GrubbinessEntities.Clear();
            _definitionsReady = false;
            _grubbinessReady = false;
            _grubbinessRefreshTicks = 0;
            _aquariumConfig = null;
            _aquariumConfigPointer = 0;
            _haveLastSettings = false;
        }

        if (_definitionsReady)
            return true;

        var botanyDefinitions = 0;
        for (var i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            var itemExhibit = definition?.TryCast<ExhibitItemDefinition>();
            var item = itemExhibit?.ItemDefinition;
            if (definition == null || item == null || string.IsNullOrEmpty(definition.SafeName))
                continue;

            var isBotany =
                definition.SafeName.IndexOf("_Botany_", StringComparison.OrdinalIgnoreCase) >= 0 &&
                definition.SafeName.IndexOf("_Display_", StringComparison.OrdinalIgnoreCase) < 0;

            if (!isBotany)
                continue;

            botanyDefinitions++;

            ECPGenericAttributes genericAttributes;
            if (!item.GetComponent<ECPGenericAttributes>(out genericAttributes) || genericAttributes == null)
                continue;

            var attributes = genericAttributes._attributes;
            if (attributes == null)
                continue;

            for (var a = 0; a < attributes.Length; a++)
            {
                var attribute = attributes[a];
                if (attribute == null || !attribute.IsVital)
                    continue;

                var id = attribute.ID;
                BotanyVitalAttributeIds.Add(id);
                RememberVanillaRate(attribute);
            }
        }

        try
        {
            var grubbiness = LevelDatabaseUtils.Get(new GenericAttributeDefinitionID(GrubbinessAttributeId));
            if (grubbiness != null)
                RememberVanillaRate(grubbiness);
        }
        catch
        {
        }

        _definitionsReady = true;
        Plugin.Log.LogInfo(
            $"Preservation definitions discovered: botany={botanyDefinitions}, " +
            $"botanyVitalAttributes={BotanyVitalAttributeIds.Count}, grubbinessKnown={DefinitionVanillaRates.ContainsKey(GrubbinessAttributeId)}");
        return true;
    }

    private static void RememberVanillaRate(GenericAttributeDefinition definition)
    {
        if (definition == null || DefinitionVanillaRates.ContainsKey(definition.ID))
            return;

        DefinitionVanillaRates[definition.ID] = definition._changeOverTime;
    }

    private static void ApplyDefinitionRates()
    {
        foreach (var id in BotanyVitalAttributeIds)
            SetDefinitionRate(id, Plugin.ImmortalPlants.Value);

        SetDefinitionRate(GrubbinessAttributeId, Plugin.ExhibitsNeverDeteriorate.Value);
    }

    private static void SetDefinitionRate(long id, bool freeze)
    {
        if (!DefinitionVanillaRates.TryGetValue(id, out var vanillaRate))
            return;

        try
        {
            var definition = LevelDatabaseUtils.Get(new GenericAttributeDefinitionID(id));
            if (definition != null)
                definition._changeOverTime = freeze ? 0.0f : vanillaRate;
        }
        catch
        {
        }
    }

    private static void ApplyBotany()
    {
        var maxId = ManagedEntityDirectory.NextLevelID;
        for (long id = 1; id < maxId; id++)
        {
            GameItemInstance item;
            if (!ManagedEntityDirectory.TryGetReference<GameItemInstance>(id, out item, false) ||
                item?.Definition == null || !item.AddedToWorld)
                continue;

            var safeName = item.Definition.SafeName ?? string.Empty;
            if (safeName.IndexOf("_Botany_", StringComparison.OrdinalIgnoreCase) < 0 ||
                safeName.IndexOf("_Display_", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            Entity entity;
            if (!ManagedEntityDirectory.TryGetEntity(id, out entity, false) || entity == Entity.Null)
                entity = item.GetStatusIconEntity();
            if (entity == Entity.Null)
                continue;

            foreach (var attributeId in BotanyVitalAttributeIds)
                SetAttributeToMax(entity, attributeId);
        }
    }

    private static void DiscoverGrubbinessEntities()
    {
        GrubbinessEntities.Clear();
        NativeArray<Entity> entities = default;

        try
        {
            entities = TPS.Core.Entities.EntityManager.Active.GetAllEntities(Allocator.Temp);
            for (var i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                ECGenericAttributeVisual visual;
                if (!TPS.Core.Entities.EntityManager.TryGetComponentData<ECGenericAttributeVisual>(entity, out visual) ||
                    visual.AttributeID.ID != GrubbinessAttributeId)
                    continue;

                GenericAttributeValue value;
                if (GenericAttributeLogic.TryGetValueByID(entity, GrubbinessAttributeId, out value))
                    GrubbinessEntities.Add(entity);
            }
        }
        finally
        {
            if (entities.IsCreated)
                entities.Dispose();
        }

        _grubbinessReady = true;
        Plugin.Log.LogInfo($"Preservation grubbiness entities discovered: {GrubbinessEntities.Count}");
    }

    private static void ApplyGrubbiness()
    {
        for (var i = 0; i < GrubbinessEntities.Count; i++)
            SetAttributeToMax(GrubbinessEntities[i], GrubbinessAttributeId);
    }

    private static void ApplyAquariums()
    {
        var manager = AquariumManager.Instance;
        var config = manager?.Config;
        if (manager == null || config == null)
            return;

        var pointer = config.Pointer.ToInt64();
        if (_aquariumConfig == null || pointer != _aquariumConfigPointer)
        {
            _aquariumConfig = config;
            _aquariumConfigPointer = pointer;
            _vanillaAquariumCleanlinessRate = config.CleanlinessRateOfChange;
            Plugin.Log.LogInfo($"Aquarium cleanliness rate discovered: {_vanillaAquariumCleanlinessRate:0.######}");
        }

        if (!Plugin.AquariumsStayClean.Value)
        {
            config.CleanlinessRateOfChange = _vanillaAquariumCleanlinessRate;
            return;
        }

        config.CleanlinessRateOfChange = 0.0f;

        var maxId = ManagedEntityDirectory.NextLevelID;
        for (long id = 1; id < maxId; id++)
        {
            RoomInstance room;
            if (!ManagedEntityDirectory.TryGetReference<RoomInstance>(id, out room, false) || room == null)
                continue;

            AquariumState state;
            if (!manager.TryGetState(room, out state) || state == null)
                continue;

            Entity roomEntity;
            if (!ManagedEntityDirectory.TryGetEntity(id, out roomEntity, false) || roomEntity == Entity.Null)
                roomEntity = room.GetStatusIconEntity();
            if (roomEntity == Entity.Null)
                continue;

            SetAttributeToMax(roomEntity, config.CleanlinessGenericAttributeID);
            SetAttributeToMin(roomEntity, config.MessinessGenericAttributeId);
            SetAttributeToMax(roomEntity, config.FilterCapacityGenericAttributeId);

            ECAquarium aquarium;
            if (TPS.Core.Entities.EntityManager.TryGetComponentData<ECAquarium>(roomEntity, out aquarium) &&
                aquarium.WaterFilterItem != Entity.Null)
            {
                SetAttributeToMax(aquarium.WaterFilterItem, config.FilterCapacityGenericAttributeId);
            }
        }
    }

    private static void SetAttributeToMax(Entity entity, long attributeId)
    {
        GenericAttributeValue value;
        if (!GenericAttributeLogic.TryGetValueByID(entity, attributeId, out value))
            return;

        var target = value.MaxValue;
        if (value.Value < target - 0.001f)
            GenericAttributeLogic.TrySetValueByID(entity, attributeId, ref target);
    }

    private static void SetAttributeToMin(Entity entity, long attributeId)
    {
        GenericAttributeValue value;
        if (!GenericAttributeLogic.TryGetValueByID(entity, attributeId, out value))
            return;

        var target = value.MinValue;
        if (value.Value > target + 0.001f)
            GenericAttributeLogic.TrySetValueByID(entity, attributeId, ref target);
    }

    private static void LogSettingsIfChanged()
    {
        var exhibits = Plugin.ExhibitsNeverDeteriorate.Value;
        var plants = Plugin.ImmortalPlants.Value;
        var aquariums = Plugin.AquariumsStayClean.Value;

        if (_haveLastSettings &&
            _lastExhibitsEnabled == exhibits &&
            _lastPlantsEnabled == plants &&
            _lastAquariumsEnabled == aquariums)
            return;

        _haveLastSettings = true;
        _lastExhibitsEnabled = exhibits;
        _lastPlantsEnabled = plants;
        _lastAquariumsEnabled = aquariums;

        Plugin.Log.LogInfo(
            $"Preservation applied: exhibitCondition100={exhibits}, botanyLife100={plants}, " +
            $"aquariumCleanFilterMax={aquariums}");
    }
}
