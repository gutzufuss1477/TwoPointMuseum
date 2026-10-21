using System;
using System.Collections.Generic;
using HarmonyLib;
using TPS.Game;
using UnityEngine.AI;

namespace TPMQoL;

internal static class StaffMovementAgentRegistry
{
    private static readonly HashSet<long> StaffAgents = new();
    private static readonly Dictionary<long, float> LastWritten = new();

    internal static void Register(NavMeshAgent agent, StaffInstance staff)
    {
        if (agent == null || staff == null)
            return;

        var key = agent.Pointer.ToInt64();
        if (key == 0)
            return;

        StaffAgents.Add(key);
        LastWritten.Remove(key);
    }

    internal static void Unregister(NavMeshAgent agent)
    {
        if (agent == null)
            return;

        var key = agent.Pointer.ToInt64();
        StaffAgents.Remove(key);
        LastWritten.Remove(key);
    }

    internal static void EnsureExistingRegistered()
    {
        var list = StaffManager.Instance?.Staff;
        if (list == null)
            return;

        for (var i = 0; i < list.Count; i++)
        {
            var staff = list[i];
            var agent = staff?.NavAgent?.Agent;
            if (staff == null || agent == null)
                continue;

            var key = agent.Pointer.ToInt64();
            if (key == 0 || StaffAgents.Contains(key))
                continue;

            Register(agent, staff);

            // Save-loaded staff may not run NavMeshAgentWrapper.Initialize again.
            // Reassign once so the setter patch applies the configured multiplier.
            var current = agent.speed;
            agent.speed = current;
        }
    }

    internal static void RefreshExistingSpeeds()
    {
        var list = StaffManager.Instance?.Staff;
        if (list == null)
            return;

        for (var i = 0; i < list.Count; i++)
        {
            var nav = list[i]?.NavAgent;
            var agent = nav?.Agent;
            if (agent == null)
                continue;

            var raw = nav.CachedSpeed;
            if (raw > 0.0f)
                agent.speed = raw;
        }
    }

    internal static void AdjustFinalSpeed(NavMeshAgent agent, ref float value)
    {
        if (agent == null)
            return;

        var key = agent.Pointer.ToInt64();
        if (key == 0 || !StaffAgents.Contains(key))
            return;

        var multiplier = Plugin.StaffMovementMultiplier.Value;
        if (multiplier <= 1.0f)
        {
            LastWritten.Remove(key);
            return;
        }

        var cached = FindCachedSpeed(key);

        // Do not multiply our own previous write again. If CachedSpeed has
        // changed to a new vanilla value, allow that new value through.
        if (LastWritten.TryGetValue(key, out var lastWritten) &&
            Math.Abs(value - lastWritten) < 0.0001f &&
            (cached <= 0.0f || Math.Abs((cached * multiplier) - value) < 0.0001f))
            return;

        value *= multiplier;
        LastWritten[key] = value;
    }

    private static float FindCachedSpeed(long agentPointer)
    {
        var list = StaffManager.Instance?.Staff;
        if (list == null)
            return -1.0f;

        for (var i = 0; i < list.Count; i++)
        {
            var staff = list[i];
            var nav = staff?.NavAgent;
            var agent = nav?.Agent;
            if (agent != null && agent.Pointer.ToInt64() == agentPointer)
                return nav.CachedSpeed;
        }

        return -1.0f;
    }
}

[HarmonyPatch(typeof(NavMeshAgentWrapper), nameof(NavMeshAgentWrapper.Initialize))]
internal static class StaffMovementRegisterPatch
{
    private static void Prefix(NavMeshAgentWrapper __instance, CharacterInstance character)
    {
        if (__instance == null || character == null)
            return;

        var staff = character.TryCast<StaffInstance>();
        if (staff != null)
            StaffMovementAgentRegistry.Register(__instance.Agent, staff);
    }
}

[HarmonyPatch(typeof(NavMeshAgentWrapper), "OnDestroy")]
internal static class StaffMovementUnregisterPatch
{
    private static void Prefix(NavMeshAgentWrapper __instance)
    {
        if (__instance != null)
            StaffMovementAgentRegistry.Unregister(__instance.Agent);
    }
}

[HarmonyPatch(typeof(NavMeshAgent), "set_speed")]
internal static class StaffMovementFinalSpeedPatch
{
    private static void Prefix(NavMeshAgent __instance, ref float value)
    {
        StaffMovementAgentRegistry.AdjustFinalSpeed(__instance, ref value);
    }
}
