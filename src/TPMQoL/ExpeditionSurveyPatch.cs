using HarmonyLib;
using TPS.Game;

namespace TPMQoL;

[HarmonyPatch(typeof(ExpeditionUtils), nameof(ExpeditionUtils.CalculateSurveyXP))]
internal static class ExpeditionSurveyPatch
{
    private static bool _logged;

    private static void Postfix(ref float __result)
    {
        var multiplier = Plugin.ExpeditionSurveyXpMultiplier.Value;
        if (multiplier <= 1.0f)
            return;

        var before = __result;
        __result *= multiplier;

        if (!_logged)
        {
            _logged = true;
            Plugin.Log.LogInfo(
                $"Expedition survey XP multiplied: {before:0.##} -> {__result:0.##} ({multiplier:0.##}x)");
        }
    }
}
