using System;
using System.Collections.Generic;
using TPS.Game;
using Unity.Collections;
using Unity.Entities;

namespace TPMQoL;

// Optional attraction-specific protection. Intentionally OFF by default:
// vanilla Medium/Low maintenance produces distinct Thrill bonuses.
internal static class RideMaintenanceRuntime
{
    private const float FullMaintenance = 100.0f;

    private static readonly List<Entity> Rides = new();
    private static long _databasePointer = long.MinValue;
    private static bool _entitiesReady;
    private static bool _wasEnabled;
    private static int _refreshTicks;
    private static int _lastCount = -1;
    private static int _reportedUpdates;

    internal static void EnsureApplied()
    {
        try
        {
            // Absolutely no ride writes or tier overrides when disabled. This
            // preserves the vanilla Medium maintenance / Thrill-bonus gameplay.
            if (!Plugin.RideMaintenanceAlwaysHigh.Value)
            {
                if (_wasEnabled)
                {
                    Rides.Clear();
                    _entitiesReady = false;
                    _refreshTicks = 0;
                    _lastCount = -1;
                    _reportedUpdates = 0;
                    Plugin.Log.LogInfo("Ride maintenance HIGH protection disabled: vanilla tier progression restored.");
                }
                _wasEnabled = false;
                return;
            }

            if (!_wasEnabled)
            {
                _wasEnabled = true;
                _entitiesReady = false;
                _refreshTicks = 0;
                _reportedUpdates = 0;
                Plugin.Log.LogInfo("Ride maintenance HIGH protection enabled: restore built attractions to 100/100.");
            }

            var database = LevelState.Instance?.LevelConfig?.ExhibitDatabase?.Get();
            if (database == null)
                return;

            var pointer = database.Pointer.ToInt64();
            if (_databasePointer != pointer)
            {
                _databasePointer = pointer;
                Rides.Clear();
                _entitiesReady = false;
                _refreshTicks = 0;
                _lastCount = -1;
            }

            // Keep already-placed rides and newly built attractions in scope.
            if (!_entitiesReady || ++_refreshTicks >= 120)
            {
                DiscoverRides();
                _refreshTicks = 0;
            }

            for (var i = 0; i < Rides.Count; i++)
                ApplyToRide(Rides[i]);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Ride maintenance protection skipped: {ex.Message}");
        }
    }

    internal static void Reapply()
    {
        _entitiesReady = false;
        _refreshTicks = 0;
        EnsureApplied();
    }

    private static void DiscoverRides()
    {
        Rides.Clear();
        NativeArray<Entity> all = default;
        try
        {
            all = TPS.Core.Entities.EntityManager.Active.GetAllEntities(Allocator.Temp);
            for (var i = 0; i < all.Length; i++)
            {
                var entity = all[i];
                ECExhibitRide ride;
                ECMaintenance maintenance;
                if (!TPS.Core.Entities.EntityManager.TryGetComponentData<ECExhibitRide>(entity, out ride) ||
                    !ride.IsBuilt ||
                    !TPS.Core.Entities.EntityManager.TryGetComponentData<ECMaintenance>(entity, out maintenance))
                    continue;

                Rides.Add(entity);
            }
        }
        finally
        {
            if (all.IsCreated)
                all.Dispose();
        }

        _entitiesReady = true;
        if (_lastCount != Rides.Count)
        {
            _lastCount = Rides.Count;
            Plugin.Log.LogInfo($"Ride maintenance HIGH: {_lastCount} built ride entities found.");
        }
    }

    private static void ApplyToRide(Entity entity)
    {
        ECExhibitRide ride;
        if (!TPS.Core.Entities.EntityManager.TryGetComponentData<ECExhibitRide>(entity, out ride) || !ride.IsBuilt)
            return;

        ECMaintenance maintenance;
        if (!TPS.Core.Entities.EntityManager.TryGetComponentData<ECMaintenance>(entity, out maintenance))
            return;

        ECMaintenanceTier tier;
        var hasTier = TPS.Core.Entities.EntityManager.TryGetComponentData<ECMaintenanceTier>(entity, out tier);
        var needsLevel = maintenance.Level.Value < FullMaintenance - 0.001f;
        var needsRepairCap = maintenance.MaxRepairLevel < FullMaintenance - 0.001f;
        var needsHighTier = hasTier && tier.MaxTier > 0 && tier.Tier < tier.MaxTier;

        if (!needsLevel && !needsRepairCap && !needsHighTier)
            return;

        var previousLevel = maintenance.Level.Value;
        var previousTier = hasTier ? tier.Tier : -1;
        if (needsLevel)
            maintenance.Level.Set(FullMaintenance);
        if (needsRepairCap)
            maintenance.MaxRepairLevel = FullMaintenance;

        if (needsLevel || needsRepairCap)
            TPS.Core.Entities.EntityManager.Active.SetComponentData(entity, maintenance);

        if (needsHighTier)
        {
            // Explicitly update the tier as well: Medium rides normally have
            // MaxRepairLevel=66 and Tier=1, whereas HIGH is Tier=2 and 100.
            tier.Tier = tier.MaxTier;
            tier.Changed = true;
            TPS.Core.Entities.EntityManager.Active.SetComponentData(entity, tier);
        }

        if (_reportedUpdates++ < 12)
            Plugin.Log.LogInfo(
                $"Ride maintenance HIGH restored: entity={entity}, level={previousLevel:0.###}->100, " +
                $"tier={previousTier}->{(hasTier ? tier.Tier : -1)}, " +
                $"repairCap={maintenance.MaxRepairLevel:0.###}");
    }
}
