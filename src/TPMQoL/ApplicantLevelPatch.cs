using System;
using HarmonyLib;
using TPS.Game;

namespace TPMQoL;

[HarmonyPatch(typeof(JobApplicantPool), nameof(JobApplicantPool.CalculateLevel))]
internal static class ApplicantLevelPatch
{
    private static int _logged;

    private static void Postfix(QualificationDefinition definition, ref int __result)
    {
        var requested = Plugin.ApplicantMinimumRank.Value;
        if (requested <= 1 || definition == null) return;

        // Staff specialisations use the long 20-level progression.
        // Normal trainable skills currently use short 3-level progressions and
        // must stay untouched, otherwise they are accidentally maxed.
        var max = Math.Max(1, definition.MaxXPLevels);
        if (max <= 3) return;

        var target = Math.Min(requested, max);
        if (__result >= target) return;

        var before = __result;
        __result = target;

        if (_logged++ < 20)
            Plugin.Log.LogInfo($"Applicant rank floor applied: {before} -> {target} (max={max})");
    }
}
