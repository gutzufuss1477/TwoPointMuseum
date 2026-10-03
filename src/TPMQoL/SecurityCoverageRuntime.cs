using System;
using System.Collections.Generic;
using TPS.Game;

namespace TPMQoL;

internal static class SecurityCoverageRuntime
{
    private static readonly Dictionary<long, float> VanillaRadius = new();
    private static long _lastDatabasePtr = long.MinValue;
    private static float _lastConfiguredRadius = float.NaN;
    private static bool _loggedNoTargets;

    internal static void EnsureApplied()
    {
        try
        {
            var levelState = LevelState.Instance;
            var levelConfig = levelState?.LevelConfig;
            var databaseRef = levelConfig?.StaffJobDatabase;
            var database = databaseRef?.Get();
            if (database == null)
                return;

            var databasePtr = database.Pointer.ToInt64();
            if (_lastDatabasePtr != databasePtr)
            {
                _lastDatabasePtr = databasePtr;
                _lastConfiguredRadius = float.NaN;
                _loggedNoTargets = false;
            }

            var definitions = database._definitions;
            if (definitions == null)
                return;

            var configuredRadius = Plugin.SecurityCoverageRadius.Value;
            var found = 0;
            var changed = 0;

            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition == null)
                    continue;

                var cameraJob = definition.TryCast<StaffJobDefinitionCamera>();
                if (cameraJob == null)
                    continue;

                found++;

                var key = cameraJob.Pointer.ToInt64();
                if (!VanillaRadius.TryGetValue(key, out var vanilla))
                {
                    vanilla = cameraJob.Radius;
                    VanillaRadius[key] = vanilla;
                    Plugin.Log.LogInfo(
                        $"Security camera-job radius discovered: definition={cameraJob.ID}, vanilla={vanilla:0.##}");
                }

                var target = configuredRadius <= 0.0f
                    ? vanilla
                    : Math.Max(vanilla, configuredRadius);

                if (Math.Abs(cameraJob.Radius - target) > 0.001f)
                {
                    cameraJob.Radius = target;
                    changed++;
                }
            }

            if (found == 0)
            {
                if (!_loggedNoTargets)
                {
                    _loggedNoTargets = true;
                    Plugin.Log.LogInfo(
                        "Security coverage: no StaffJobDefinitionCamera found in the loaded StaffJobDatabase.");
                }
                return;
            }

            if (float.IsNaN(_lastConfiguredRadius) ||
                Math.Abs(_lastConfiguredRadius - configuredRadius) > 0.001f)
            {
                _lastConfiguredRadius = configuredRadius;
                Plugin.Log.LogInfo(
                    $"Security coverage applied: radius={(configuredRadius <= 0 ? "vanilla" : configuredRadius.ToString("0.##"))}, " +
                    $"cameraJobs={found}, changed={changed}");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Security coverage skipped: {ex.Message}");
        }
    }
}
