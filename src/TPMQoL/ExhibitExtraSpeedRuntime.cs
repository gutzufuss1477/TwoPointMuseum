using System;
using System.Collections.Generic;
using TPS.Game;

namespace TPMQoL;

internal static class ExhibitExtraSpeedRuntime
{
    private sealed class PerkBaseline
    {
        internal PerkDefinition Definition;
        internal float VanillaInstallTime;
        internal string Name = string.Empty;
    }

    private static readonly Dictionary<long, PerkBaseline> ExhibitPerks = new();
    private static float _lastMultiplier = float.NaN;

    internal static void EnsureApplied()
    {
        ApplyExhibitPerks();
    }

    internal static void Reapply()
    {
        _lastMultiplier = float.NaN;
        ApplyExhibitPerks();
    }

    private static void ApplyExhibitPerks()
    {
        var manager = PerksManager.Instance;
        var perks = manager?._availablePerksCache;
        if (perks == null)
            return;

        var multiplier = Plugin.ExhibitExtraInstallSpeedMultiplier.Value;

        for (var i = 0; i < perks.Count; i++)
        {
            var perk = perks[i];
            if (perk == null || perk.Type != EPerkType.Exhibits)
                continue;

            var key = perk.Pointer.ToInt64();
            if (key == 0)
                continue;

            if (!ExhibitPerks.TryGetValue(key, out var baseline))
            {
                baseline = new PerkBaseline
                {
                    Definition = perk,
                    VanillaInstallTime = perk.InstallTime,
                    Name = perk.DisplayName ?? perk.name ?? string.Empty
                };
                ExhibitPerks[key] = baseline;

                Plugin.Log.LogInfo(
                    $"Exhibit extra install definition discovered: '{baseline.Name}', " +
                    $"installTime={baseline.VanillaInstallTime:0.###}s");
            }
            else
            {
                baseline.Definition = perk;
            }

            perk.InstallTime =
                multiplier <= 1.0f
                    ? baseline.VanillaInstallTime
                    : Math.Max(0.05f, baseline.VanillaInstallTime / multiplier);
        }

        if (float.IsNaN(_lastMultiplier) ||
            Math.Abs(_lastMultiplier - multiplier) > 0.0001f)
        {
            _lastMultiplier = multiplier;
            Plugin.Log.LogInfo(
                $"Exhibit extra installation speed applied: {multiplier:0.##}x to " +
                $"{ExhibitPerks.Count} known exhibit perks.");
        }
    }
}
