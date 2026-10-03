using HarmonyLib;
using TPS.Game;

namespace TPMQoL;

[HarmonyPatch(typeof(AnalysisRoomManager), nameof(AnalysisRoomManager.GetExhibitRewards))]
internal static class AnalysisKnowledgeRewardPatch
{
    private static int _logged;

    private static void Postfix(ref ECExhibit exhibit, ref int insightReward, ref long moneyReward)
    {
        if (!Plugin.AnalysisMaxKnowledgeAfterOne.Value)
            return;

        var threshold = KnowledgeSpeedRuntime.MaxKnowledgeInsightThreshold();
        if (threshold <= 0 || insightReward >= threshold)
            return;

        var before = insightReward;
        insightReward = threshold;

        if (_logged++ < 20)
            Plugin.Log.LogInfo(
                $"Analysis insight boosted for max knowledge: {before} -> {insightReward} " +
                $"(definition={exhibit.ExhibitDefinitionID.ID})");
    }
}

[HarmonyPatch(typeof(VetRoomManager), nameof(VetRoomManager.GetExhibitRewards))]
internal static class WildlifeKnowledgeRewardPatch
{
    private static int _logged;

    private static void Postfix(ref ECExhibit exhibit, ref int insightReward)
    {
        if (!Plugin.WildlifeMaxKnowledgeAfterOne.Value)
            return;

        var threshold = KnowledgeSpeedRuntime.MaxKnowledgeInsightThreshold();
        if (threshold <= 0 || insightReward >= threshold)
            return;

        var before = insightReward;
        insightReward = threshold;

        if (_logged++ < 20)
            Plugin.Log.LogInfo(
                $"Wildlife insight boosted for max knowledge: {before} -> {insightReward} " +
                $"(definition={exhibit.ExhibitDefinitionID.ID})");
    }
}
