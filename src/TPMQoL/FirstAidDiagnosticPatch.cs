using HarmonyLib;
using TPS.Game;

namespace TPMQoL;

[HarmonyPatch(typeof(InteractionInstance), nameof(InteractionInstance.EnableModifiers))]
internal static class FirstAidInteractionPatch
{
    private static void Postfix(InteractionInstance __instance, CharacterInstance character)
    {
        if (!Plugin.FirstAidCuresExpeditionAilments.Value ||
            __instance == null || character == null)
            return;

        var definition = __instance.Item?.Definition;
        if (definition == null || !FirstAidRuntime.IsFirstAid(definition.DefinitionID))
            return;

        FirstAidRuntime.Queue(character, definition);
    }
}
