using HarmonyLib;
using TPS.Game;

namespace TPMQoL;

[HarmonyPatch(typeof(ActiveWorkshopProject), nameof(ActiveWorkshopProject.Update))]
internal static class WorkshopProgressPatch
{
    private static bool _logged;

    private static void Prefix(ref float deltaTime)
    {
        var multiplier = Plugin.WorkshopSpeedMultiplier.Value;
        if (multiplier <= 1.0f) return;

        var before = deltaTime;
        deltaTime *= multiplier;

        if (!_logged)
        {
            _logged = true;
            Plugin.Log.LogInfo(
                $"Workshop runtime hook active: deltaTime {before:0.####} -> {deltaTime:0.####} ({multiplier:0.##}x)");
        }
    }
}
