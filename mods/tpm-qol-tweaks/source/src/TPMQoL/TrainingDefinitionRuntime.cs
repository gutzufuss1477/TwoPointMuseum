using System;
using System.Collections.Generic;
using TPS.Game;

namespace TPMQoL;

internal static class TrainingDefinitionRuntime
{
    private static readonly Dictionary<long, float> VanillaRates = new();
    private static long _lastDefinitionId = long.MinValue;
    private static float _lastAppliedMultiplier = float.NaN;

    internal static void EnsureApplied()
    {
        try
        {
            var levelState = LevelState.Instance;
            var config = levelState?.Config;
            if (config == null)
                return;

            var id = config.TrainingDefinition;
            var definition = LevelDatabaseUtils.Get(id);
            if (definition == null)
                return;

            var key = id.ID;
            if (!VanillaRates.TryGetValue(key, out var vanillaRate))
            {
                vanillaRate = definition.XPRatePerSecond;
                VanillaRates[key] = vanillaRate;
                Plugin.Log.LogInfo(
                    $"Training definition discovered: id={key}, vanilla XP/s={vanillaRate:0.###}");
            }

            var multiplier = Plugin.TrainingXpMultiplier.Value;
            var target = vanillaRate * multiplier;

            if (Math.Abs(definition.XPRatePerSecond - target) > 0.0001f)
                definition.XPRatePerSecond = target;

            if (_lastDefinitionId != key ||
                Math.Abs(_lastAppliedMultiplier - multiplier) > 0.0001f)
            {
                _lastDefinitionId = key;
                _lastAppliedMultiplier = multiplier;
                Plugin.Log.LogInfo(
                    $"Training definition speed applied: {vanillaRate:0.###} -> {target:0.###} XP/s ({multiplier:0.##}x)");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Training definition speed skipped: {ex.Message}");
        }
    }

    internal static void RestoreKnownVanilla()
    {
        try
        {
            var levelState = LevelState.Instance;
            var config = levelState?.Config;
            if (config == null)
                return;

            var id = config.TrainingDefinition;
            if (!VanillaRates.TryGetValue(id.ID, out var vanillaRate))
                return;

            var definition = LevelDatabaseUtils.Get(id);
            if (definition != null)
                definition.XPRatePerSecond = vanillaRate;
        }
        catch
        {
            // Best-effort restore only.
        }
    }
}
