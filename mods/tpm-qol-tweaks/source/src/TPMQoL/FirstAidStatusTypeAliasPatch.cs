using System.Collections.Generic;
using HarmonyLib;
using TPS.Game;

namespace TPMQoL;

[HarmonyPatch(
    typeof(CharacterModifiers),
    nameof(CharacterModifiers.TryGetStatusEffectOfType))]
internal static class FirstAidStatusTypeAliasPatch
{
    private const long FirstAidInjuryStatusTypeId = -1892560411L;
    private const long CureMachineIllnessStatusTypeId = -1032762696L;

    private static readonly HashSet<long> LoggedIllnesses = new();

    private static void Postfix(
        CharacterModifiers __instance,
        StatusEffectTypeID statusEffectType,
        ref CharacterStatusEffectBaseDefinition result,
        ref bool __result)
    {
        if (__result ||
            !Plugin.FirstAidCuresExpeditionAilments.Value ||
            __instance == null ||
            statusEffectType.ID != FirstAidInjuryStatusTypeId)
            return;

        var activeDefinitions = __instance.StatusEffects;
        if (activeDefinitions == null)
            return;

        for (var i = 0; i < activeDefinitions.Count; i++)
        {
            var definition = activeDefinitions[i];
            if (definition == null ||
                definition.StatusEffectType.ID != CureMachineIllnessStatusTypeId)
                continue;

            // Alias Cure-Machine-only expedition illnesses to the physical-
            // injury lookup used by the vanilla Expedition Recovery Device.
            // The original definition and its real StatusEffectType remain
            // unchanged; only this failed lookup receives the fallback.
            result = definition;
            __result = true;

            if (LoggedIllnesses.Add(definition.ID))
            {
                Plugin.Log.LogInfo(
                    $"First Aid illness routing alias used: " +
                    $"illness='{definition.name}', id={definition.ID}, " +
                    $"requestedType={FirstAidInjuryStatusTypeId}, " +
                    $"actualType={CureMachineIllnessStatusTypeId}");
            }

            return;
        }
    }
}
