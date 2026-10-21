using System;
using System.Collections.Generic;
using TPS.Game;

namespace TPMQoL;

internal static class ExhibitRideSpeedRuntime
{
    private static readonly Dictionary<long, float> BuildVanillaRates = new();
    private static readonly Dictionary<long, StaffJobDefinitionExhibitRideBuilding> BuildJobs = new();
    private static readonly Dictionary<long, float> UpgradeVanillaPoints = new();
    private static readonly Dictionary<long, GameItemUpgradeDefinition> RideUpgrades = new();

    private static long _staffDatabasePointer = long.MinValue;
    private static long _exhibitDatabasePointer = long.MinValue;
    private static bool _ready;
    private static bool _haveLastSettings;
    private static float _lastBuildMultiplier;
    private static float _lastUpgradeMultiplier;

    internal static void EnsureApplied()
    {
        try
        {
            if (!EnsureDefinitions())
                return;

            var buildMultiplier = Math.Max(1.0f, Plugin.ExhibitRideBuildSpeedMultiplier.Value);
            var upgradeMultiplier = Math.Max(1.0f, Plugin.ExhibitRideUpgradeSpeedMultiplier.Value);

            foreach (var pair in BuildJobs)
            {
                if (pair.Value != null && BuildVanillaRates.TryGetValue(pair.Key, out var vanilla))
                    pair.Value.BaseRate = vanilla * buildMultiplier;
            }

            foreach (var pair in RideUpgrades)
            {
                if (pair.Value != null && UpgradeVanillaPoints.TryGetValue(pair.Key, out var vanilla))
                    pair.Value.Points = Math.Max(0.01f, vanilla / upgradeMultiplier);
            }

            if (!_haveLastSettings ||
                Math.Abs(_lastBuildMultiplier - buildMultiplier) > 0.0001f ||
                Math.Abs(_lastUpgradeMultiplier - upgradeMultiplier) > 0.0001f)
            {
                _haveLastSettings = true;
                _lastBuildMultiplier = buildMultiplier;
                _lastUpgradeMultiplier = upgradeMultiplier;
                Plugin.Log.LogInfo(
                    $"Attraction speed applied: build={buildMultiplier:0.##}x ({BuildJobs.Count} job definition), " +
                    $"upgrade={upgradeMultiplier:0.##}x ({RideUpgrades.Count} ride upgrade definitions)");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Attraction speed skipped: {ex.Message}");
        }
    }

    internal static void Reapply()
    {
        _haveLastSettings = false;
        EnsureApplied();
    }

    private static bool EnsureDefinitions()
    {
        var levelConfig = LevelState.Instance?.LevelConfig;
        var staffDatabase = levelConfig?.StaffJobDatabase?.Get();
        var exhibitDatabase = levelConfig?.ExhibitDatabase?.Get();
        var staffDefinitions = staffDatabase?._definitions;
        var exhibitDefinitions = exhibitDatabase?._definitions;

        if (staffDatabase == null || exhibitDatabase == null ||
            staffDefinitions == null || exhibitDefinitions == null ||
            staffDefinitions.Count == 0 || exhibitDefinitions.Count == 0)
            return false;

        var staffPointer = staffDatabase.Pointer.ToInt64();
        var exhibitPointer = exhibitDatabase.Pointer.ToInt64();
        if (staffPointer != _staffDatabasePointer || exhibitPointer != _exhibitDatabasePointer)
        {
            RestoreVanilla();
            _staffDatabasePointer = staffPointer;
            _exhibitDatabasePointer = exhibitPointer;
            BuildVanillaRates.Clear();
            BuildJobs.Clear();
            UpgradeVanillaPoints.Clear();
            RideUpgrades.Clear();
            _ready = false;
            _haveLastSettings = false;
        }

        if (_ready)
            return true;

        for (var i = 0; i < staffDefinitions.Count; i++)
        {
            var definition = staffDefinitions[i];
            if (definition == null)
                continue;

            var buildJob = definition.TryCast<StaffJobDefinitionExhibitRideBuilding>();
            if (buildJob == null)
                continue;

            BuildJobs[buildJob.ID] = buildJob;
            if (!BuildVanillaRates.ContainsKey(buildJob.ID))
                BuildVanillaRates[buildJob.ID] = buildJob.BaseRate;
        }

        for (var i = 0; i < exhibitDefinitions.Count; i++)
        {
            var exhibit = exhibitDefinitions[i];
            var itemExhibit = exhibit?.TryCast<ExhibitItemDefinition>();
            var item = itemExhibit?.ItemDefinition;
            if (item == null)
                continue;

            ECPExhibitRide ride;
            if (!item.GetComponent<ECPExhibitRide>(out ride) || ride == null)
                continue;

            var upgrades = item.Upgrades;
            if (upgrades == null)
                continue;

            for (var u = 0; u < upgrades.Length; u++)
            {
                var upgrade = upgrades[u];
                if (upgrade == null)
                    continue;

                RideUpgrades[upgrade.ID] = upgrade;
                if (!UpgradeVanillaPoints.ContainsKey(upgrade.ID))
                    UpgradeVanillaPoints[upgrade.ID] = upgrade.Points;
            }
        }

        _ready = true;
        Plugin.Log.LogInfo(
            $"Attraction speed definitions discovered: buildJobs={BuildJobs.Count}, rideUpgrades={RideUpgrades.Count}");
        return true;
    }

    private static void RestoreVanilla()
    {
        foreach (var pair in BuildJobs)
        {
            if (pair.Value != null && BuildVanillaRates.TryGetValue(pair.Key, out var vanilla))
                pair.Value.BaseRate = vanilla;
        }

        foreach (var pair in RideUpgrades)
        {
            if (pair.Value != null && UpgradeVanillaPoints.TryGetValue(pair.Key, out var vanilla))
                pair.Value.Points = vanilla;
        }
    }
}
