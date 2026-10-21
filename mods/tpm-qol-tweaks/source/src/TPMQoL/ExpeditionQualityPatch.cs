using HarmonyLib;
using TPS.Game;

namespace TPMQoL;

internal static class ExpeditionQualitySettings
{
    internal static bool TryGetTarget(out ExhibitQuality quality)
    {
        var mode = Plugin.ExpeditionQuality.Value;
        if (mode < 0 || mode > 3)
        {
            quality = default;
            return false;
        }

        quality = (ExhibitQuality)mode;
        return true;
    }

    internal static ExhibitQuality Apply(ExhibitQuality vanilla)
    {
        if (!TryGetTarget(out var target))
            return vanilla;

        if (Plugin.ExpeditionForceQuality.Value)
            return target;

        return (int)vanilla < (int)target ? target : vanilla;
    }
}

[HarmonyPatch(typeof(ExpeditionUtils), nameof(ExpeditionUtils.GenerateQuality))]
internal static class ExpeditionGeneratedQualityPatch
{
    private static bool _logged;

    private static void Postfix(ref ExhibitQuality __result)
    {
        var before = __result;
        __result = ExpeditionQualitySettings.Apply(__result);

        if (!_logged && before != __result)
        {
            _logged = true;
            Plugin.Log.LogInfo(
                $"Expedition generated quality adjusted: {before} -> {__result}; " +
                $"force={Plugin.ExpeditionForceQuality.Value}");
        }
    }
}

[HarmonyPatch(typeof(ExpeditionUtils), nameof(ExpeditionUtils.GetMaxExhibitQuality))]
internal static class ExpeditionDisplayedMaxQualityPatch
{
    private static void Postfix(ref ExhibitQuality __result)
    {
        __result = ExpeditionQualitySettings.Apply(__result);
    }
}
