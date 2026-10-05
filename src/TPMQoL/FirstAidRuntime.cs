using System.Collections.Generic;
using TPS.Game;

namespace TPMQoL;

internal static class FirstAidRuntime
{
    private sealed class PendingCure
    {
        internal int Polls;
        internal long ItemDefinitionId;
        internal string ItemName = string.Empty;
    }

    // Confirmed usable first-aid items. Upgrade levels I/II/III keep the same base definition.
    private static readonly HashSet<long> FirstAidDefinitionIds = new()
    {
        // Expedition Recovery Device. Upgrade levels II/III keep this base
        // GameItemDefinition; only their upgrade definitions use other IDs.
        -27820635L
    };

    private static readonly Dictionary<long, PendingCure> Pending = new();

    internal static bool IsFirstAid(long definitionId)
    {
        return FirstAidDefinitionIds.Contains(definitionId);
    }

    internal static void Queue(CharacterInstance character, GameItemDefinition definition)
    {
        if (!Plugin.FirstAidCuresExpeditionAilments.Value ||
            character == null || definition == null)
            return;

        CharacterInstanceID characterId = character;
        if (characterId.ID == 0)
            return;

        Pending[characterId.ID] = new PendingCure
        {
            Polls = 3,
            ItemDefinitionId = definition.DefinitionID,
            ItemName = definition.DisplayName ?? string.Empty
        };

        if (Plugin.FirstAidDiagnostics.Value)
        {
            Plugin.Log.LogInfo(
                $"First-aid use queued: staff='{character.DisplayName}', " +
                $"item='{definition.DisplayName}', definitionID={definition.DefinitionID}");
        }
    }

    internal static void EnsureApplied()
    {
        if (Pending.Count == 0)
            return;

        if (!Plugin.FirstAidCuresExpeditionAilments.Value)
        {
            Pending.Clear();
            return;
        }

        var manager = ExpeditionManager.Instance;
        var types = manager?.Config?.ExpeditionStatusEffectTypesPriorityOrder;
        if (types == null || types.Count == 0)
            return;

        var ids = new List<long>(Pending.Keys);
        foreach (var rawId in ids)
        {
            var pending = Pending[rawId];
            if (pending.Polls-- > 0)
                continue;

            var id = new CharacterInstanceID(rawId);
            CharacterInstance character;
            if (!id.TryGet(out character) || character == null)
            {
                Pending.Remove(rawId);
                continue;
            }

            var modifiers = character.Modifiers;
            if (modifiers == null)
            {
                Pending.Remove(rawId);
                continue;
            }

            var removedTypes = 0;
            for (var i = 0; i < types.Count; i++)
            {
                if (modifiers.RemoveStatusEffectsOfType(types[i]))
                    removedTypes++;
            }

            if (removedTypes > 0)
            {
                CharacterModifiers.FlushRemoveStatusEffects();
                Plugin.Log.LogInfo(
                    $"First-aid expedition cure: staff='{character.DisplayName}', " +
                    $"item='{pending.ItemName}', removedTypes={removedTypes}");
            }
            else if (Plugin.FirstAidDiagnostics.Value)
            {
                Plugin.Log.LogInfo(
                    $"First-aid cure check: staff='{character.DisplayName}', " +
                    $"item='{pending.ItemName}', removedTypes=0");
            }

            Pending.Remove(rawId);
        }
    }
}
