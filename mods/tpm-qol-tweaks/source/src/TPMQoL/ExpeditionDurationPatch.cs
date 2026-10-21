using System;
using HarmonyLib;
using TPS.Game;

namespace TPMQoL;

[HarmonyPatch(typeof(ExpeditionSettings), nameof(ExpeditionSettings.GetDurationInDays))]
internal static class ExpeditionDurationPatch
{
    private static int _logged;

    private static void Postfix(ExpeditionSettings __instance, ref int __result)
    {
        ExpeditionSurveyRuntime.Observe(__instance?.POI);

        var multiplier = Plugin.ExpeditionSpeedMultiplier.Value;
        if (multiplier <= 1.0f || __result <= 0)
            return;

        var before = __result;
        __result = Math.Max(1, (int)Math.Ceiling(before / (double)multiplier));

        if (_logged++ < 20)
        {
            Plugin.Log.LogInfo(
                $"Expedition duration adjusted: {before} -> {__result} days ({multiplier:0.##}x)");
        }
    }
}
