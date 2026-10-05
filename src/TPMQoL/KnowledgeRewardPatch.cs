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

        var needed = KnowledgeSpeedRuntime.InsightRewardNeededForOwnMaximum(
            exhibit.ExhibitDefinitionID,
            out var rating,
            out var targetInsight,
            out var calculatedKnowledge);

        if (needed < 0)
            return;

        var before = insightReward;
        insightReward = needed;

        if (_logged++ < 60)
        {
            Plugin.Log.LogInfo(
                $"Analysis insight capped to exhibit maximum: " +
                $"insight={rating.Insight}, initial={rating.InitialKnowledge}, " +
                $"knowledge={rating.Knowledge}, calculated={calculatedKnowledge}, " +
                $"max={rating.MaxKnowledge}, targetInsight={targetInsight}, " +
                $"reward {before}->{insightReward} " +
                $"(definition={exhibit.ExhibitDefinitionID.ID})");
        }
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

        var needed = KnowledgeSpeedRuntime.InsightRewardNeededForOwnMaximum(
            exhibit.ExhibitDefinitionID,
            out var rating,
            out var targetInsight,
            out var calculatedKnowledge);

        if (needed < 0)
            return;

        var before = insightReward;
        insightReward = needed;

        if (_logged++ < 60)
        {
            Plugin.Log.LogInfo(
                $"Wildlife insight capped to exhibit maximum: " +
                $"insight={rating.Insight}, initial={rating.InitialKnowledge}, " +
                $"knowledge={rating.Knowledge}, calculated={calculatedKnowledge}, " +
                $"max={rating.MaxKnowledge}, targetInsight={targetInsight}, " +
                $"reward {before}->{insightReward} " +
                $"(definition={exhibit.ExhibitDefinitionID.ID})");
        }
    }
}
